// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text.Json;
using System.Text.Json.Serialization;
using Pluto;
using Ulysses.Resources.Data;
using Ulysses.Struct;

namespace Ulysses.Json;

public class ACETableConverter : JsonConverter<ACETableData> {
	public override ACETableData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

	public override void Write(Utf8JsonWriter writer, ACETableData value, JsonSerializerOptions options) {
		writer.WriteStartArray();

		var obj = ObjectPool<Dictionary<HashId, object?>>.Rent();
		foreach (var info in value.GetRows(obj)) {
			JsonSerializer.Serialize(writer, info, options);
		}

		obj.Clear();
		ObjectPool<Dictionary<HashId, object?>>.Return(obj);

		writer.WriteEndArray();
	}
}
