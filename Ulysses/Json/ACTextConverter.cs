// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ulysses.Json;

public class ACTextConverter : JsonConverter<ACTextFile> {
	public override ACTextFile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

	public override void Write(Utf8JsonWriter writer, ACTextFile value, JsonSerializerOptions options) {
		writer.WriteStartObject();

		foreach (var (hash, info) in value.Hashes) {
			writer.WritePropertyName(info.Label);
			writer.WriteStartObject();

			var text = value.Texts[info.Index];
			writer.WriteString("Hash", $"0x{hash.Value:x8}");
			writer.WriteString("Label", text.Label);
			writer.WritePropertyName("Values");
			writer.WriteStartObject();

			foreach (var (language, index) in value.Languages) {
				writer.WriteString(language, value.GetStringForLanguage(text, index));
			}

			writer.WriteEndObject();
			writer.WriteEndObject();
		}

		writer.WriteEndObject();
	}
}
