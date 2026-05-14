// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.CommandLine;
using Pluto.IO.FileSystem;
using Serilog;
using Ulysses;
using Ulysses.DPLUnpack;
using Ulysses.Struct;
using Ulysses.Struct.FHM;

Log.Logger = new LoggerConfiguration()
			 .MinimumLevel.Debug()
			 .WriteTo.Console()
			 .MinimumLevel.Debug()
			 .WriteTo.File("Ulysses.log")
			 .CreateLogger();

var flags = CommandLineFlags.Singleton<ProgramFlags>.Instance;

var unknownHashes = new HashSet<uint>();
var dplFiles = new List<(string, DPLFile)>();
foreach (var pacPath in new FileEnumerator(flags.InputPath, "*.PAC")) {
	var pacName = Path.GetFileNameWithoutExtension(pacPath);
	var pacNumber = pacName.Length > 4 ? int.Parse(pacName[4..]) : 0;
	switch (pacNumber) {
		case >= 10 and < 20: // skip audio
		case >= 20 and < 30: // skip video
		case 99: // skip region key
			continue;
	}

	dplFiles.Add((pacName, new DPLFile(pacPath)));
}

foreach (var (pacName, dpl) in dplFiles) {
	var output = Path.Combine(flags.OutputPath, pacName);
	Directory.CreateDirectory(output);

	foreach (var (id, (_, header)) in dpl.FHMTable) {
		var hashStr = header.HashId.GetDebugString("DPL");
		if (hashStr.StartsWith("DPL::[0x")) {
			hashStr = header.HashId.ToString();
			unknownHashes.Add(header.HashId.Value);
		}

		var path = Path.Combine(output, hashStr);
		using var buf = dpl.ReadFile(id);
		if (buf == null) {
			Log.Error("{PacName}: cannot export {HashId}", pacName, id);
			continue;
		}

		if (flags.SaveFHM && !File.Exists(path + ".fhm")) {
			using var stream = new FileStream(path + ".fhm", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			stream.Write(buf.Span);
		}

		// a fhm is basically anything. textures for example are split up into several slices.
		// need to check if fhm[0] is something we can read and then rebuild the original asset so it is easier to read
		using var fhm = new FHMFile(buf, 0, header);
		ProcessFHM(path, fhm);
	}

	dpl.Dispose();
}

Log.Debug("Writing all unknown hashes...");
using (var missingHashFile = new StreamWriter(new FileStream("DplHash.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))) {
	foreach (var hash in unknownHashes) {
		missingHashFile.WriteLine(hash.ToString("x8"));
	}
}

using (var missingHashFile = new StreamWriter(new FileStream("LvstHash.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))) {
	foreach (var hash in ProcessAsset.UnknownHashes) {
		missingHashFile.WriteLine(hash.ToString("x8"));
	}
}

Log.Debug("Writing all LVST strings...");
using (var strings = new StreamWriter(new FileStream("LvstStr.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))) {
	foreach (var str in ProcessAsset.LVSTStr) {
		strings.WriteLine(str);
	}
}
void ProcessFHM(string path, FHMFile fhm) {
	if (flags.Convert && fhm.Count > 0) {
		using var rebuiltFile = fhm.RebuildAsset(out var ext);
		if (rebuiltFile != null) {
			using var stream = new FileStream(path + (ext ?? ".bin"), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			Log.Information("Rebuilt {Name}", Path.GetRelativePath(flags.OutputPath, stream.Name));
			stream.Write(rebuiltFile.Span);

			if (flags.OnlyConvert || flags.ConvertOrRaw) {
				return;
			}
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

			if (flags.Convert) {
				var didConvert = ProcessAsset.Convert(magic, buf, currentPath);
				if (didConvert) {
					Log.Information("Converted {Path}", Path.GetRelativePath(flags.OutputPath, currentPath));
				}

				if (flags.OnlyConvert) {
					continue;
				}

				if (didConvert && flags.ConvertOrRaw) {
					continue;
				}
			}

			if (File.Exists(currentPath + ext)) {
				continue;
			}

			Log.Information("Saving {Path}", Path.GetRelativePath(flags.OutputPath, currentPath + ext));
			using var stream = new FileStream(currentPath + ext, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
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
