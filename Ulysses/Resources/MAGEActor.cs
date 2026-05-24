// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.IO.Binary;

namespace Ulysses.Resources;

// Rebuilding the actor FHM for blender importer using a more optimized format
public class MAGEActor : ACEActor {
	public MAGEActor(FHMFile fhm, string name, bool leaveOpen = false) : base(fhm, name, leaveOpen) { }

	public override string GetResourceName(int resourceIndex, string baseName) => baseName + $"/{ActorName}.mage";

	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x1C)]
	private struct MageHeader {
		// ReSharper disable UnusedAutoPropertyAccessor.Local
		public uint Magic { get; set; }
		public byte VersionMajor { get; set; }
		public byte VersionMinor { get; set; }
		public byte VersionPatch { get; set; }
		[field: MarshalAs(UnmanagedType.I1)] public bool DataIsBigEndian { get; set; }
		public int TableOffset { get; set; }
		public int Count { get; set; }
		// ReSharper disable once PropertyCanBeMadeInitOnly.Local
		public int Alignment { get; set; }
		public int Type { get; set; }
		public uint Game { get; set; }
		// int NameLength
		// char Name[NameLength]
		// [int Offset, int Size] DataBlob[Count] at TableOffset
	}

	public override bool Save(Stream stream, int resourceIndex) {
		using var writer = new StreamBinaryWriter(stream, true);

		var header = new MageHeader {
			Magic = 0x4547414D, // "MAGE"
			VersionMajor = 1,
			VersionMinor = 0,
			VersionPatch = 0,
			DataIsBigEndian = true,
			TableOffset = int.MaxValue,
			Count = FHM.Count,
			Alignment = 16,
			Type = Type,
			Game = 0x41454341, // "ACEA"
		};
		writer.Write(header);
		writer.WritePString<int, byte>(ActorName);
		writer.Align(header.Alignment);

		header.TableOffset = writer.Position;
		var offsetPairs = (stackalloc (int, int)[FHM.Count]);
		writer.Write(offsetPairs);
		writer.Align(header.Alignment);

		for(var index = 0; index <  FHM.Count; index++) {
			using var buffer = FHM[index];
			offsetPairs[index] = (writer.Position, buffer.Length);
			writer.WriteBytes(buffer);
			writer.Align(header.Alignment);
		}

		writer.Position = header.TableOffset;
		writer.Write(offsetPairs);

		writer.Position = 0;
		writer.Write(header);
		return true;
	}
}
