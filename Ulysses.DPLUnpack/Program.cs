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
			 .WriteTo.File("Ulysses.log")
			 .CreateLogger();

var flags = CommandLineFlags.Singleton<ProgramFlags>.Instance;

using var mgr = ResourceManager.Instance;
mgr.Mount(flags.InputPath);
mgr.Collect();

CreateDirectory(flags.OutputPath);

if (flags.Merged) {
	foreach (var id in mgr.FHM.Keys) {
		using var buf = mgr.ReadFile(id, out var header);
		if (buf == null) {
			Log.Error("cannot export {HashId}", id);
			continue;
		}

		ExtractFHM(buf, header, flags.OutputPath);
	}
} else {
	foreach (var dpl in mgr.DPL.Values) {
		HashTracker.AddHashes(dpl);
		var output = Path.Combine(flags.OutputPath, dpl.Name);
		CreateDirectory(output);

		foreach (var (id, (_, header)) in dpl.FHMTable) {
			using var buf = dpl.ReadFile(id);
			if (buf == null) {
				Log.Error("{PacName}: cannot export {HashId}", dpl.Name, id);
				continue;
			}

			HashTracker.AddStrings(buf, true);
			ExtractFHM(buf, header, output);
		}
	}
}

if (flags.DumpDPL) {
	var infoTarget = Path.Combine(flags.OutputPath, "__ULYSSES_DPL_INFO");
	CreateDirectory(infoTarget);
	foreach (var dpl in mgr.DPL.Values) {
		using var stream = new StreamWriter(CreateFile(Path.Combine(infoTarget, dpl.Name + ".csv")));
		stream.NewLine = "\n";
		stream.WriteLine("id,group_id,name,deleted");
		foreach (var (_, header) in dpl.FHMTable.Values) {
			var hashStr = header.HashId.HasValue ? header.HashId.ToString() : string.Empty;
			var isDeleted = header.IsDeleted ? "yes" : "no";
			stream.WriteLine($"{header.HashId.Value:x08},{header.GroupId:x04},{hashStr},{isDeleted}");
		}
	}
}

if (flags.DumpHashes) {
	using (var stream = new StreamWriter(CreateFile("DplHash.txt", true))) {
		stream.NewLine = "\n";
		foreach (var hash in HashTracker.DPLHashes) {
			stream.WriteLine(hash.ToString("x8"));
		}
	}

	using (var stream = new StreamWriter(CreateFile("TextHash.txt", true))) {
		stream.NewLine = "\n";
		foreach (var hash in HashTracker.Hashes) {
			stream.WriteLine(hash.ToString("x8"));
		}
	}
}

if (flags.DumpStrings) {
	using var stream = new StreamWriter(CreateFile("Strings.txt", true));
	stream.NewLine = "\n";
	foreach (var str in HashTracker.Strings) {
		stream.WriteLine(str);
	}
}

return;

void CreateDirectory(string path, bool force = false) {
	if (flags.Dry && !force) {
		return;
	}

	Directory.CreateDirectory(path);
}

Stream CreateFile(string path, bool force = false) {
	if (flags.Dry && !force) {
		return Stream.Null;
	}

	return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
}

string GetExtension(ReadOnlySpan<byte> span) {
	if (span.Length < 4) {
		return ".bin";
	}

	var magic = MemoryMarshal.Read<ResourceMagic>(span);
	var ext = $"{magic.Ext}";
	return string.IsNullOrEmpty(ext) || ext[0] != '.' ? ".bin" : ext;
}

void ExtractFHM(IRentedArray<byte> buf, FHMHeader header, string output) {
	var hashStr = header.HashId.HasValue ? header.HashId.ToString() : $"DPL_0x{header.HashId.Value:x8}";

	var path = Path.Combine(output, hashStr);

	using var fhm = new FHMFile(buf, 0, header);

	if (flags.FHMShape) {
		var builder = ObjectPool<StringBuilder>.Rent();
		try {
			builder.Clear();
			builder.AppendLine($"Hash {fhm.ShapeHash():x16}");
			fhm.DumpShape(builder, 0);
			using var stream = new StreamWriter(CreateFile(path + ".fhmshape"));
			stream.Write(builder.ToString());
		} finally {
			builder.Clear();
			ObjectPool<StringBuilder>.Return(builder);
		}
	}

	if (flags.SaveFHM) {
		using var stream = CreateFile(path + ".fhm");
		stream.Write(buf.Span);

		if (flags.OnlyFHM) {
			Log.Information("Saved {Name}", Path.GetRelativePath(flags.OutputPath, path));
			return;
		}
	}

	ProcessFHM(fhm, output, hashStr, true);
}

