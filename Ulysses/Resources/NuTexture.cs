// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pluto;
using Pluto.Extensions;
using Pluto.IO.Binary;
using Triton;
using Triton.Encoder;
using Triton.Pixel.Formats;
using Triton.Surface;
using Triton.Surface.Compression;
using Triton.Surface.DirectDraw;
using Ulysses.Struct;
using Ulysses.Struct.FHM;
using Ulysses.Struct.Nu;

namespace Ulysses.Resources;

public class NuTexture : Resource {
	public NuTexture(FHMFile fhm, FHMItemHeader item, int fhmIndex, string name, bool leaveOpen = false) : base(fhm, name, leaveOpen) {
		Surfaces = ObjectPool<List<Surface>>.Rent();
		Surfaces.Clear();

		using var data = fhm.GetItemData(item);
		if (data.Length == 0) {
			return;
		}

		var span = data.Span;
		var header = MemoryMarshal.Read<NuTextureHeader>(span).ReverseEndianness();
		if (header.Version != 2) {
			return;
		}

		IsFullyUtilized = fhmIndex == 0 && (fhm.Count == 2 || fhm.Count == 1 + 2 * header.SurfaceCount);

		// 3 types
		// 1: "normal" nu file where the data and streams are in the same file
		// 2: "fast load" fhm file, where the headers are copied to 0, and the normal nu file is in [1]
		// 3: split virtual fhm file, where only the nut header is in [0], surface headers are in [1..surfaceCount] and data is in [surfaceCount..]

		if (data.Length == NuHeaderSize) {
			// type 3
			for (int index = 0, surfaceIndex = header.SurfaceCount; index < header.SurfaceCount; index++, surfaceIndex++) {
				ProcessGPU(fhm, index + fhmIndex + 1, surfaceIndex + fhmIndex + 1);
			}

			return;
		}

		var startIndex = 1;
		if (data.Length > Unsafe.SizeOf<NuTextureSurface>() + NuHeaderSize) {
			var surfaceHeader = MemoryMarshal.Read<NuTextureSurface>(span[NuHeaderSize..]).ReverseEndianness();
			if (data.Length >= surfaceHeader.HeaderSize + NuHeaderSize + surfaceHeader.PixelSize) {
				// type 1
				startIndex = 0;
			}
		}

		for (var index = startIndex; index < fhm.Count - fhmIndex; index++) {
			// type 2
			ProcessRAM(fhm, index + fhmIndex);
		}
	}

	private static int NuHeaderSize { get; } = Unsafe.SizeOf<NuTextureHeader>();

	public List<Surface> Surfaces { get; private set; }
	public override int ResourceCount => Surfaces.Count;

	private static PNGEncoder Encoder { get; } = new(PNGCompressionLevel.Small);

	private static EncoderWriteOptions EncoderOptions { get; } = new() {
		Compress = true,
		AssociateAlpha = false,
	};

	private void ProcessGPU(FHMFile fhm, int index, int surfaceIndex) {
		var itemHeader = fhm.GetItemDataHeader(index);
		using var surfaceBuf = fhm.GetItemData(itemHeader);
		if (surfaceBuf.Length == 0) {
			return;
		}

		var bufSpan = surfaceBuf.Span;
		var surfaceHeader = MemoryMarshal.Read<NuTextureSurface>(bufSpan).ReverseEndianness();
		itemHeader = fhm.GetItemDataHeader(surfaceIndex);
		var dataBuffer = fhm.GetItemData(itemHeader);
		if (dataBuffer.Length == 0) {
			dataBuffer.Dispose();
			return;
		}

		LoadSurface(surfaceHeader, bufSpan[..surfaceHeader.HeaderSize], dataBuffer, RentedArray<byte>.Empty);
	}

