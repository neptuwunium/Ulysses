// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using System.Text;
using Pluto;
using Pluto.CommandLine;
using Pluto.IO.Binary;
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
using var mgr = ResourceManager.Instance;
mgr.Mount(flags.InputPath);
mgr.Collect();

if (flags.Merged) {
	foreach (var id in mgr.FHM.Keys) {
		Directory.CreateDirectory(flags.OutputPath);

		using var buf = mgr.ReadFile(id, out var header);
		if (buf == null) {
			Log.Error("cannot export {HashId}", id);
			continue;
		}

		ExtractFHM(buf, header, flags.OutputPath);
	}
} else {
	foreach (var dpl in mgr.DPL.Values) {
		var output = Path.Combine(flags.OutputPath, dpl.Name);
		Directory.CreateDirectory(output);

		foreach (var (id, (_, header)) in dpl.FHMTable) {
			using var buf = dpl.ReadFile(id);
			if (buf == null) {
				Log.Error("{PacName}: cannot export {HashId}", dpl.Name, id);
				continue;
			}

			ExtractFHM(buf, header, output);
		}
	}
}

Log.Debug("Writing all unknown hashes...");
if (unknownHashes.Count > 0) {
	using var missingHashFile = new StreamWriter(new FileStream("DplHash.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
	foreach (var hash in unknownHashes) {
		missingHashFile.WriteLine(hash.ToString("x8"));
	}
}

if (ProcessAsset.UnknownHashes.Count > 0) {
	using var missingHashFile = new StreamWriter(new FileStream("LvstHash.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
	foreach (var hash in ProcessAsset.UnknownHashes) {
		missingHashFile.WriteLine(hash.ToString("x8"));
	}
}

return;

void ExtractFHM(IRentedArray<byte> buf, FHMHeader header, string output) {
	var hashStr = header.HashId.GetDebugString("DPL");
	if (hashStr.StartsWith("DPL::[0x")) {
		hashStr = header.HashId.ToString();
		unknownHashes.Add(header.HashId.Value);
	}

	var path = Path.Combine(output, hashStr);

	// a fhm is basically anything. textures for example are split up into several slices.
	// need to check if fhm[0] is something we can read and then rebuild the original asset so it is easier to read
	using var fhm = new FHMFile(buf, 0, header);

	if (flags.FHMShape) {
		var builder = ObjectPool<StringBuilder>.Rent();
		builder.Clear();
		builder.AppendLine($"Hash {fhm.ShapeHash():x16}");
		fhm.DumpShape(builder);
		using var stream = new StreamWriter(new FileStream(path + ".fhmshape", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
		stream.Write(builder.ToString());
		builder.Clear();
		ObjectPool<StringBuilder>.Return(builder);
	}

	if (flags.SaveFHM) {
		using var stream = new FileStream(path + ".fhm", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
		stream.Write(buf.Span);

		if (flags.OnlyFHM) {
			Log.Information("Saved {Name}", Path.GetRelativePath(flags.OutputPath, path));
			return;
		}
	}

	ProcessFHM(path, fhm, true);
}

void ProcessFHM(string path, FHMFile fhm, bool isRoot = false) {
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

	if (isRoot && fhm.Count == 1) {
		var header = fhm.ItemHeaders.First();
		if (header.Type == FHMItemType.Normal) {
			ProcessFHMItem(header, path);
			return;
		}
	}

	if (!isRoot && flags.SaveFHMBuffer) {
		using var fhmBuf = fhm.GetFullBuffer();
		if (fhmBuf != null) {
			var magic = fhmBuf.Length >= 4 ? MemoryMarshal.Read<ResourceMagic>(fhmBuf.Span) : 0;
			var ext = magic.Ext;
			if (ext.Length == 0 || ext[0] != '.') {
				ext = ".bin";
			}
			var dir = Path.GetDirectoryName(path)!;
			Directory.CreateDirectory(dir);
			using var stream = new FileStream(path + ext, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			stream.Write(fhmBuf.Span);
		}
	}

	var idx = 0;
	foreach (var itemHeader in fhm.ItemHeaders) {
		ProcessFHMItem(itemHeader, Path.Combine(path, (idx++).ToString()));
	}

	return;

	void ProcessFHMItem(FHMItemHeader itemHeader, string currentPath) {
		if (itemHeader.Type == FHMItemType.Normal) {
			using var buf = fhm.GetItemData(itemHeader);
			if (buf.Length == 0) {
				return;
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
					return;
				}

				if (didConvert && flags.ConvertOrRaw) {
					return;
				}
			}

			Log.Information("Saving {Path}", Path.GetRelativePath(flags.OutputPath, currentPath + ext));
			using var stream = new FileStream(currentPath + ext, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			stream.Write(buf.Span);
		} else {
			using var child = fhm.GetChildItem(itemHeader);
			if (child == null) {
				return;
			}

			ProcessFHM(currentPath, child);
		}
	}
}