void ProcessFHM(FHMFile fhm, string path, string name, bool isRoot) {
	// ReSharper disable once ConvertIfStatementToSwitchStatement
	if (isRoot && fhm.Count == 1) {
		var header = fhm.ItemHeaders.First();
		if (header.Type == FHMItemType.Normal) {
			ProcessFHMItem(fhm, header, path, name, 0);
			return;
		}
	}

	if (!isRoot && flags.SaveFHMBuffer) {
		using var fhmBuf = fhm.GetFullBuffer();
		if (fhmBuf.Length > 0) {
			var bufPath = $"{path}/{name}{GetExtension(fhmBuf.Span)}";
			var dir = Path.GetDirectoryName(bufPath)!;
			CreateDirectory(dir);
			using var stream = CreateFile(bufPath);
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

bool ProcessFHMItem(FHMFile fhm, FHMItemHeader itemHeader, string outputPath, string dplName, int itemIndex) {
	var name = $"{dplName}/{itemIndex}";

	if (flags.OnlyConvert) {
		var result = ProcessResource(fhm, itemHeader, dplName, outputPath, itemIndex);
		if (result) {
			return true;
		}

		if (itemHeader.Type == FHMItemType.Child) {
			ProcessFHMChild(fhm, itemHeader, outputPath, name);
		}

		return false;
	}

	if (itemHeader.Type == FHMItemType.Normal) {
		using var buf = fhm.GetItemData(itemHeader);
		if (buf.Length == 0) {
			return false;
		}

		HashTracker.AddData(buf);
		var path = Path.Combine(outputPath, name);
		var dir = Path.GetDirectoryName(path)!;
		CreateDirectory(dir);
		var ext = GetExtension(buf.Span);
		Log.Information("Saving {Path}", Path.GetRelativePath(flags.OutputPath, path + ext));
		using var stream = CreateFile(path + ext);
		stream.Write(buf.Span);
	} else {
		ProcessFHMChild(fhm, itemHeader, outputPath, name);
	}

	return ProcessResource(fhm, itemHeader, dplName, outputPath, itemIndex);
}

bool ProcessResource(FHMFile fhmFile, FHMItemHeader fhmItemHeader, string dplName, string path, int itemIndex) {
	if (flags is { Convert: false, OnlyConvert: false }) {
		return false;
	}

	using var resource = Resource.Construct(fhmFile, fhmItemHeader, itemIndex, dplName, true);

	if (resource is null || resource.ResourceCount <= 0) {
		return false;
	}

	var baseName = dplName;
	if (resource.ResourceCount > 1 || !resource.IsFullyUtilized) {
		baseName += $"/{itemIndex}";
	}

	for (var resourceIndex = 0; resourceIndex < resource.ResourceCount; ++resourceIndex) {
		var resourceName = resource.GetResourceName(resourceIndex, baseName);
		if (resourceName == null) {
			continue;
		}

		var resourcePath = Path.Combine(path, resourceName);
		var dir = Path.GetDirectoryName(resourcePath)!;
		CreateDirectory(dir);

		Log.Information("Saving {Path}", resourceName);
		using (var stream = CreateFile(resourcePath)) {
			if (resource.Save(stream, resourceIndex)) {
				continue;
			}
		}

		File.Delete(resourcePath);
		return false;
	}

	return resource.IsFullyUtilized;
}

void ProcessFHMChild(FHMFile fhmFile, FHMItemHeader fhmItemHeader, string path, string name) {
	using var child = fhmFile.GetChildItem(fhmItemHeader);
	if (child == null) {
		return;
	}

	ProcessFHM(child, path, name, false);
}
