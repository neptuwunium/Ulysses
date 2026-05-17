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
using Ulysses.Resources;
using Ulysses.Struct;
using Ulysses.Struct.FHM;

Log.Logger = new LoggerConfiguration()
			 .MinimumLevel.Debug()
			 .WriteTo.Console()
			 .MinimumLevel.Debug()
			 .WriteTo.File("Ulysses.log")
			 .CreateLogger();

var flags = CommandLineFlags.Singleton<ProgramFlags>.Instance;

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

return;

void ExtractFHM(IRentedArray<byte> buf, FHMHeader header, string output) {
	var hashStr = header.HashId.GetDebugString("DPL");
	if (hashStr.StartsWith("DPL::[0x")) {
		hashStr = header.HashId.ToString();
	}

	var path = Path.Combine(output, hashStr);

	// a fhm is basically anything. textures for example are split up into several slices.
	// need to check if fhm[0] is something we can read and then rebuild the original asset so it is easier to read
	using var fhm = new FHMFile(buf, 0, header);

	if (flags.FHMShape) {
		var builder = ObjectPool<StringBuilder>.Rent();
		builder.Clear();
		builder.AppendLine($"Hash {fhm.ShapeHash():x16}");
		fhm.DumpShape(builder, 0);
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

	ProcessFHM(fhm, output, hashStr, true);
}

void ProcessFHM(FHMFile fhm, string path, string name, bool isRoot) {
	if (isRoot && fhm.Count == 1) {
		var header = fhm.ItemHeaders.First();
		if (header.Type == FHMItemType.Normal) {
			ProcessFHMItem(fhm, header, path, name, 0);
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

			var bufPath = $"{path}/{name}{ext}";
			var dir = Path.GetDirectoryName(bufPath)!;
			Directory.CreateDirectory(dir);
			using var stream = new FileStream(bufPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			stream.Write(fhmBuf.Span);
		}
	}

	var idx = 0;
	foreach (var itemHeader in fhm.ItemHeaders) {
		if (ProcessFHMItem(fhm, itemHeader, path, name, idx++)) {
			break;
		}
	}
}

bool ProcessFHMItem(FHMFile fhm, FHMItemHeader itemHeader, string path, string name, int itemIndex) {
	if (flags.Convert) {
		using var resource = Resource.Construct(fhm, itemHeader, itemIndex, name, true);

		if (resource is not null && resource.ResourceCount > 0) {
			var didConvert = true;

			for (var resourceIndex = 0; resourceIndex < resource.ResourceCount; ++resourceIndex) {
				var resourceName = resource.GetResourceName(resourceIndex, resource.ResourceCount == 1 && (fhm.Count == 1 || resource.IsFullyUtilized) ? string.Empty : $"/{resourceIndex}");
				if (resourceName == null) {
					continue;
				}

				var resourcePath = Path.Combine(path, resourceName);
				var dir = Path.GetDirectoryName(resourcePath)!;
				Directory.CreateDirectory(dir);

				Log.Information("Saving {Path}", resourceName);
				using var stream = new FileStream(resourcePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
				if (!resource.Save(stream, resourceIndex)) {
					File.Delete(resourcePath);
					didConvert = false;
					break;
				}
			}

			if (flags.ConvertOrRaw && didConvert) {
				return resource.IsFullyUtilized;
			}
		}
	}

	name = $"{name}/{itemIndex}";

	if (itemHeader.Type == FHMItemType.Normal) {
		if (flags.OnlyConvert) {
			return false;
		}

		using var buf = fhm.GetItemData(itemHeader);
		if (buf.Length == 0) {
			return false;
		}

		path = Path.Combine(path, name);
		var dir = Path.GetDirectoryName(path)!;
		Directory.CreateDirectory(dir);
		var magic = buf.Length >= 4 ? MemoryMarshal.Read<ResourceMagic>(buf.Span) : 0;
		var ext = magic.Ext;
		if (ext.Length == 0 || ext[0] != '.') {
			ext = ".bin";
		}

		Log.Information("Saving {Path}", Path.GetRelativePath(flags.OutputPath, path + ext));
		using var stream = new FileStream(path + ext, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
		stream.Write(buf.Span);
	} else {
		using var child = fhm.GetChildItem(itemHeader);
		if (child == null) {
			return false;
		}

		ProcessFHM(child, path, name, false);
	}

	return false;
}
