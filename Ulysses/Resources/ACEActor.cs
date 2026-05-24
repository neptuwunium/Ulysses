// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers.Binary;
using System.Text;
using Pluto.Extensions;

namespace Ulysses.Resources;

public class ACEActor : Resource {
	public ACEActor(FHMFile fhm, string name, bool leaveOpen = false) : base(fhm, name, leaveOpen) {
		IsFullyUtilized = true;

		using var actorInfoBuf = fhm[^1];
		using var actorNameBuf = fhm[^3];

		ActorName = actorNameBuf.Span[0x10..].ReadString(Encoding.ASCII) ?? name;
		Type = BinaryPrimitives.ReadUInt16BigEndian(actorInfoBuf.Span[0x4..]);
	}

	public string ActorName { get; }
	public ushort Type { get; }
	public override int ResourceCount => 1;

	public override string GetResourceName(int resourceIndex, string baseName) => baseName + $"/{ActorName}.glb";
	public override bool Save(Stream stream, int resourceIndex) => throw new NotImplementedException();
}