	private void ProcessRAM(FHMFile fhm, int index) {
		var itemHeader = fhm.GetItemDataHeader(index);
		var surfaceBuf = fhm.GetItemData(itemHeader);
		if (surfaceBuf.Length == 0) {
			surfaceBuf.Dispose();
			return;
		}

		var dataSpan = surfaceBuf.Span;
		var header = MemoryMarshal.Read<NuTextureHeader>(dataSpan).ReverseEndianness();

		if (header.Magic != ResourceMagic.NuTexturePS3) {
			surfaceBuf.Dispose();
			return;
		}

		var offset = NuHeaderSize;
		for (var surfaceIndex = 0; surfaceIndex < header.SurfaceCount; surfaceIndex++) {
			var surfaceHeader = MemoryMarshal.Read<NuTextureSurface>(dataSpan[offset..]).ReverseEndianness();
			var dataBuffer = new UnownedRentedArray<byte>(surfaceBuf, surfaceHeader.PixelOffset + offset, surfaceHeader.PixelSize);
			LoadSurface(surfaceHeader, dataSpan.Slice(offset, surfaceHeader.HeaderSize), dataBuffer, surfaceBuf);
			offset += surfaceHeader.HeaderSize;
		}
	}

	private void LoadSurface(NuTextureSurface info, ReadOnlySpan<byte> headerBuffer, IRentedArray<byte> dataBuffer, IRentedArray<byte> owner) {
		var offset = Unsafe.SizeOf<NuTextureSurface>();
		if ((info.Caps2 & DDSCaps2.Cubemap) != 0) {
			offset += sizeof(int) * 4; // surf x, surf y, align
		}

		if (headerBuffer.Length <= offset + 4 * Math.Min(1, (int) info.MipMapCount) || BinaryPrimitives.ReadUInt32BigEndian(headerBuffer[offset..]) == 0x65587400) {
			Surfaces.Add(new Surface(info, CalculateSurfaceSize(info), dataBuffer, owner));
			return;
		}

		var surfaceSize = 0;
		var sizes = MemoryMarshal.Cast<byte, int>(headerBuffer[offset..]);
		for (var index = 0; index < info.MipMapCount; index++) {
			surfaceSize += BinaryPrimitives.ReverseEndianness(sizes[index]);
		}

		surfaceSize = surfaceSize.Align(0x80);

		Surfaces.Add(new Surface(info, surfaceSize, dataBuffer, owner));
	}

	private static int CalculateSurfaceSize(NuTextureSurface info) {
		var format = info.PixelFormat switch {
			NuTextureFormat.BC1 => DXGIFormat.BC1_UNORM,
			NuTextureFormat.BC2 => DXGIFormat.BC2_UNORM,
			NuTextureFormat.BC3 => DXGIFormat.BC3_UNORM,
			NuTextureFormat.A8 or NuTextureFormat.L8 => DXGIFormat.A8_UNORM,
			NuTextureFormat.A4R4G4B4 or NuTextureFormat.X4R4G4B4 or NuTextureFormat.Q4W4V4U4 => DXGIFormat.B4G4R4A4_UNORM,
			NuTextureFormat.A8L8 or NuTextureFormat.V8U8 or NuTextureFormat.G8R8 => DXGIFormat.R8G8_UNORM,
			NuTextureFormat.L16 => DXGIFormat.R16_UNORM,
			NuTextureFormat.R16_FLOAT => DXGIFormat.R16_FLOAT,
			NuTextureFormat.A8R8G8B8 or NuTextureFormat.X8R8G8B8 or NuTextureFormat.A8B8G8R8 or NuTextureFormat.X8B8G8R8 or NuTextureFormat.X8L8V8U8 or NuTextureFormat.Q8W8V8U8 => DXGIFormat.R8G8B8A8_UNORM,
			NuTextureFormat.X2R10G10B10 or NuTextureFormat.A2B10G10R10 => DXGIFormat.R10G10B10A2_UNORM,
			NuTextureFormat.A16L16 or NuTextureFormat.G16R16 or NuTextureFormat.V16U16 => DXGIFormat.R16G16_UNORM,
			NuTextureFormat.G16R16_FLOAT => DXGIFormat.R16G16_FLOAT,
			NuTextureFormat.L32 => DXGIFormat.R32_UINT,
			NuTextureFormat.R32_FLOAT => DXGIFormat.R32_FLOAT,
			NuTextureFormat.A16B16G16R16 or NuTextureFormat.Q16W16V16U16 => DXGIFormat.R16G16B16A16_UNORM,
			NuTextureFormat.A16B16G16R16_FLOAT => DXGIFormat.R16G16B16A16_FLOAT,
			NuTextureFormat.A32L32 or NuTextureFormat.G32R32 or NuTextureFormat.V32U32 => DXGIFormat.R32G32_UINT,
			NuTextureFormat.G32R32_FLOAT => DXGIFormat.R32G32_FLOAT,
			NuTextureFormat.A32B32G32R32 or NuTextureFormat.Q32W32V32U32 or NuTextureFormat.A32B32G32R32_FLOAT => DXGIFormat.R32G32B32A32_UINT,
			_ => DXGIFormat.UNKNOWN,
		};
		return format == DXGIFormat.UNKNOWN ? -1 : (int) DDS.CalculateSurfaceSize(info.Width, info.Height, format, info.MipMapCount, out _);
	}

