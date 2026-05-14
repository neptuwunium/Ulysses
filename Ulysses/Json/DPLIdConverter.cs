// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text.Json;
using System.Text.Json.Serialization;
using Ulysses.Struct;

namespace Ulysses.Json;

public class DPLIdConverter : JsonConverter<DPLId> {
	public override DPLId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();
	public override void Write(Utf8JsonWriter writer, DPLId value, JsonSerializerOptions options) => writer.WriteStringValue(value.DebugString);
	public override void WriteAsPropertyName(Utf8JsonWriter writer, DPLId value, JsonSerializerOptions options) => writer.WritePropertyName(value.ToString());
}
