// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.FHM;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 8)] 
public partial record struct FHMItemDataHeader {
	public ushort MemoryIndex { get; set; }
	public FHMMemoryType MemoryType { get; set; }
	public int Alignment { get; set; }
	public int Offset { get; set; }
	public int Size { get; set; }
}
