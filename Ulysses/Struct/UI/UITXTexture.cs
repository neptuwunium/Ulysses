// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;
using Silk.NET.Maths;

namespace Ulysses.Struct.UI;

[StructLayout(LayoutKind.Sequential, Pack = 4), EndianSwappable]
public partial record struct UITXTexture {
	public Vector2D<ushort> TopLeft { get; set; }
	public Vector2D<ushort> Size { get; set; }
	public int SurfaceIndex { get; set; }
}
