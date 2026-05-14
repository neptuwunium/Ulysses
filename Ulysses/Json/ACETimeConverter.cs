// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text.Json;
using System.Text.Json.Serialization;
using Ulysses.Struct;

namespace Ulysses.Json;

public class ACETimeConverter : JsonConverter<ACETime> {
	public override ACETime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var value = JsonSerializer.Deserialize<TimeSpan>(ref reader, options);
		return (uint) value.TotalSeconds;
	}

	public override void Write(Utf8JsonWriter writer, ACETime value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value.TimeSpan, options);
}
