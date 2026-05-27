// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.Nu;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 4)]
public partial record struct NuTextureHeader {
	[DoNotSwap] public ResourceMagic Magic { get; set; }
	public byte Version { get; set; }
	public byte Platform { get; set; }
	public ushort SurfaceCount { get; set; }
	private uint Reserved1 { get; init; }
	private uint Reserved2 { get; init; }
}
