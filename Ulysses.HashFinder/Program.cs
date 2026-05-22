// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Globalization;
using Ulysses.Struct;

var dplHash = new HashSet<uint>();
var textHash = new HashSet<uint>();
var dplPrefix = IdRegistry.Lookup.Where(x => x.Value.StartsWith("DPL_")).Select(x => x.Value).ToHashSet();

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

if (dplHash.Count > 0) {
	for (var i = 0; i < 1000; i++) {
		TestDplHash($"DPL_EMBLEM_{i:D3}");
		TestDplHash($"DPL_2DIMAGE_EMB_{i:D3}");
		TestDplHash($"DPL_2DIMAGE_EMBL_{i:D3}");
		TestDplHash($"DPL_2DIMAGE_AC_{i:D}");
		TestDplHash($"DPL_2DIMAGE_MIS_{i:D}");
	}

	for (var i = 0; i < 10000; i++) {
		TestDplHash($"DPL_2DIMAGE_GIFT_{i:D4}");
	}

	for (var i = 0; i < 100; i++) {
		TestDplHash($"DPL_DLC_CATALOG_{i}");
		TestDplHash($"DPL_2DIMAGE_GIFT_{i}");
		TestDplHash($"DPL_2DIMAGE_PARTS_{i}");
		TestDplHash($"DPL_2DIMAGE_GUIDE_{i}");
		TestDplHash($"DPL_2DIMAGE_TIPS_{i}");
		TestDplHash($"DPL_UI_LOADING_TIP{i}");
		for (var l = 'A'; l <= 'Z'; ++l) {
			TestDplHash($"DPL_MIC{l}{i:D2}_RES");
			TestDplHash($"DPL_MIC{l}{i:D2}_SCRIPT");
			TestDplHash($"DPL_MIC{l}{i:D2}_LUASCRIPT");
		}

		TestDplHash($"DPL_P_USERDATA_T{i:D2}");
		TestDplHash($"DPL_MAP_MS{i:D2}");
		TestDplHash($"DPL_MAP_MS{i:D2}_MPT");
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

	TestDplHash("DPL_CINEMA_CD_HANGAR_04");
	TestDplHash("DPL_CINEMA_CD_HANGAR_04_S01");

	for (var i = 0; i < 100; ++i) {
		for (var j = 0; j < 100; ++j) {
			for (var l = 'A'; l <= 'Z'; ++l) {
				for (var l2 = 'A'; l2 <= 'Z'; ++l2) {
					TestDplHash($"DPL_CINEMA_DD_MIC{l}{i:D2}_{j:D2}{l2}");
					TestDplHash($"DPL_CINEMA_DD_MIC{l}{i:D2}_{j:D2}{l2}_S01");
					TestDplHash($"DPL_CINEMA_DD_MIT{l}{i:D2}_{j:D2}{l2}");
					TestDplHash($"DPL_CINEMA_DD_MIT{l}{i:D2}_{j:D2}{l2}_S01");
				}
			}
		}
	}
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
	Parallel.ForEach(lines, TestLvstHashFormat);
}

if (dplHash.Count > 0) {
	Parallel.ForEach(linesUpper, TestDplHashFormat);
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
		TestDplHash($"DPL_CINEMA_{text}_VO_{i:D}");
		TestDplHash($"DPL_CINEMA_{text}_VO_{i:D3}");
	}

	foreach (var prefix in dplPrefix) {
		TestDplHash($"{prefix}_{text}");
		TestDplHash($"{prefix}_VO_{text}");
		TestDplHash($"{prefix}_SE_{text}");
		TestDplHash($"{prefix}_VO_S01_{text}");
		TestDplHash($"{prefix}_SE_S01_{text}");
	}

	TestDplHash($"DPL_UI_{text}");
	TestDplHash($"DPL_UI_{text}_1");
	TestDplHash($"DPL_2DIMAGE_{text}");
	TestDplHash($"DPL_2DIMAGE_{text}_1");
	TestDplHash($"DPL_2DIMAGE_{text}_01");
	TestDplHash($"DPL_2DIMAGE_{text}_001");
	TestDplHash($"DPL_2DIMAGE_{text}_0001");
	TestDplHash($"DPL_UI_{text}");
	TestDplHash($"DPL_UI_{text}_1");
	TestDplHash($"DPL_UI_{text}_01");
	TestDplHash($"DPL_UI_{text}_001");
	TestDplHash($"DPL_UI_{text}_0001");

	TestDplHash($"DPL_P_{text}_EMBLEM");
	TestDplHash($"DPL_D_{text}");
	TestDplHash($"DPL_D_{text}_EMBLEM");
	TestDplHash($"DPL_P_{text}_K");
	TestDplHash($"DPL_P_{text}");
	TestDplHash($"DPL_MAP_{text}");
	TestDplHash($"DPL_MAP_{text}_MPT");
	TestDplHash($"DPL_BGM_{text}");
	TestDplHash($"DPL_{text}_BGM");
	TestDplHash($"DPL_{text}_RES");
	TestDplHash($"DPL_{text}_SCRIPT");
	TestDplHash($"DPL_{text}_LUASCRIPT");
	TestDplHash($"DPL_CINEMA_{text}_S01");
	TestDplHash($"DPL_CINEMA_{text}");
	TestDplHash($"DPL_CINEMA_DD_{text}_S01");
	TestDplHash($"DPL_CINEMA_DD_{text}");
	TestDplHash($"DPL_CINEMA_CD_{text}_S01");
	TestDplHash($"DPL_CINEMA_CD_{text}");
	TestDplHash($"DPL_CINEMA_DD_{text}_VO_S01");
	TestDplHash($"DPL_CINEMA_DD_{text}_VO");
	TestDplHash($"DPL_CINEMA_CD_{text}_SE_S01");
	TestDplHash($"DPL_CINEMA_CD_{text}_SE");
	TestDplHash($"DPL_CINEMA_DD_{text}_01a_S01");
	TestDplHash($"DPL_CINEMA_DD_{text}_01a");
	TestDplHash($"DPL_CINEMA_CD_{text}_01a_S01");
	TestDplHash($"DPL_CINEMA_CD_{text}_01a");
	TestDplHash($"DPL_CINEMA_DD_{text}_01a_VO_S01");
	TestDplHash($"DPL_CINEMA_DD_{text}_01a_VO");
	TestDplHash($"DPL_CINEMA_CD_{text}_01a_SE_S01");
	TestDplHash($"DPL_CINEMA_CD_{text}_01a_SE");
	TestDplHash($"DPL_{text}_S01");
	TestDplHash($"DPL_DD_{text}_S01");
	TestDplHash($"DPL_DD_{text}");
	TestDplHash($"DPL_CD_{text}_S01");
	TestDplHash($"DPL_CD_{text}");
	TestDplHash($"DPL_DD_{text}_VO_S01");
	TestDplHash($"DPL_DD_{text}_VO");
	TestDplHash($"DPL_CD_{text}_SE_S01");
	TestDplHash($"DPL_CD_{text}_SE");
	TestDplHash($"DPL_DD_{text}_01a_S01");
	TestDplHash($"DPL_DD_{text}_01a");
	TestDplHash($"DPL_CD_{text}_01a_S01");
	TestDplHash($"DPL_CD_{text}_01a");
	TestDplHash($"DPL_DD_{text}_01a_VO_S01");
	TestDplHash($"DPL_DD_{text}_01a_VO");
	TestDplHash($"DPL_CD_{text}_01a_SE_S01");
	TestDplHash($"DPL_CD_{text}_01a_SE");
}
