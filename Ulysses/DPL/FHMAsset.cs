// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pluto.IO.Binary;
using Ulysses.Struct.FHM;

namespace Ulysses.DPL;

public sealed class FHMAsset : IDisposable {
	public FHMAsset(IRentedArray<byte> pool, int offset, FHMHeader header) {
		Pool = pool;
		Header = header;
		Offset = offset;
		Count = BinaryPrimitives.ReadInt32BigEndian(pool.Span[offset..]);
	}

	public FHMHeader Header { get; }
	public int Count { get; }
	public int Offset { get; set; }
	public IRentedArray<byte> Pool { get; set; }

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

	public FHMAsset? GetChildItem(int index) => GetChildItem(GetItemHeader(index));
	public FHMAsset? GetChildItem(FHMItemHeader item) => item.Type != FHMItemType.Child && item.Offset > 0 ? null : new FHMAsset(Pool, Offset + item.Offset, Header);

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

	public IRentedArray<byte>? RebuildAsset(out string? ext) {
		ext = null;
		// todo: check magic for .act .mis, .nut, .num, .lua, etc

		return null;
	}
}
