// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Reflection;
using System.Text;
using Charon.Hash;
using Charon.Hash.Basis;

namespace Ulysses.Struct;

public static class IdRegistry {
	public static uint Hash(string name) {
		var text = (stackalloc byte[Encoding.UTF8.GetMaxByteCount(name.Length)]);
		var n = Encoding.UTF8.GetBytes(name, text);
		return CRC.HashData(CRC32Variants.BZip2, text[..n]);
	}

	static IdRegistry() {
		ParseTypeFile("Resources/DPL.name", DPLLookup);
		ParseTypeFile("Resources/LVST.name", LVSTLookup);
	}

	public static Dictionary<uint, string> DPLLookup { get; } = [];
	public static Dictionary<uint, string> LVSTLookup { get; } = [];
	public static bool Freeze { get; set; }

	private static void ParseTypeFile(string path, Dictionary<uint, string> lookup) {
		var asm = Assembly.GetExecutingAssembly();
		using var resource = asm.GetManifestResourceStream($"{asm.GetName().Name!}.{path.Replace('/', '.')}");
		if (resource != null) {
			ParseTypeFile(resource, lookup);
		}

		if (!File.Exists(path)) {
			return;
		}

		using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		ParseTypeFile(file, lookup);
	}

	public static void ParseTypeFile(Stream stream, Dictionary<uint, string> lookup) {
		if (Freeze) {
			return;
		}

		using var reader = new StreamReader(stream);

		while (!reader.EndOfStream) {
			var line = reader.ReadLine()?.Trim();

			if (string.IsNullOrEmpty(line)) {
				continue;
			}

			lookup[Hash(line)] = line;
		}
	}
}
