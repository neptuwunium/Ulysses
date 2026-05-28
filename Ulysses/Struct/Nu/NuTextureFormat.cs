// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.CompilerServices;
using Triton.Surface.DirectDraw;

namespace Ulysses.Struct.Nu;

public enum NuTextureFormat : byte {
	BC1 = 0x0,
	BC2 = 0x1,
	BC3 = 0x2,
	A8 = 0x5,
	B5G5R5A1 = 0x6,
	B4G4R4A4 = 0x7,
	B5G6R5 = 0x8,
	A8R8G8B8 = 0xe,
	B8G8R8A8 = 0x11,
	BC4 = 0x15,
	BC5 = 0x16,
}

public static class NuTextureFormatExtensions {
	extension(NuTextureFormat fmt) {
		public DDSPixelFormat PixelFormat {
			get {
				var blank = new DDSPixelFormat {
					Size = Unsafe.SizeOf<DDSPixelFormat>(),
				};

				switch (fmt) {
					case NuTextureFormat.BC1:
						return blank with {
							Flags = DDSPixelFlags.FourCC,
							FourCC = D3DFORMAT.DXT1,
						};
					case NuTextureFormat.BC2:
						return blank with {
							Flags = DDSPixelFlags.FourCC,
							FourCC = D3DFORMAT.DXT3,
						};
					case NuTextureFormat.BC3:
						return blank with {
							Flags = DDSPixelFlags.FourCC,
							FourCC = D3DFORMAT.DXT5,
						};
					case NuTextureFormat.BC4:
						return blank with {
							Flags = DDSPixelFlags.FourCC,
							FourCC = D3DFORMAT.ATI1,
						};
					case NuTextureFormat.BC5:
						return blank with {
							Flags = DDSPixelFlags.FourCC,
							FourCC = D3DFORMAT.ATI2,
						};
					case NuTextureFormat.A8:
						return blank with {
							Flags = DDSPixelFlags.RGB,
							RGBBitCount = 8,
							RBitMask = 0xFF,
						};
					case NuTextureFormat.B5G5R5A1:
						return blank with {
							Flags = DDSPixelFlags.RGB | DDSPixelFlags.AlphaPixels,
							RGBBitCount = 16,
							RBitMask = 0x7C00,
							GBitMask = 0x03E0,
							BBitMask = 0x001F,
							ABitMask = 0x8000,
						};
					case NuTextureFormat.B4G4R4A4:
						return blank with {
							Flags = DDSPixelFlags.RGB | DDSPixelFlags.AlphaPixels,
							RGBBitCount = 16,
							RBitMask = 0x00f0,
							GBitMask = 0x0f00,
							BBitMask = 0xf000,
							ABitMask = 0x000f,
						};
					case NuTextureFormat.B5G6R5:
						return blank with {
							Flags = DDSPixelFlags.RGB,
							RGBBitCount = 16,
							RBitMask = 0xF800,
							GBitMask = 0x07E0,
							BBitMask = 0x001F,
						};
					case NuTextureFormat.A8R8G8B8:
						return blank with {
							Flags = DDSPixelFlags.RGB | DDSPixelFlags.AlphaPixels,
							RGBBitCount = 32,
							RBitMask = 0x0000FF00,
							GBitMask = 0x00FF0000,
							BBitMask = 0xFF000000,
							ABitMask = 0x000000FF,
						};
					case NuTextureFormat.B8G8R8A8:
						return blank with {
							Flags = DDSPixelFlags.RGB | DDSPixelFlags.AlphaPixels,
							RGBBitCount = 32,
							RBitMask = 0x00FF0000,
							GBitMask = 0x0000FF00,
							BBitMask = 0x000000FF,
							ABitMask = 0xFF000000,
						};
					default: throw new ArgumentOutOfRangeException(nameof(fmt), fmt, null);
				}
			}
		}
	}
}
