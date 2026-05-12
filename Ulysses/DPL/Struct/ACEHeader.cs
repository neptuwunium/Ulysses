// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.DPL.Struct;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 8)]
public partial record struct ACEHeader {
	public ACEMagic Magic { get; set; }
	public uint Version { get; set; }
	public uint Date { get; set; }
}
