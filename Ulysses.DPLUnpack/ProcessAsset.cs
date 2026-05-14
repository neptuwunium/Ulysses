// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pluto.IO.Binary;
using Ulysses.Struct;

namespace Ulysses.DPLUnpack;

public static class ProcessAsset {
	public static JsonSerializerOptions JsonSettings { get; } = new() {
		WriteIndented = true,
		NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public static bool Convert(ResourceMagic magic, IRentedArray<byte> buffer, string path) {
		switch (magic) {
			case ResourceMagic.Table: {
				using var table = new LVSTableFile(buffer, true);
				using var stream = new FileStream(path + ".json", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
				JsonSerializer.Serialize(stream, table, JsonSettings);
				return true;
			}
			case ResourceMagic.Text: {
				using var text = new ACTextFile(buffer, true);
				using var stream = new FileStream(path + ".json", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
				JsonSerializer.Serialize(stream, text, JsonSettings);
				return true;
			}
			default: return false;
		}
	}
}
