// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.DPL.Struct;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 8)]
public partial record struct FHMItemHeader {
	public FHMItemType Type { get; set; }
	public int Offset { get; set; }
}
