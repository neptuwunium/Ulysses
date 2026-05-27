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

		using var data = fhm[item];
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
		// 2: "fast load" fhm file, where the headers are copied to [0], and the normal nu file is in [1]
		// 3: split virtual fhm file, where only the nut header is in [0], surface headers are in [1...surfaceCount] and data is in [surfaceCount...]

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

	internal static PNGEncoder PNGEncoder { get; } = new(PNGCompressionLevel.Small);

	internal static EncoderWriteOptions PNGEncoderOptions { get; } = new() {
		Compress = true,
		AssociateAlpha = false,
	};

	private void ProcessGPU(FHMFile fhm, int index, int surfaceIndex) {
		using var surfaceBuf = fhm[index];
		if (surfaceBuf.Length == 0) {
			return;
		}

		var bufSpan = surfaceBuf.Span;
		var surfaceHeader = MemoryMarshal.Read<NuTextureSurface>(bufSpan).ReverseEndianness();
		var dataBuffer = fhm[surfaceIndex];
		if (dataBuffer.Length == 0) {
			dataBuffer.Dispose();
			return;
		}

		LoadSurface(surfaceHeader, bufSpan[..surfaceHeader.HeaderSize], dataBuffer, RentedArray<byte>.Empty);
	}

	private void ProcessRAM(FHMFile fhm, int index) {
		var surfaceBuf = fhm[index];
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

		var globalIndex = (uint) Surfaces.Count;

		// believe it or not, this is what the game does, though it first looks for eXt\0 then GIDX after that.
		var header32 = MemoryMarshal.Cast<byte, uint>(headerBuffer);
		var gidx = header32.IndexOf((uint) ResourceMagic.GlobalIndex);
		if (gidx > -1 && gidx + 2 < header32.Length) {
			globalIndex = BinaryPrimitives.ReverseEndianness(header32[gidx + 2]);
		}

		if (headerBuffer.Length <= offset + 4 * Math.Min(1, (int) info.MipMapCount) || BinaryPrimitives.ReadUInt32BigEndian(headerBuffer[offset..]) == 0x65587400) {
			Surfaces.Add(new Surface(info, CalculateSurfaceSize(info), globalIndex, dataBuffer, owner));
			return;
		}

		var surfaceSize = 0;
		var sizes = MemoryMarshal.Cast<byte, int>(headerBuffer[offset..]);
		for (var index = 0; index < info.MipMapCount; index++) {
			surfaceSize += BinaryPrimitives.ReverseEndianness(sizes[index]);
		}

		surfaceSize = surfaceSize.Align(0x80);

		Surfaces.Add(new Surface(info, surfaceSize, globalIndex, dataBuffer, owner));
	}

	private static int CalculateSurfaceSize(NuTextureSurface info) {
		var format = info.PixelFormat switch {
			NuTextureFormat.BC1 => DXGIFormat.BC1_UNORM,
			NuTextureFormat.BC2 => DXGIFormat.BC2_UNORM,
			NuTextureFormat.BC3 => DXGIFormat.BC3_UNORM,
			NuTextureFormat.A8R8G8B8 => DXGIFormat.R8G8B8A8_UNORM,
			NuTextureFormat.A8 => DXGIFormat.A8_UNORM,
			NuTextureFormat.B5G5R5A1 => DXGIFormat.B5G5R5A1_UNORM,
			NuTextureFormat.B5G6R5 => DXGIFormat.B5G6R5_UNORM,
			NuTextureFormat.B4G4R4A4 => DXGIFormat.B4G4R4A4_UNORM,
			NuTextureFormat.B8G8R8A8 => DXGIFormat.B8G8R8A8_UNORM,
			NuTextureFormat.BC4 => DXGIFormat.BC4_SNORM,
			NuTextureFormat.BC5 => DXGIFormat.BC5_UNORM,
			_ => DXGIFormat.UNKNOWN,
		};
		return format == DXGIFormat.UNKNOWN ? -1 : (int) DDS.CalculateSurfaceSize(info.Width, info.Height, format, info.MipMapCount, out _);
	}

	public override string? GetResourceName(int resourceIndex, string baseName) {
		if (resourceIndex < 0 || resourceIndex > ResourceCount) {
			return null;
		}

		var slash = baseName.IndexOfAny('/', '\\');
		if (slash > -1) {
			return $"{baseName}/{Surfaces[0].GlobalIndex:x08}@{resourceIndex}_{baseName[..slash]}.png";
		}

		return $"{baseName}/{Surfaces[0].GlobalIndex:x08}@{resourceIndex}.png";
	}

	public override bool Save(Stream stream, int resourceIndex) => resourceIndex <= ResourceCount && SaveSurface(stream, Surfaces[resourceIndex]);

	public bool SaveSurface(Stream stream, Surface surface) {
		using var image = DecompressSurface(surface);
		if (image == null) {
			return false;
		}

		PNGEncoder.Write(stream, PNGEncoderOptions, image);
		return true;
	}

	public IImageBuffer? DecompressSurface(Surface surface) {
		// there's a whole palette thing but the game just sets the pointer to 0xacea.
		var buffer = surface.DataBuffer;
		if (buffer.Length == 0) {
			return null;
		}

		if ((surface.Info.Caps2 & DDSCaps2.Cubemap) == 0) {
			return DecompressSurface(surface.Info, buffer);
		}

		if (surface.SurfaceSize == -1) {
			return null;
		}

		using var collection = new ImageCollection();
		var offset = 0;
		for (var i = 0; i < 6; ++i) {
			if (offset >= buffer.Length) {
				return null;
			}

			var image = DecompressSurface(surface.Info, new UnownedRentedArray<byte>(buffer, offset));
			if (image == null) {
				return null;
			}

			collection.Add(image);
			offset += surface.SurfaceSize;
		}

		using var ibl = new IBLImage(collection, CubemapOrder.DXGIOrder);
		return ibl.ToEquirectangular();
	}

	private static IImageBuffer? DecompressSurface(NuTextureSurface info, IRentedArray<byte> buffer) {
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
			case NuTextureFormat.BC4: {
				var pixels = new RentedArray<byte>(info.Width * info.Height * 1);
				BCDec.DecompressBC4(buffer.Memory, pixels.Memory, info.Width, info.Height, false);
				return new ImageBuffer<ColorR<byte>, byte>(pixels, info.Width, info.Height);
			}
			case NuTextureFormat.BC5: {
				var pixels = new RentedArray<byte>(info.Width * info.Height * 2);
				BCDec.DecompressBC5(buffer.Memory, pixels.Memory, info.Width, info.Height, false);
				return new ImageBuffer<ColorRG<byte>, byte>(pixels, info.Width, info.Height);
			}
			case NuTextureFormat.A8R8G8B8: {
				return new ImageBuffer<ColorARGB<byte>, byte>(buffer, info.Width, info.Height);
			}
			case NuTextureFormat.A8: {
				return new ImageBuffer<ColorR<byte>, byte>(buffer, info.Width, info.Height);
			}
			case NuTextureFormat.B5G5R5A1: {
				return new ImageBuffer<ColorB5G5R5A1, float>(buffer, info.Width, info.Height);
			}
			case NuTextureFormat.B4G4R4A4: {
				return new ImageBuffer<ColorB4G4R4A4, byte>(buffer, info.Width, info.Height);
			}
			case NuTextureFormat.B5G6R5: {
				return new ImageBuffer<ColorB5G6R5, float>(buffer, info.Width, info.Height);
			}
			case NuTextureFormat.B8G8R8A8: {
				return new ImageBuffer<ColorARGB<byte>, byte>(buffer, info.Width, info.Height);
			}
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

	public readonly record struct Surface(NuTextureSurface Info, int SurfaceSize, uint GlobalIndex, IRentedArray<byte> DataBuffer, IRentedArray<byte> Owner) : IDisposable {
		public void Dispose() {
			DataBuffer.Dispose();
			Owner.Dispose();
		}
	}
}
