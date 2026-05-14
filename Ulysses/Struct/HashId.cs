// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Diagnostics;
using System.Text.Json.Serialization;
using Pluto.SourceGen.ReverseEndiannessGenerator;
using Pluto.SourceGen.TransparentStructGenerator;
using Ulysses.Json;

namespace Ulysses.Struct;

[TransparentStruct<uint>, EndianSwappable, DebuggerDisplay("{DebugString}"), JsonConverter(typeof(HashIdConverter))]   
public partial struct HashId {
	public HashId(string name) => Value = IdRegistry.Register(name);
	public override string ToString() => IdRegistry.Lookup.TryGetValue(Value, out var name) ? name : $"[0x{Value:x08}]";
	public string DebugString => GetDebugString("Hash");
	public string GetDebugString(string prefix) => $"{IdRegistry.Lookup.GetValueOrDefault(Value, prefix)}::[0x{Value:x08}]";
}
