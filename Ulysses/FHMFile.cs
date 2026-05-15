// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Charon.Hash;
using Charon.Hash.Algorithms;
using Charon.Hash.Basis;
using Pluto.IO.Binary;
using Ulysses.Struct;
using Ulysses.Struct.FHM;

namespace Ulysses;

public sealed class FHMFile : IDisposable {
	public FHMFile(IRentedArray<byte> pool, int offset, FHMHeader header) {
		Pool = pool;
		Header = header;
		Offset = offset;
		Count = BinaryPrimitives.ReadInt32BigEndian(pool.Span[offset..]);
	}

	public FHMHeader Header { get; }
	public int Count { get; }
	public int Offset { get; set; }
	public IRentedArray<byte> Pool { get; set; }

	public IEnumerable<FHMItemHeader> ItemHeaders {
		get {
			for (var i = 0; i < Count; ++i) {
				yield return GetItemHeader(i);
			}
		}
	}

	public void Dispose() {
		if (Offset != 0) {
			return;
		}

		Pool.Dispose();
	}

	public FHMItemHeader GetItemHeader(int index) => MemoryMarshal.Read<FHMItemHeader>(Pool.Span[(Offset + 4 + index * Unsafe.SizeOf<FHMItemHeader>())..]).ReverseEndianness();

	public FHMItemDataHeader GetItemDataHeader(int index) => GetItemDataHeader(GetItemHeader(index));

	public FHMItemDataHeader GetItemDataHeader(FHMItemHeader item) {
		if (item.Type != FHMItemType.Normal) {
			return default;
		}

		return MemoryMarshal.Read<FHMItemDataHeader>(Pool.Span[(Offset + item.Offset)..]).ReverseEndianness();
	}

	public IRentedArray<byte> GetItemData(int index) => GetItemData(GetItemDataHeader(index));
	public IRentedArray<byte> GetItemData(FHMItemHeader item) => GetItemData(GetItemDataHeader(item));
	public IRentedArray<byte> GetItemData(FHMItemDataHeader dataItem) => dataItem.Size == 0 ? RentedArray<byte>.Empty : new UnownedCovariantArray<byte>(Pool, dataItem.Offset, dataItem.Size);

	public FHMFile? GetChildItem(int index) => GetChildItem(GetItemHeader(index));
	public FHMFile? GetChildItem(FHMItemHeader item) => item.Type != FHMItemType.Child && item.Offset > 0 ? null : new FHMFile(Pool, Offset + item.Offset, Header);

	public IRentedArray<byte>? RebuildAsset(out string? ext) {
		if (ResourceConverter.FindConverter(this) is not { } converter) {
			ext = null;
			return null;
		}

		ext = converter.Extension;
		return converter.Uncook(this);
	}

	public ulong ShapeHash() {
		using var crc = CRC.Create(CRC64Variants.Default);
		ShapeHash(crc);
		return crc.GetValueFinal();
	}

	public void ShapeHash(CRCAlgorithm<ulong> crc) {
		crc.Update("FHM "u8);
		var value = (uint) Count;
		crc.Update(MemoryMarshal.AsBytes(new ReadOnlySpan<uint>(ref value)));
		foreach (var item in ItemHeaders) {
			if (item.Type == FHMItemType.Normal) {
				using var header = GetItemData(item);
				value = header.Length >= 4 ? MemoryMarshal.Read<uint>(header.Span) : uint.MaxValue;
				crc.Update(MemoryMarshal.AsBytes(new ReadOnlySpan<uint>(ref value)));
			} else {
				using var child = GetChildItem(item);
				if (child == null) {
					crc.Update("FHM "u8);
					value = 0;
					crc.Update(MemoryMarshal.AsBytes(new ReadOnlySpan<uint>(ref value)));
				} else {
					child.ShapeHash(crc);
				}
			}
		}
	}

	public void DumpShape(StringBuilder builder, string indent = "") {
		builder.AppendLine($"{indent}FHM\t{Count}");
		foreach (var item in ItemHeaders) {
			if (item.Type == FHMItemType.Normal) {
				using var header = GetItemData(item);
				builder.AppendLine($"{indent}\t{(header.Length >= 4 ? MemoryMarshal.Read<ResourceMagic>(header.Span).ToString() : "NULL")}");
			} else {
				using var child = GetChildItem(item);
				if (child == null) {
					builder.AppendLine($"{indent}\tFHM 0");
				} else {
					child.DumpShape(builder, indent + "\t");
				}
			}
		}
	}

	public bool CheckMagic(int index, ResourceMagic magic) {
		using var buf = GetItemData(index);
		if (buf.Length < 4) {
			return false;
		}

		return MemoryMarshal.Read<ResourceMagic>(buf.Span) == magic;
	}
}
