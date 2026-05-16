// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.LVST;

[EndianSwappable, StructLayout(LayoutKind.Sequential)]
public partial record struct LVSTColumnInfo {
	public LVSTColumnType ColumnType { get; set; }
	public byte ElementSize { get; set; }
	public byte ElementCount { get; set; }
	public byte Reserved { get; set; }
	public int RowCount { get; set; }
}
