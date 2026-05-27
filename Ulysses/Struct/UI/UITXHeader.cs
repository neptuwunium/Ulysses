// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.UI;

[StructLayout(LayoutKind.Sequential, Pack = 4), EndianSwappable]
public partial record struct UITXHeader {
	[DoNotSwap] public ResourceMagic Magic { get; set; }
	public int VariableCount { get; set; }
	public int VariableOffset { get; set; }
	public int TextureCount { get; set; }
	public int TextureOffset { get; set; }
}