	public override string? GetResourceName(int resourceIndex, string prefix) => ResourceCount switch {
		1 => Name + prefix + ".png",
		> 1 => Name + prefix + $"{resourceIndex}.png",
		_ => null,
	};

	public override bool Save(Stream stream, int resourceIndex) => resourceIndex <= ResourceCount && SaveSurface(stream, Surfaces[resourceIndex]);

	public static bool SaveSurface(Stream stream, Surface surface) {
		// there's a whole palette thing but the game just sets the pointer to 0xacea.
		var buffer = surface.DataBuffer;
		if ((surface.Info.Caps2 & DDSCaps2.Cubemap) != 0) {
			if (surface.SurfaceSize == -1) {
				return false;
			}

			using var collection = new ImageCollection();
			var offset = 0;
			for (var i = 0; i < 6; ++i) {
				var image = DecompressSurface(surface.Info, new UnownedRentedArray<byte>(buffer, offset));
				if (image == null) {
					return false;
				}

				collection.Add(image);
				offset += surface.SurfaceSize;
			}

			using var ibl = new IBLImage(collection, CubemapOrder.DXGIOrder);
			using var equirect = ibl.ToEquirectangular();
			Encoder.Write(stream, EncoderOptions, equirect);
		} else {
			using var image = DecompressSurface(surface.Info, buffer);
			if (image == null) {
				return false;
			}

			Encoder.Write(stream, EncoderOptions, image);
		}

		return true;
	}

