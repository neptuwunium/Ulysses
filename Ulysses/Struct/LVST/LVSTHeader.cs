// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.LVST;

[EndianSwappable, StructLayout(LayoutKind.Sequential)]
public partial record struct LVSTHeader {
	public uint Magic { get; set; }
	public uint Version { get; set; }
	public uint Reserved1 { get; set; }
	public uint Reserved2 { get; set; }
}
