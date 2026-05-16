// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.Nu;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 4)]
public partial record struct NuTextureExtra {
	public uint Magic { get; set; }
	public int Size { get; set; }
	public int HeaderSize { get; set; }
	private int Reserved1 { get; init; }
}
