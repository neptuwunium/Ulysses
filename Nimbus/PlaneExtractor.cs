// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Metsys.Bson;
using Pluto;
using Pluto.Extensions;
using Pluto.IO.Binary;
using Ulysses;
using Ulysses.Resources;
using Ulysses.Struct;
using Ulysses.Struct.FHM;

namespace Nimbus;

public static class PlaneExtractor {
	public static readonly (string Prefix, string Name)[] Parts = [
		("DPL_P_", string.Empty),
		("DPL_D_", "destroyed"),
	];

	public static void SaveEmblem(HashId id, ResourceManager manager, string path) {
		using var fhm = manager.ReadFile(id);
		if (fhm is null) {
			return;
		}

		SaveFHM(fhm, path);
	}

	public static void SaveSkin(HashId id, ResourceManager manager, string path) {
		using var fhm = manager.ReadFile(id);
		if (fhm is null) {
			return;
		}

		SaveFHM(fhm, path);
		SavePaint(fhm, path);
	}

	private static void SavePaint(FHMFile fhm, string path) {
		var child = fhm.GetChildItem(0);
		var materialSet = child?.GetChildItem(1);
		if (!(materialSet?.Count > 3)) {
			return;
		}

		var paintBin = materialSet[^2];
		if (paintBin.Length != 0x180) {
			return;
		}

		var color = MemoryMarshal.Cast<byte, float>(paintBin.Span);
		color.ReverseEndianness();

		Directory.CreateDirectory(path);
		using var stream = new FileStream(Path.Combine(path, "paint.txt"), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
		using var writer = new StreamWriter(stream);
		writer.NewLine = "\n";
		for (var i = 0; i < color.Length; i += 4) {
			writer.WriteLine($"{color[i + 0]}, {color[i + 1]}, {color[i + 2]}, {color[i + 3]}");
		}
	}

	public static void SavePlane(FHMFile fhm, string path) {
		for (var i = 0; i < 6; ++i) {
			using var child = fhm.GetChildItem(i);
			using var mesh = child?.GetChildItem(1);
			if (mesh is null) {
				continue;
			}

			using var actorNameBuf = mesh[^3];
			if (actorNameBuf.Length != 0x400) {
				continue;
			}

			var actorName = actorNameBuf.Span[0x10..].ReadString(Encoding.ASCII) ?? string.Empty;
			if (string.IsNullOrEmpty(actorName)) {
				actorName = $"mesh{i}";
			}

			var actorPath = Path.Combine(path, actorName.SanitizeFilename());
			SaveFHM(child, actorPath);
		}

		using var engine = fhm[18];
		using var alert = fhm[19];
		SaveFileIfResource(engine, ResourceMagic.CriWare, path, "engine.acb");
		SaveFileIfResource(alert, ResourceMagic.CriWare, path, "alert.acb");

		using var iconFhm = fhm.GetChildItem(6);
		using var iconTxFhm = iconFhm?.GetChildItem(1);
		if (iconTxFhm is null) {
			return;
		}

		using var resource = Resource.Construct(iconTxFhm, iconTxFhm.GetItemHeader(0), 0, fhm.Header.HashId.ToString(), true);
		if (resource is null || resource.ResourceCount <= 0) {
			return;
		}

		var resourcePath = Path.Combine(path, "hud");
		SaveResource(resourcePath, resource);
	}

	private static void SaveFileIfResource(IRentedArray<byte> buffer, ResourceMagic magic, string path, string name) {
		if (buffer.Length < 4) {
			return;
		}

		var sp = buffer.Span;
		if (MemoryMarshal.Read<ResourceMagic>(sp) != magic) {
			return;
		}

		Directory.CreateDirectory(path);
		using var stream = new FileStream(Path.Combine(path, name), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
		stream.Write(sp);
	}

	public static void SaveFHM(FHMFile? fhm, string path) {
		if (fhm is null) {
			return;
		}

		var index = 0;
		foreach (var header in fhm.ItemHeaders) {
			if (header.Type == FHMItemType.Child) {
				using var child = fhm.GetChildItem(header);
				SaveFHM(child, path);
			} else {
				using var resource = Resource.Construct(fhm, header, index, fhm.Header.HashId.ToString(), true);
				SaveResource(path, resource);
			}

			index++;
		}
	}

	private static void SaveResource(string path, Resource? resource) {
		if (resource is null || resource.ResourceCount <= 0) {
			return;
		}

		Directory.CreateDirectory(path);

		for (var resourceIndex = 0; resourceIndex < resource.ResourceCount; ++resourceIndex) {
			var resourceName = resource.GetResourceName(resourceIndex, path);
			if (resourceName == null) {
				continue;
			}

			var resourcePath = Path.Combine(path, resourceName);
			using (var stream = new FileStream(resourcePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)) {
				if (resource.Save(stream, resourceIndex)) {
					continue;
				}
			}

			File.Delete(resourcePath);
		}
	}
}
