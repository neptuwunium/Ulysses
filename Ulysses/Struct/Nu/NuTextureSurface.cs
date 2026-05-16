// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;
using Triton.Surface.DirectDraw;

namespace Ulysses.Struct.Nu;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 4)]
public partial record struct NuTextureSurface {
	public int Size { get; set; }
	public int PaletteSize { get; set; }
	public int PixelSize { get; set; }
	public ushort HeaderSize { get; set; }
	public ushort PaletteCount { get; set; }
	public byte SurfaceType { get; set; }
	public byte MipMapCount { get; set; }
	public NuTextureFormat PaletteFormat { get; set; }
	public NuTextureFormat PixelFormat { get; set; }
	public ushort Width { get; set; }
	public ushort Height { get; set; }
	public DDSCaps1 Caps1 { get; set; }
	public DDSCaps2 Caps2 { get; set; }
	public int PixelOffset { get; set; }
	private int Reserved1 { get; init; }
	private int Reserved2 { get; init; }
	private int Reserved3 { get; init; }

	public bool HasExtra => HeaderSize > Unsafe.SizeOf<NuTextureSurface>();

	public int GetPaletteOffset(int version) => version switch {
		not (1 or 2) => -1,
		_ => GetPixelOffset(version) + PixelSize,
	};

	public int GetPixelOffset(int version) => version switch {
		1 => HeaderSize,
		2 => PixelOffset,
		_ => -1
	};

	public int GetSurfaceSize(int version) => version switch {
		1 => Size,
		2 => HeaderSize,
		_ => -1
	};
}
