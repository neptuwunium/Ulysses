// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Diagnostics;
using System.Text.Json.Serialization;
using Pluto.SourceGen.ReverseEndiannessGenerator;
using Pluto.SourceGen.TransparentStructGenerator;
using Ulysses.Json;

namespace Ulysses.Struct;

[TransparentStruct<uint>, EndianSwappable, DebuggerDisplay("{DebugString}"), JsonConverter(typeof(DPLIdConverter))]
public partial struct DPLId {
	public DPLId(string name) => IdRegistry.DPLLookup[Value = IdRegistry.Hash(name)] = name;
	public override string ToString() => IdRegistry.DPLLookup.TryGetValue(Value, out var name) ? name : $"DPL::[0x{Value:x08}]";
	public string DebugString => $"{IdRegistry.DPLLookup.GetValueOrDefault(Value, "DPL")}::[0x{Value:x08}]";
}
