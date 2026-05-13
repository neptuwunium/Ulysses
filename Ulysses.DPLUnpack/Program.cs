// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.IO.FileSystem;
using Ulysses;
using Ulysses.DPL;
using Ulysses.Struct.FHM;

if (args.Length < 2) {
	Console.WriteLine("Usage: Ulysses.DPLUnpack.exe <input> <output>");
	return;
}

foreach (var pacPath in new FileEnumerator(args[0], "*.PAC")) {
	var pacName = Path.GetFileNameWithoutExtension(pacPath);
	var pacNumber = pacName.Length > 4 ? int.Parse(pacName[4..]) : 0;
	switch (pacNumber) {
		case >= 10 and < 20: // skip audio
		case >= 20 and < 30: // skip video
		case 99: // skip region key
			continue;
	}

	var output = Path.Combine(args[1], pacName);
	Directory.CreateDirectory(output);
	using var dpl = new DPLFile(pacPath);

	foreach (var (id, (_, header)) in dpl.FHMTable) {
		var path = Path.Combine(output, header.ResourceId.DebugString);

		using var buf = dpl.ReadFile(id);
		if (buf == null) {
			Console.WriteLine($"{pacName}: cannot export {id}");
			continue;
		}
		Console.WriteLine($"{pacName}: {id}");

		#if DEBUG
		using var stream = new FileStream(path + ".fhm", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
		stream.Write(buf.Span);
		#endif

		// a fhm is basically anything. textures for example are split up into several slices.
		// need to check if fhm[0] is something we can read and then rebuild the original asset so it is easier to read
		using var fhm = new FHMAsset(buf, 0, header);
		ProcessFHM(path, fhm);
	}
}

return;

void ProcessFHM(string path, FHMAsset fhm) {
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
			var ext = (buf.Length >= 4 ? MemoryMarshal.Read<ResourceMagic>(buf.Span) : 0).Ext;
			if (ext.Length == 0 || ext[0] != '.') {
				ext = ".bin";
			}
			using var stream = new FileStream(currentPath + ext, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			Console.WriteLine(stream.Name);
			stream.Write(buf.Span);
		} else {
			using var child = fhm.GetChildItem(itemHeader);
			if (child == null) {
				continue;
			}

			ProcessFHM(currentPath, child);
		}
	}
}
