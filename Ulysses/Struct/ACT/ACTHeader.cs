// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.ACT;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 8)]
public partial record struct ACTHeader {
	public uint Magic { get; set; }
	public uint Version { get; set; }
	public uint DataVersion { get; set; }
	public bool IsBigEndian { get; set; }
	public byte LanguageCount { get; set; }
	public int TextCount { get; set; }
	public int HashCount { get; set; }
	public int LanguageTableOffset { get; set; }
	public int TextTableOffset { get; set; }
	public int HashTableOffset { get; set; }
}
