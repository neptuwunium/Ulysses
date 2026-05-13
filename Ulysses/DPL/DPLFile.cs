// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Charon.Compression;
using Pluto;
using Pluto.IO.Binary;
using Ulysses.Struct.DPL;
using Ulysses.Struct.FHM;

namespace Ulysses.DPL;

public sealed class DPLFile : IDisposable {
	public DPLFile(string path) {
		File = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
		using var reader = new MemoryMapBinaryReader(File, leaveOpen: true);
		Header = reader.Read<DPLHeader>().ReverseEndianness();

		FHMTable = ObjectPool<Dictionary<ResourceId, (int Offset, FHMHeader Header)>>.Rent();
		FHMTable.Clear();
		FHMTable.EnsureCapacity(Header.Count);

		GroupToResourceIdMap = ObjectPool<Dictionary<uint, ResourceId>>.Rent();
		GroupToResourceIdMap.Clear();
		GroupToResourceIdMap.EnsureCapacity(Header.Count);

		for (var i = 0; i < Header.Count; i++) {
			var offset = reader.Position;
			var header = reader.Read<FHMHeader>().ReverseEndianness();

			reader.Skip<FHMMemoryRange>(header.MemoryRangeCount);

			FHMTable.Add(header.ResourceId, (offset, header));
			GroupToResourceIdMap.Add(header.GroupId, header.ResourceId);
		}
	}

	public MemoryMappedFile File { get; set; }
	public Dictionary<ResourceId, (int Offset, FHMHeader Header)> FHMTable { get; set; }
	public Dictionary<uint, ResourceId> GroupToResourceIdMap { get; set; }
	public DPLHeader Header { get; }

	public FHMMemoryRange GetMemoryRange(ResourceId resourceId, int index) {
		if (!FHMTable.TryGetValue(resourceId, out var info) || index >= info.Header.MemoryRangeCount) {
			return default;
		}

		var offset = info.Offset + Unsafe.SizeOf<FHMHeader>() + Unsafe.SizeOf<FHMMemoryRange>() * index;
		using var reader = new MemoryMapBinaryReader(File, leaveOpen: true);
		reader.Position = offset;
		return reader.Read<FHMMemoryRange>().ReverseEndianness();
	}

	public RentedArray<byte>? ReadFile(ResourceId resourceId) {
		if (!FHMTable.TryGetValue(resourceId, out var info) || info.Header.IsDeleted) {
			return default;
		}

		var xor = DPLCrypto.GetXor(info.Header.Seed);

		using var reader = new MemoryMapBinaryReader(File, info.Header.Offset, info.Header.DiskSize, leaveOpen: true);
		var result = new RentedArray<byte>(info.Header.MemorySize);
		var span = result.Memory;
		try {
			var offset = 0;

			while (offset < info.Header.MemorySize) {
				var header = reader.Read<DPLCompressionHeader>().ReverseEndianness();
				if (header.Magic != 'C') {
					throw new InvalidDataException("block failed magic");
				}

				using var buffer = reader.ReadBytes(header.DiskSize);
				var chunk = span.Slice(offset, header.MemorySize);

				if (xor.Length > 0) {
					var sp = buffer.Span;
					var sp8 = MemoryMarshal.Cast<byte, ulong>(sp);
					var x = MemoryMarshal.Read<ulong>(xor);

					for (var i = 0; i < sp8.Length; ++i) {
						sp8[i] ^= x;
					}

					for (var i = sp8.Length * 8; i < sp.Length; ++i) {
						sp[i] ^= xor[i % 8];
					}
				}

				switch (header.CompressType) {
					case DPLCompressType.Lz77: {
						throw new NotSupportedException("Lz77 is not supported");
					}
					case DPLCompressType.Deflate: {
						if (CompressionHelper.Decompress(CompressionType.Deflate, buffer.Memory, chunk) != header.MemorySize) {
							throw new InvalidDataException("cannot decompress deflate");
						}

						break;
					}
					case DPLCompressType.None: {
						if (CompressionHelper.Decompress(CompressionType.None, buffer.Memory, chunk) != header.MemorySize) {
							throw new InvalidDataException("cannot copy data");
						}

						break;
					}
					default: throw new NotSupportedException($"unknown compression type {header.CompressType}");
				}

				offset += header.MemorySize;
			}
		} catch {
			result.Dispose();
			throw;
		}

		return result;
	}

	public void Dispose() {
		File.Dispose();
		File = null!;
		FHMTable.Clear();
		ObjectPool<Dictionary<ResourceId, (int Offset, FHMHeader Header)>>.Return(FHMTable);
		FHMTable = null!;
		GroupToResourceIdMap.Clear();
		ObjectPool<Dictionary<uint, ResourceId>>.Return(GroupToResourceIdMap);
		GroupToResourceIdMap = null!;
	}
}
