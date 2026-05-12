// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Pluto.IO.FileSystem;
using Ulysses.DPL;
using Ulysses.DPL.Struct;

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
		var path = Path.Combine(output, $"{header.GroupId}_0x{id.Value:x08}");

		using var buf = dpl.ReadFile(id);
		if (buf == null) {
			Console.WriteLine($"{pacName}: cannot export {id}");
			continue;
		}
		Console.WriteLine($"{pacName}: {id}");

		using var fhm = new FHMAsset(buf, 0, header);
		ProcessFHM(path, fhm);
	}
}

return;

void ProcessFHM(string path, FHMAsset fhm) {
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
			using var stream = new FileStream(currentPath + ".bin", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			Console.WriteLine(currentPath);
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
