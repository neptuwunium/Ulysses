// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers.Binary;
using System.Globalization;
using Ulysses.Struct;

var dplHash = new HashSet<uint>();
var lvstHash = new HashSet<uint>();

using (var fs = new StreamReader(new FileStream("DplHash.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
	while (fs.ReadLine() is { } line) {
		line = line.Trim();
		if (line.Length != 8) {
			continue;
		}

		dplHash.Add(uint.Parse(line, NumberStyles.HexNumber));
	}
}

using (var fs = new StreamReader(new FileStream("LvstHash.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
	while (fs.ReadLine() is { } line) {
		line = line.Trim();
		if (line.Length != 8) {
			continue;
		}

		lvstHash.Add(BinaryPrimitives.ReverseEndianness(uint.Parse(line, NumberStyles.HexNumber)));
	}
}

for (var i = 0; i < 1000; i++) {
	TestDplHash($"DPL_EMBLEM_{i:D3}");
	TestDplHash($"DPL_2DIMAGE_EMB_{i:D3}");
	TestDplHash($"DPL_2DIMAGE_EMBL_{i:D3}");
}

for (var i = 0; i < 100; i++) {
	TestDplHash($"DPL_DLC_CATALOG_{i}");
	TestDplHash($"DPL_2DIMAGE_GIFT_{i}");
	TestDplHash($"DPL_2DIMAGE_PARTS_{i}");
	TestDplHash($"DPL_2DIMAGE_GUIDE_{i}");
	TestDplHash($"DPL_UI_LOADING_TIP{i}");
	TestDplHash($"DPL_MICP{i:D2}_RES");
	TestDplHash($"DPL_MICP{i:D2}_SCRIPT");
	TestDplHash($"DPL_MICP{i:D2}_LUASCRIPT");
	TestDplHash($"DPL_P_USERDATA_T{i:D2}");
}

TestDplHash("DPL_DEVELOPMENT");
TestDplHash("DPL_INFORMATION");
TestDplHash("DPL_TSS_MISC");
TestDplHash("DPL_TSS_INFO");
TestDplHash("DPL_TSS_DROP_ITEM");
TestDplHash("DPL_TSS_SALES_LIST");
TestDplHash("DPL_TSS_ITEM");
TestDplHash("DPL_UI_STARTUP");
TestDplHash("DPL_UI_TSS_COMMON");
TestDplHash("DPL_UI_TSS_MENU");
TestDplHash("DPL_MAP");
TestDplHash("DPL_MAP_MPT");
TestDplHash("DPL_RES");
TestDplHash("DPL_SCRIPT");
TestDplHash("DPL_LUASCRIPT");
TestDplHash("DPL_CINEMA");
TestDplHash("DPL_CINEMA_S01");
TestDplHash("DPL_COMMON");
TestDplHash("DPL_MAP_COMMON");
TestDplHash("DPL_POSTPROCESS");

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

Parallel.ForEach(lines, TestLvstHashFormat);

Parallel.ForEach(linesUpper, TestDplHashFormat);

return;

void TestDplHash(string text) {
	var hash = IdRegistry.Hash(text);
	if (dplHash.Contains(hash)) {
		Console.WriteLine($"DPL,{hash:x8},{text}");
	}
}

void TestLvstHash(string text) {
	var hash = IdRegistry.Hash(text);
	if (lvstHash.Contains(hash)) {
		Console.WriteLine($"LVST,{hash:x8},{text}");
	}
}

void TestLvstHashFormat(string text) {
	TestLvstHash(text);
	TestLvstHash(text + "Hash");
	TestLvstHash(text + "FileId");
	TestLvstHash(text + "File");
	TestLvstHash(text + "DPL");
	TestLvstHash(text + "Id");
	TestLvstHash(text + "Dpl");
	TestLvstHash(text + "ID");
	TestLvstHash(text + "MsgId");
	TestLvstHash(text + "IdStr");
	TestLvstHash(text + "IDStr");
}

void TestDplHashFormat(string text) {
	text = text.ToUpper();
	TestDplHash("DPL_" + text);
	for (var i = 0; i < 100; i++) {
		TestDplHash($"DPL_P_{text}_T{i:D2}");
		TestDplHash($"DPL_P_{text}_E{i:D2}");
		TestDplHash($"DPL_D_{text}_T{i:D2}");
		TestDplHash($"DPL_D_{text}_E{i:D2}");
	}

	TestDplHash($"DPL_P_{text}_EMBLEM");
	TestDplHash($"DPL_D_{text}");
	TestDplHash($"DPL_D_{text}_EMBLEM");
	TestDplHash($"DPL_P_{text}_K");
	TestDplHash($"DPL_P_{text}");
	TestDplHash($"DPL_MAP_{text}");
	TestDplHash($"DPL_MAP_{text}_MPT");
	TestDplHash($"DPL_{text}_RES");
	TestDplHash($"DPL_{text}_SCRIPT");
	TestDplHash($"DPL_{text}_LUASCRIPT");
	TestDplHash($"DPL_CINEMA_{text}");
	TestDplHash($"DPL_CINEMA_{text}_S01");
	TestDplHash($"DPL_CINEMA_{text}");
}
