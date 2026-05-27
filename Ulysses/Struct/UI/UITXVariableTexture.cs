// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.UI;

[StructLayout(LayoutKind.Sequential, Pack = 4), EndianSwappable]
public partial record struct UITXVariableTexture {
	public int NUTHeaderIndex { get; set; }
	public int NUTIndex { get; set; }
	public int SurfaceIndex { get; set; }
}
