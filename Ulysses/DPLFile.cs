// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers.Binary;
using System.Globalization;
using System.IO.MemoryMappedFiles;
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
	public DPLFile(string path, int priority) {
		Name = Path.GetFileNameWithoutExtension(path);
		Priority = priority;
		File = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
		using var reader = new MemoryMapBinaryReader(File, leaveOpen: true);
		Header = reader.Read<DPLHeader>().ReverseEndianness();

		if (Header.ACE.Magic.Value != 0x44504C) {
			throw new InvalidDataException();
		}

		FHMTable = ObjectPool<Dictionary<HashId, (long Offset, FHMHeader Header)>>.Rent();
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

	public string Name { get; set; }
	public int Priority { get; set; }
	public MemoryMappedFile File { get; set; }
	public Dictionary<HashId, (long Offset, FHMHeader Header)> FHMTable { get; set; }
	public Dictionary<uint, HashId> GroupToIdMap { get; set; }
	public DPLHeader Header { get; }

	public void Dispose() {
		File.Dispose();
		File = null!;
		FHMTable.Clear();
		ObjectPool<Dictionary<HashId, (long Offset, FHMHeader Header)>>.Return(FHMTable);
		FHMTable = null!;
		GroupToIdMap.Clear();
		ObjectPool<Dictionary<uint, HashId>>.Return(GroupToIdMap);
		GroupToIdMap = null!;
	}

	public static int GetPriority(string path) {
		var pacName = Path.GetFileNameWithoutExtension(path);
		if (!pacName.StartsWith("DATA")) {
			return -1;
		}

		var chars = pacName.AsSpan(4);
		if (!int.TryParse(chars[..2], NumberStyles.Integer, null, out var priority)) {
			return -1;
		}

		switch (priority) {
			case >= 10 and < 20: // skip audio
			case >= 20 and < 30: // skip video
				return -1;
		}

		priority *= 1000;
		// DATAnn_nnn
		// DATAnn_nn
		if (chars.Length > 5 &&
			((chars[2] == '_' && int.TryParse(chars[..2], NumberStyles.Integer, null, out var subPriority)) ||
				(chars[3] == '_' && int.TryParse(chars[..3], NumberStyles.Integer, null, out subPriority)))) {
			priority += subPriority;
		}

		if (priority == 99000) {
			// skip encrypted region data
			return -1;
		}

		return priority;
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

		var xor = MachinCrypto.GetXor(info.Header.Seed);

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

	public static class MachinCrypto {
		static MachinCrypto() {
			XorConst = new byte[0x800];

			var x = (stackalloc uint[0x209]);
			var y = (stackalloc uint[0x20D]);

			// calculate Pi via Machin's formula up to 515 bytes.
			// https://en.wikipedia.org/wiki/Machin-like_formula
			const int DIGITS = 0x203;
			Arctan(x, 5, 4, DIGITS);
			Arctan(y, 239, 1, DIGITS);
			Sub(x, y, DIGITS);
			Mul(x, 4, DIGITS);

			var xor = XorConst.AsSpan();
			for (var index = 0; index < 0x100; index++) {
				var piIndex = (((index & 0xFF) << 4) + 0x10) >> 3;
				BinaryPrimitives.WriteUInt32BigEndian(xor[(index * 8)..], x[piIndex]);
				BinaryPrimitives.WriteUInt32BigEndian(xor[(index * 8 + 4)..], x[piIndex + 1]);
			}
		}

		private static byte[] XorConst { get; }

		private static void Mul(Span<uint> x, uint m, int digit) {
			if (digit <= 0) {
				return;
			}

			var carry = 0u;
			for (var i = digit - 1; i >= 0; i--) {
				var result = (ulong) x[i] * m + carry;
				x[i] = (uint) (result & 0xFFFFFFFF);
				carry = (uint) (result >> 32);
			}
		}

		private static void Div(Span<uint> x, uint d, int digit) {
			if (digit <= 0) {
				return;
			}

			var remainder = 0u;
			for (var i = 0; i < digit; i++) {
				var current = ((ulong) remainder << 32) + x[i];
				x[i] = (uint) (current / d);
				remainder = (uint) (current % d);
			}
		}

		private static void Add(Span<uint> x, ReadOnlySpan<uint> y, int digit) {
			if (digit <= 0) {
				return;
			}

			var carry = 0u;
			for (var i = digit - 1; i >= 0; i--) {
				var total = (ulong) x[i] + y[i] + carry;
				x[i] = (uint) (total & 0xFFFFFFFF);
				carry = (uint) (total >> 32);
			}
		}

		private static void Sub(Span<uint> x, ReadOnlySpan<uint> y, int digit) {
			if (digit <= 0) {
				return;
			}

			var borrow = 0;
			for (var i = digit - 1; i >= 0; i--) {
				var diff = (long) x[i] - y[i] - borrow;
				if (diff < 0) {
					diff += 0x100000000L;
					borrow = 1;
				} else {
					borrow = 0;
				}

				x[i] = (uint) (diff & 0xFFFFFFFF);
			}
		}

		private static void Arctan(Span<uint> x, uint r, uint m, int digit) {
			var y = (stackalloc uint[0x20D]);
			y[0] = m;

			Div(y, r, digit);
			y[..digit].CopyTo(x);

			var rSquared = r * r;
			var divisor = 3u;
			var sign = 1;

			var temp = (stackalloc uint[digit]);
			while (true) {
				Div(y, rSquared, digit);

				y[..digit].CopyTo(temp);
				Div(temp, divisor, digit);

				var allZero = true;
				for (var i = 0; i < digit; i++) {
					if (temp[i] == 0) {
						continue;
					}

					allZero = false;
					break;
				}

				if (allZero) {
					break;
				}

				if ((sign & 1) != 0) {
					Sub(x, temp, digit);
				} else {
					Add(x, temp, digit);
				}

				divisor += 2;
				sign += 1;
			}
		}

		public static ReadOnlySpan<byte> GetXor(int seed) => seed == 0 ? ReadOnlySpan<byte>.Empty : XorConst.AsSpan((seed & 0xff) * 8, 8);
	}
}
