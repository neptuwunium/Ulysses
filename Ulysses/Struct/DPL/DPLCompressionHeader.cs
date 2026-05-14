// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.DPL;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 8, Size = 0x10)] 
public partial record struct DPLCompressionHeader {
	public byte Magic { get; set; }
	public DPLCompressType CompressType { get; set; }
	public ushort Index { get; set; }
	public uint Checksum { get; set; }
	public int MemorySize { get; set; }
	public int DiskSize { get; set; }
}
