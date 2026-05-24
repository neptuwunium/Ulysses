// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Globalization;
using System.Reflection;
using System.Text;
using Charon.Hash;
using Charon.Hash.Basis;

namespace Ulysses.Struct;

public static class IdRegistry {
	static IdRegistry() {
		ParseTypeFile("Names/DPL.name");
		ParseTypeFile("Names/LVST.name");
		ParseTypeFile("Names/ACT.name");
		ParseMissingFile("Names/MISSING.name");
	}

	public static Dictionary<uint, string> Lookup { get; } = [];
	public static bool Freeze { get; set; }

	public static uint Hash(string name) {
		var text = (stackalloc byte[Encoding.UTF8.GetMaxByteCount(name.Length)]);
		var n = Encoding.UTF8.GetBytes(name, text);
		return CRC.HashData(CRC32Variants.BZip2, text[..n]);
	}

	private static void ParseMissingFile(string path) {
		var asm = Assembly.GetExecutingAssembly();
		using var resource = asm.GetManifestResourceStream($"{asm.GetName().Name!}.{path.Replace('/', '.')}");
		if (resource != null) {
			ParseMissingFile(resource);
		}

		if (!File.Exists(path)) {
			return;
		}

		using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		ParseMissingFile(file);
	}

	private static void ParseTypeFile(string path) {
		var asm = Assembly.GetExecutingAssembly();
		using var resource = asm.GetManifestResourceStream($"{asm.GetName().Name!}.{path.Replace('/', '.')}");
		if (resource != null) {
			ParseTypeFile(resource);
		}

		if (!File.Exists(path)) {
			return;
		}

		using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		ParseTypeFile(file);
	}

	public static void ParseMissingFile(Stream stream) {
		if (Freeze) {
			return;
		}

		using var reader = new StreamReader(stream);

		while (!reader.EndOfStream) {
			var line = reader.ReadLine()?.Trim();

			if (string.IsNullOrEmpty(line)) {
				continue;
			}

			var lineSpan = line.AsSpan();
			if (lineSpan.Length < 13) {
				continue;
			}

			var hashSpan = lineSpan[..0x8];
			if (!uint.TryParse(hashSpan, NumberStyles.HexNumber, null, out var hash)) {
				continue;
			}

			var name = new string(lineSpan[0x9..]) + $"_UNKNOWN_0x{hash:x8}";

			Lookup[hash] = name;
		}
	}

	public static void ParseTypeFile(Stream stream) {
		if (Freeze) {
			return;
		}

		using var reader = new StreamReader(stream);

		while (!reader.EndOfStream) {
			var line = reader.ReadLine()?.Trim();

			if (string.IsNullOrEmpty(line)) {
				continue;
			}

			Lookup[Hash(line)] = line;
		}
	}

	public static uint Register(string name) {
		var value = Hash(name);

		if (!Freeze) {
			Lookup.TryAdd(value, name);
		}

		return value;
	}

	public static void Register(HashId value, string name) {
		if (!Freeze) {
			Lookup.TryAdd(value, name);
		}
	}
}
