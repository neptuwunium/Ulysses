// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;
using Silk.NET.Maths;

namespace Ulysses.Struct.UI;

[StructLayout(LayoutKind.Sequential, Pack = 4), EndianSwappable]
public partial record struct UI2DImageTexture {
	public int UnknownIndex { get; set; }
	public int FHMIndex { get; set; }
	public int UITXUniqueIndex { get; set; }
	public Vector4D<ushort> Crop { get; set; }
	public Vector4D<float> Size { get; set; }
	public Vector2D<float> Scale { get; set; }
	public uint Unknown1 { get; set; }
	public int UITXIndex { get; set; }
}
