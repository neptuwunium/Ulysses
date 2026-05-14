// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Diagnostics;
using System.Text.Json.Serialization;
using Pluto.SourceGen.ReverseEndiannessGenerator;
using Pluto.SourceGen.TransparentStructGenerator;
using Ulysses.Json;

namespace Ulysses.Struct;

[TransparentStruct<uint>, EndianSwappable, DebuggerDisplay("{DebugString}"), JsonConverter(typeof(ResourceIdConverter))]
public partial struct ResourceId {
	public ResourceId(string name) => IdRegistry.LVSTLookup[Value = IdRegistry.Hash(name)] = name;
	public override string ToString() => IdRegistry.LVSTLookup.TryGetValue(Value, out var name) ? name : $"0x{Value:x08}";
	public string DebugString => $"{IdRegistry.LVSTLookup.GetValueOrDefault(Value, "Resource")}::[0x{Value:x08}]";
}
