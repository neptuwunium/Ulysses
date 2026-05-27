// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.UI;

[StructLayout(LayoutKind.Sequential, Pack = 4), EndianSwappable]
public partial record struct UI2DImageLayerPtr {
	public HashId LayerId { get; set; }
	public int Offset { get; set; }
	public int Size { get; set; }
}
