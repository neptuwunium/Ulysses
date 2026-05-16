// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.Nu;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 8)]
public partial record struct NuTextureExtraData<T> where T : IEndianReversible<T> {
	public uint Magic { get; set; }
	public int Size { get; set; }
	public T Data { get; set; }
}
