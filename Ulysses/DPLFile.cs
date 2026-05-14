// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Charon.Compression;
using Pluto;
using Pluto.IO.Binary;
using Ulysses.Struct;
using Ulysses.Struct.DPL;
using Ulysses.Struct.FHM;

namespace Ulysses;

public sealed class DPLFile : IDisposable {
	public DPLFile(string path) {
		File = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
		using var reader = new MemoryMapBinaryReader(File, leaveOpen: true);
		Header = reader.Read<DPLHeader>().ReverseEndianness();

		FHMTable = ObjectPool<Dictionary<HashId, (int Offset, FHMHeader Header)>>.Rent();
		FHMTable.Clear();
		FHMTable.EnsureCapacity(Header.Count);

		GroupToIdMap = ObjectPool<Dictionary<uint, HashId>>.Rent();
		GroupToIdMap.Clear();
		GroupToIdMap.EnsureCapacity(Header.Count);

		for (var i = 0; i < Header.Count; i++) {
			var offset = reader.Position;
			var header = reader.Read<FHMHeader>().ReverseEndianness();

			reader.Skip<FHMMemoryRange>(header.MemoryRangeCount);

			FHMTable.Add(header.HashId, (offset, header));
			GroupToIdMap.Add(header.GroupId, header.HashId);
		}
	}

	public MemoryMappedFile File { get; set; }
	public Dictionary<HashId, (int Offset, FHMHeader Header)> FHMTable { get; set; }
	public Dictionary<uint, HashId> GroupToIdMap { get; set; }
	public DPLHeader Header { get; }

	public void Dispose() {
		File.Dispose();
		File = null!;
		FHMTable.Clear();
		ObjectPool<Dictionary<HashId, (int Offset, FHMHeader Header)>>.Return(FHMTable);
		FHMTable = null!;
		GroupToIdMap.Clear();
		ObjectPool<Dictionary<uint, HashId>>.Return(GroupToIdMap);
		GroupToIdMap = null!;
	}

	public FHMMemoryRange GetMemoryRange(HashId id, int index) {
		if (!FHMTable.TryGetValue(id, out var info) || index >= info.Header.MemoryRangeCount) {
			return default;
		}

		var offset = info.Offset + Unsafe.SizeOf<FHMHeader>() + Unsafe.SizeOf<FHMMemoryRange>() * index;
		using var reader = new MemoryMapBinaryReader(File, leaveOpen: true);
		reader.Position = offset;
		return reader.Read<FHMMemoryRange>().ReverseEndianness();
	}

	public RentedArray<byte>? ReadFile(HashId id) {
		if (!FHMTable.TryGetValue(id, out var info) || info.Header.IsDeleted) {
			return default;
		}

		var xor = Crypto.GetXor(info.Header.Seed);

		using var reader = new MemoryMapBinaryReader(File, info.Header.Offset, info.Header.DiskSize, true);
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

	public static class Crypto {
		static Crypto() {
			var bytes = new byte[0x800];
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ulysses.Resources.DPLXor.bin") ?? throw new FileNotFoundException();
			stream.ReadExactly(bytes);
			XorConst = bytes;
		}

		private static byte[] XorConst { get; }

		public static ReadOnlySpan<byte> GetXor(int seed) => seed == 0 ? ReadOnlySpan<byte>.Empty : XorConst.AsSpan((seed & 0xff) * 8, 8);
	}
}
