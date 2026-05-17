// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text.Json.Serialization;
using Pluto.SourceGen.ReverseEndiannessGenerator;
using Pluto.SourceGen.TransparentStructGenerator;
using Ulysses.Json;

namespace Ulysses.Struct;

[EndianSwappable, TransparentStruct<uint>, JsonConverter(typeof(ACETimeConverter))]
public partial struct ACETime {
	public TimeSpan TimeSpan => TimeSpan.FromSeconds(Value);

	public override string ToString() => TimeSpan.ToString("HH:mm:ss");
}