	private static IImageBuffer? DecompressSurface(NuTextureSurface info, IRentedArray<byte> buffer) {
		// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
		switch (info.PixelFormat) {
			case NuTextureFormat.BC1: {
				var pixels = new RentedArray<byte>(info.Width * info.Height * 4);
				BCDec.DecompressBC1(buffer.Memory, pixels.Memory, info.Width, info.Height);
				return new ImageBuffer<ColorRGBA<byte>, byte>(pixels, info.Width, info.Height);
			}
			case NuTextureFormat.BC2: {
				var pixels = new RentedArray<byte>(info.Width * info.Height * 4);
				BCDec.DecompressBC2(buffer.Memory, pixels.Memory, info.Width, info.Height);
				return new ImageBuffer<ColorRGBA<byte>, byte>(pixels, info.Width, info.Height);
			}
			case NuTextureFormat.BC3: {
				var pixels = new RentedArray<byte>(info.Width * info.Height * 4);
				BCDec.DecompressBC3(buffer.Memory, pixels.Memory, info.Width, info.Height);
				return new ImageBuffer<ColorRGBA<byte>, byte>(pixels, info.Width, info.Height);
			}
			case NuTextureFormat.DXN:
			case NuTextureFormat.CTX1: return null;
			case NuTextureFormat.A8:
			case NuTextureFormat.L8: {
				return new ImageBuffer<ColorR<byte>, byte>(buffer, info.Width, info.Height);
			}
			case NuTextureFormat.R5G6B5: {
				return new ImageBuffer<ColorR5G6B5, float>(buffer, info.Width, info.Height);
			}
			case NuTextureFormat.A4R4G4B4:
			case NuTextureFormat.X4R4G4B4:
			case NuTextureFormat.Q4W4V4U4: {
				return new ImageBuffer<ColorARGB16, byte>(buffer, info.Width, info.Height);
			}
			case NuTextureFormat.A8L8:
			case NuTextureFormat.V8U8:
			case NuTextureFormat.G8R8:
				return new ImageBuffer<ColorRG<byte>, byte>(buffer, info.Width, info.Height);
			case NuTextureFormat.L16:
				return new ImageBuffer<ColorR<ushort>, ushort>(buffer, info.Width, info.Height);
			case NuTextureFormat.R16_FLOAT:
				return new ImageBuffer<ColorR<float>, float>(buffer, info.Width, info.Height);
			case NuTextureFormat.A8R8G8B8:
			case NuTextureFormat.X8R8G8B8:
				return new ImageBuffer<ColorARGB<byte>, byte>(buffer, info.Width, info.Height);
			case NuTextureFormat.A8B8G8R8:
			case NuTextureFormat.X8B8G8R8:
				return new ImageBuffer<ColorABGR<byte>, byte>(buffer, info.Width, info.Height);
			case NuTextureFormat.X8L8V8U8:
			case NuTextureFormat.Q8W8V8U8:
				return new ImageBuffer<ColorARGB<byte>, byte>(buffer, info.Width, info.Height);
			case NuTextureFormat.X2R10G10B10:
			case NuTextureFormat.A2B10G10R10:
				return new ImageBuffer<ColorA2R10G10B10, float>(buffer, info.Width, info.Height);
			case NuTextureFormat.A16L16:
			case NuTextureFormat.G16R16:
			case NuTextureFormat.V16U16:
				return new ImageBuffer<ColorRG<ushort>, ushort>(buffer, info.Width, info.Height);
			case NuTextureFormat.R11G11B10:
			case NuTextureFormat.W11V11U10:
				return new ImageBuffer<ColorR11G11B10, float>(buffer, info.Width, info.Height);
			case NuTextureFormat.G16R16_FLOAT:
				return new ImageBuffer<ColorRG<float>, float>(buffer, info.Width, info.Height);
			case NuTextureFormat.L32:
				return new ImageBuffer<ColorR<uint>, uint>(buffer, info.Width, info.Height);
			case NuTextureFormat.R32_FLOAT:
				return new ImageBuffer<ColorR<float>, float>(buffer, info.Width, info.Height);
			case NuTextureFormat.A16B16G16R16:
				return new ImageBuffer<ColorABGR<ushort>, ushort>(buffer, info.Width, info.Height);
			case NuTextureFormat.Q16W16V16U16:
				return new ImageBuffer<ColorRGBA<ushort>, ushort>(buffer, info.Width, info.Height);
			case NuTextureFormat.A16B16G16R16_FLOAT:
				return new ImageBuffer<ColorRGBA<Half>, Half>(buffer, info.Width, info.Height);
			case NuTextureFormat.A32L32:
			case NuTextureFormat.G32R32:
			case NuTextureFormat.V32U32:
				return new ImageBuffer<ColorRG<uint>, uint>(buffer, info.Width, info.Height);
			case NuTextureFormat.G32R32_FLOAT:
				return new ImageBuffer<ColorRG<float>, float>(buffer, info.Width, info.Height);
			case NuTextureFormat.A32B32G32R32:
				return new ImageBuffer<ColorABGR<uint>, uint>(buffer, info.Width, info.Height);
			case NuTextureFormat.Q32W32V32U32:
				return new ImageBuffer<ColorRGBA<uint>, uint>(buffer, info.Width, info.Height);
			case NuTextureFormat.A32B32G32R32_FLOAT:
				return new ImageBuffer<ColorRGBA<float>, float>(buffer, info.Width, info.Height);
			default: return null;
		}
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (var surface in Surfaces) {
				surface.Dispose();
			}
		}

		Surfaces.Clear();
		ObjectPool<List<Surface>>.Return(Surfaces);
		Surfaces = null!;
	}

	public readonly record struct Surface(NuTextureSurface Info, int SurfaceSize, IRentedArray<byte> DataBuffer, IRentedArray<byte> Owner) : IDisposable {
		public void Dispose() {
			DataBuffer.Dispose();
			Owner.Dispose();
		}
	}
}
