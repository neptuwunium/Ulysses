// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text.Json;
using System.Text.Json.Serialization;
using Ulysses.Struct;

namespace Ulysses.Json;

public class ACEDateConverter : JsonConverter<ACEDate> {
	public override ACEDate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var value = JsonSerializer.Deserialize<DateTimeOffset>(ref reader, options);
		return (uint) (value.Year * 10000 + value.Month * 100 + value.Day);
	}

	public override void Write(Utf8JsonWriter writer, ACEDate value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value.DateTime, options);
}
