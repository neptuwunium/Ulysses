// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Globalization;
using Ulysses.Struct;

var dplHash = new HashSet<uint>();
var textHash = new HashSet<uint>();

if (File.Exists("DplHash.txt")) {
	LoadHashFile("DplHash.txt", dplHash);
}

if (File.Exists("TextHash.txt")) {
	LoadHashFile("TextHash.txt", textHash);
}

foreach (var id in IdRegistry.Lookup.Keys) {
	dplHash.Remove(id);
	textHash.Remove(id);
}

var lines = new HashSet<string>();
var linesUpper = new HashSet<string>();
using (var fs = new StreamReader(new FileStream("names.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
	while (fs.ReadLine() is { } line) {
		if (line.Length is > 0 and < 0x200 && line.All(x => x == ' ' || char.IsAsciiLetterOrDigit(x) || x == '_')) {
			lines.Add(line);
			linesUpper.Add(line.ToUpperInvariant());
		}
	}
}

linesUpper.RemoveWhere(x => x.Contains(' ', StringComparison.Ordinal) || x[0] is '_' || x[^1] is '_');

using (var fs = new StreamReader(new FileStream("Strings.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
	while (fs.ReadLine() is { } line) {
		line = line.Trim();
		if (line.Length < 5) {
			continue;
		}

		lines.Add(line);
		linesUpper.Add(line.ToUpperInvariant());
	}
}

if (textHash.Count > 0) {
	Parallel.ForEach(lines, TestLvstHashFormatOuter);
}

if (dplHash.Count > 0) {
	Parallel.ForEach(linesUpper, TestDplHashFormatRecurOuter);
}

return;

void LoadHashFile(string file, HashSet<uint> output) {
	using var fs = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
	while (fs.ReadLine() is { } line) {
		line = line.Trim();
		if (line.Length != 8) {
			continue;
		}

		output.Add(uint.Parse(line, NumberStyles.HexNumber));
	}
}

void TestDplHash(string text) {
	var hash = IdRegistry.Hash(text);
	if (dplHash.Contains(hash)) {
		Console.WriteLine($"DPL,{hash:x8},{text}");
	}
}

void TestLvstHash(string text) {
	var hash = IdRegistry.Hash(text);
	if (textHash.Contains(hash)) {
		Console.WriteLine($"LVST,{hash:x8},{text}");
	}
}

void TestLvstHashFormat(string text) {
	TestLvstHash(text);
	TestLvstHash(text + "Hash");
	TestLvstHash(text + "HashID");
	TestLvstHash(text + "FileId");
	TestLvstHash(text + "FileID");
	TestLvstHash(text + "File");
	TestLvstHash(text + "DPL");
	TestLvstHash(text + "DPLID");
	TestLvstHash(text + "Id");
	TestLvstHash(text + "Dpl");
	TestLvstHash(text + "ID");
	TestLvstHash(text + "MsgId");
	TestLvstHash(text + "MsgID");
	TestLvstHash(text + "IdStr");
	TestLvstHash(text + "IDStr");
}

void TestLvstHashFormatRecur(string text, int nest) {
	foreach (var piece in lines) {
		var pt = text + piece;
		TestLvstHashFormat(pt);

		if (nest > 0) {
			TestLvstHashFormatRecur(pt + "_", nest - 1);
		}
	}
}

void TestLvstHashFormatOuter(string text) {
	TestLvstHashFormat(text);
	TestLvstHashFormatRecur(text + "_", 0);
}


void TestDplHashFormat(string text) {
	TestDplHash("DPL_" + text);
	TestDplHash("DPL_UI_" + text);
	TestDplHash("DPL_TSS_" + text);
	TestDplHash("DPL_UI_TSS_" + text);
	TestDplHash("DPL_MAP_" + text);
	TestDplHash("DPL_2DIMAGE_" + text);

	for (var i = 0; i < 99; ++i) {
		TestDplHash("DPL_MAP_" + text + $"{i:D2}");
		TestDplHash("DPL_" + text + $"{i:D2}");
	}

	for (var i = 0; i < 3000; ++i) {
		TestDplHash("DPL_2DIMAGE_" + text + $"_{i}");
	}
}

void TestDplHashFormatRecur(string text, int nest) {
	foreach (var piece in linesUpper) {
		var pt = text + piece;
		TestDplHashFormat(pt);

		if (nest > 0) {
			TestDplHashFormatRecur(pt + "_", nest - 1);
		}
	}
}

void TestDplHashFormatRecurOuter(string text) {
	TestDplHashFormat(text);
	TestDplHashFormatRecur(text + "_", 0);
}
