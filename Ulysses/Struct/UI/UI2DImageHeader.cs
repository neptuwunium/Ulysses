// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.UI;

[StructLayout(LayoutKind.Sequential, Pack = 4), EndianSwappable]
public partial record struct UI2DImageHeader {
	[DoNotSwap] public ResourceMagic Magic { get; set; }
	public HashId ImageId { get; set; }
	public int LayerCount { get; set; }
}
