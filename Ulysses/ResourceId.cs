// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Diagnostics;
using Pluto.SourceGen.ReverseEndiannessGenerator;
using Pluto.SourceGen.TransparentStructGenerator;

namespace Ulysses;

[TransparentStruct<uint>, EndianSwappable, DebuggerDisplay("{DebugString}")]
public partial struct ResourceId {
	public ResourceId(string name) => ResourceIdRegistry.NameLookup[Value = ResourceIdRegistry.Hash(name)] = name;
	public override string ToString() => ResourceIdRegistry.NameLookup.TryGetValue(Value, out var name) ? name : $"DPL::[0x{Value:x08}]";
	public string DebugString => $"{ResourceIdRegistry.NameLookup.GetValueOrDefault(Value, "DPL")}::[0x{Value:x08}]";
}
