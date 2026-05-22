// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using System.Text;
using Pluto.CommandLine;
using Pluto.IO.Binary;
using Ulysses.Resources.Data;
using Ulysses.Struct;
using Ulysses.Struct.LVST;

namespace Ulysses.DPLUnpack;

public static class HashTracker {
	public static HashSet<uint> Hashes { get; } = [];
	public static HashSet<uint> DPLHashes { get; } = [];
	public static HashSet<string> Strings { get; } = [];

	private static bool DumpHashes { get; } = CommandLineFlags.Singleton<ProgramFlags>.Instance.DumpStrings;
	private static bool DumpStrings { get; } = CommandLineFlags.Singleton<ProgramFlags>.Instance.DumpStrings;

	public static void AddHashes(DPLFile dplFile) {
		if (!DumpHashes) {
			return;
		}

		DPLHashes.UnionWith(dplFile.FHMTable.Keys.Select(x => x.Value));
	}

	public static void AddHashes(ACETableData table) {
		if (!DumpHashes) {
			return;
		}

		var info = table.ColumnInfos.Span;
		var ids = table.ColumnIds.Span;
		for (var index = 0; index < ids.Length; index++) {
			var columnId = ids[index];
			Hashes.Add(columnId);

			if (info[index].ColumnType != LVSTColumnType.Hash) {
				continue;
			}

			foreach (var hash in table.GetColumn(columnId).OfType<HashId>()) {
				Hashes.Add(hash);
			}
		}
	}

	public static void AddStrings(ACETextData text) {
		if (!DumpStrings) {
			return;
		}

		Strings.UnionWith(text.Hashes.Values.Select(x => x.Label));
	}

	public static void AddStrings(IRentedArray<byte> buffer, bool createCasing, int minLength = 5, int maxLength = 256) {
		if (!DumpStrings) {
			return;
		}

		var span = buffer.Span;
		var startIndex = 0;

		while (startIndex < span.Length) {
			var length = 0;

			// match [\s_a-zA-Z0-9]
			while (length < maxLength && length + startIndex < span.Length &&
				   (char.IsAsciiLetterOrDigit((char) span[length + startIndex]) || (char) span[length + startIndex] is '_')) {
				length++;
			}

			if (length >= minLength && minLength > 0) {
				var str = Encoding.ASCII.GetString(span.Slice(startIndex, length));
				Strings.Add(str);

				if (createCasing) {
					if (str[0] == '_') {
						str = str[1..];
						Strings.Add(str);
					}

					if (char.IsAsciiLetterLower(str[0])) {
						str = str[1..];
						var tmp = char.ToUpper(str[0]) + str[1..];
						Strings.Add(tmp);
						Strings.Add('_' + tmp);
					}

					if (char.IsAsciiLetterUpper(str[0])) {
						str = str[1..];
						var tmp = char.ToLower(str[0]) + str[1..];
						Strings.Add(tmp);
						Strings.Add('_' + tmp);
					}
				}
			}

			startIndex += length + 1;
		}
	}

	public static void AddStrings(ACETableData table) {
		if (!DumpStrings) {
			return;
		}

		var info = table.ColumnInfos.Span;
		var ids = table.ColumnIds.Span;
		for (var index = 0; index < info.Length; index++) {
			if (info[index].ColumnType != LVSTColumnType.String) {
				continue;
			}

			foreach (var str in table.GetColumn(ids[index]).OfType<string>()) {
				Strings.Add(str);
			}
		}
	}

	public static void AddData(IRentedArray<byte> buffer) {
		if (!DumpStrings && !DumpHashes) {
			return;
		}

		if (buffer.Length < 0x4) {
			return;
		}

		var magic = MemoryMarshal.Read<ResourceMagic>(buffer.Span);

		// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
		switch (magic) {
			case ResourceMagic.ACEText when !DumpStrings:
				return;
			case ResourceMagic.ACEText: {
				using var text = new ACETextData(buffer, true);
				AddStrings(text);
				break;
			}
			case ResourceMagic.ACETable: {
				using var table = new ACETableData(buffer, true);
				AddHashes(table);
				AddStrings(table);
				break;
			}
		}
	}
}
