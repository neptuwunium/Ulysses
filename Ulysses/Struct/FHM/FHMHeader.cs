// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.FHM;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 8)]
public partial record struct FHMHeader {
	public ACEHeader ACE { get; set; }
	public int MemoryRangeCount { get; set; }
	public int FHMTableSize { get; set; }
	public int MemorySize { get; set; }
	public int CPUMemorySize { get; set; }
	public int GPUMemorySize { get; set; }
	public int RAMMemorySize { get; set; }
	public int CPUAlignment { get; set; }
	public int GPUAlignment { get; set; }
	public int RAMAlignment { get; set; }
	public long Offset { get; set; }
	public int DiskSize { get; set; }
	public uint GroupId { get; set; }
	public HashId HashId { get; set; }
	public ushort Seed { get; set; }
	public bool IsDeleted { get; set; }
}
