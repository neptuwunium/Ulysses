// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pluto.CommandLine;
using Pluto.IO.FileSystem;
using Ulysses;
using Ulysses.DPLUnpack;
using Ulysses.Struct;
using Ulysses.Struct.FHM;

var flags = CommandLineFlags.Singleton<ProgramFlags>.Instance;

var jsonSettings = new JsonSerializerOptions {
	WriteIndented = true,
	NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
};

foreach (var pacPath in new FileEnumerator(flags.InputPath, "*.PAC")) {
	var pacName = Path.GetFileNameWithoutExtension(pacPath);
	var pacNumber = pacName.Length > 4 ? int.Parse(pacName[4..]) : 0;
	switch (pacNumber) {
		case >= 10 and < 20: // skip audio
		case >= 20 and < 30: // skip video
		case 99: // skip region key
			continue;
	}

	var output = Path.Combine(flags.OutputPath, pacName);
	Directory.CreateDirectory(output);
	using var dpl = new DPLFile(pacPath);

	foreach (var (id, (_, header)) in dpl.FHMTable) {
		var path = Path.Combine(output, header.HashId.GetDebugString("DPL"));
		using var buf = dpl.ReadFile(id);
		if (buf == null) {
			Console.WriteLine($"{pacName}: cannot export {id}");
			continue;
		}
		Console.WriteLine($"{pacName}: {id}");

		if (flags.SaveFHM) {
			using var stream = new FileStream(path + ".fhm", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			stream.Write(buf.Span);
		}

		// a fhm is basically anything. textures for example are split up into several slices.
		// need to check if fhm[0] is something we can read and then rebuild the original asset so it is easier to read
		using var fhm = new FHMFile(buf, 0, header);
		ProcessFHM(path, fhm);
	}
}

return;

void ProcessFHM(string path, FHMFile fhm) {
	if (fhm.Count > 0) {
		using var rebuiltFile = fhm.RebuildAsset(out var ext);
		if (rebuiltFile != null) {
			using var stream = new FileStream(path + (ext ?? ".bin"), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			Console.WriteLine(stream.Name);
			stream.Write(rebuiltFile.Span);
			return;
		}
	}

	var idx = 0;
	foreach (var itemHeader in fhm.ItemHeaders) {
		var currentPath = Path.Combine(path, (idx++).ToString());

		if (itemHeader.Type == FHMItemType.Normal) {
			using var buf = fhm.GetItemData(itemHeader);
			if (buf.Length == 0) {
				continue;
			}
			var dir = Path.GetDirectoryName(currentPath)!;
			Directory.CreateDirectory(dir);
			var magic = buf.Length >= 4 ? MemoryMarshal.Read<ResourceMagic>(buf.Span) : 0;
			var ext = magic.Ext;
			if (ext.Length == 0 || ext[0] != '.') {
				ext = ".bin";
			}

			Console.WriteLine(Path.GetRelativePath(flags.OutputPath, currentPath + ext));

			if (flags.Convert) {
				var didConvert = true;
				switch (magic) {
					case ResourceMagic.Table: {
						using var table = new LVSTableFile(buf, true);
						using var stream = new FileStream(currentPath + ".json", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
						JsonSerializer.Serialize(stream, table, jsonSettings);
						break;
					}
					default:
						didConvert = false;
						break;
				}

				if (flags.OnlyConvert) {
					continue;
				}

				if (didConvert && flags.ConvertOrRaw) {
					continue;
				}
			}

			using (var stream = new FileStream(currentPath + ext, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)) {
				stream.Write(buf.Span);
			}
		} else {
			using var child = fhm.GetChildItem(itemHeader);
			if (child == null) {
				continue;
			}

			ProcessFHM(currentPath, child);
		}
	}
}
