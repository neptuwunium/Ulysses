// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using Pluto.Extensions;
using Pluto.IO.Binary;
using Ulysses.Json;
using Ulysses.Struct;
using Ulysses.Struct.LVST;

namespace Ulysses;

[JsonConverter(typeof(LVSTableConverter))]
public sealed class LVSTableFile : IDisposable {
	public LVSTableFile(IRentedArray<byte> buffer, bool leaveOpen = false) {
		Buffer = buffer;
		LeaveOpen = leaveOpen;

		using var reader = new ArrayPoolBinaryReader(buffer, true);

		Header = reader.Read<LVSTHeader>().ReverseEndianness();

		var idByteSize = BinaryPrimitives.ReverseEndianness(reader.Read<int>());
		ColumnIds = new UnownedCovariantArray<HashId>(buffer, reader.Position, idByteSize / 4);
		reader.Skip<byte>(idByteSize);
		ColumnIds.Span.ReverseEndianness();

		var offsetByteSize = BinaryPrimitives.ReverseEndianness(reader.Read<int>());

		if (offsetByteSize != idByteSize) {
			throw new InvalidOperationException("mismatched sizes");
		}

		ColumnOffsets = new UnownedCovariantArray<int>(buffer, reader.Position, offsetByteSize / 4);
		reader.Skip<byte>(offsetByteSize);
		ColumnOffsets.Span.ReverseEndianness();

		ColumnInfos = new RentedArray<LVSTColumnInfo>(ColumnOffsets.Length);
		for (var i = 0; i < ColumnOffsets.Length; i++) {
			reader.Position = ColumnOffsets[i];
			var info = ColumnInfos.Span[i] = reader.Read<LVSTColumnInfo>().ReverseEndianness();
			RowCount = Math.Max(RowCount, info.RowCount);
		}
	}

	public bool LeaveOpen { get; }
	public LVSTHeader Header { get; }
	public IRentedArray<byte> Buffer { get; }
	public IRentedArray<HashId> ColumnIds { get; }
	public IRentedArray<LVSTColumnInfo> ColumnInfos { get; }
	public IRentedArray<int> ColumnOffsets { get; }
	public int RowCount { get; }

	public void Dispose() {
		ColumnIds.Dispose();
		ColumnOffsets.Dispose();
		ColumnInfos.Dispose();

		if (LeaveOpen) {
			return;
		}

		Buffer.Dispose();
	}

	private void ReadCellData(Span<byte> buffer, int rowStartOffset, int rowIndex, int byteWidth) {
		var offset = rowStartOffset + Unsafe.SizeOf<LVSTColumnInfo>() + buffer.Length * rowIndex;
		Buffer.Span.Slice(offset, buffer.Length).CopyTo(buffer);

		if (byteWidth <= 1) {
			return;
		}

		for (var i = 0; i < buffer.Length / byteWidth; i++) {
			buffer.Slice(i * byteWidth, byteWidth).Reverse();
		}
	}

	public object? ReadCell(int columnIndex, int rowIndex) {
		if (columnIndex > ColumnOffsets.Length) {
			return null;
		}

		var info = ColumnInfos[columnIndex];

		return info.ColumnType switch {
			LVSTColumnType.None => throw new UnreachableException(),
			LVSTColumnType.String when info is { ElementCount: 1, ElementSize: 1 } => Read<byte>(columnIndex, rowIndex),
			LVSTColumnType.String => ReadString(columnIndex, rowIndex),
			LVSTColumnType.Float when info is { ElementCount: 1, ElementSize: 4 } => Read<float>(columnIndex, rowIndex),
			LVSTColumnType.Int when info is { ElementCount: 1, ElementSize: 4 } => Read<int>(columnIndex, rowIndex),
			LVSTColumnType.Hash when info is { ElementCount: 1, ElementSize: 4 } => Read<HashId>(columnIndex, rowIndex),
			LVSTColumnType.Date when info is { ElementCount: 1, ElementSize: 4 } => Read<ACEDate>(columnIndex, rowIndex),
			LVSTColumnType.Null => null,
			_ => throw new NotSupportedException(info.ToString())
		};
	}

	public T Read<T>(int columnIndex, int rowIndex) where T : unmanaged {
		if (columnIndex > ColumnOffsets.Length) {
			return default;
		}

		var info = ColumnInfos[columnIndex];
		if (info.RowCount == 0) {
			return default;
		}

		if (rowIndex >= info.RowCount) {
			rowIndex %= info.RowCount; // maybe set it to max?
		}

		if (Unsafe.SizeOf<T>() != info.ElementSize) {
			throw new NotSupportedException(info.ToString());
		}

		var buf = (stackalloc byte[info.ElementCount * info.ElementSize]);
		ReadCellData(buf, ColumnOffsets[columnIndex], rowIndex, info.ElementSize);
		return MemoryMarshal.Read<T>(buf);
	}

	public string? ReadString(int columnIndex, int rowIndex) {
		if (columnIndex > ColumnOffsets.Length) {
			return null;
		}

		var info = ColumnInfos[columnIndex];
		if (info.RowCount == 0) {
			return null;
		}

		if (rowIndex > info.RowCount) {
			rowIndex %= info.RowCount; // maybe set it to max?
		}

		var buf = (stackalloc byte[info.ElementCount * info.ElementSize]);
		ReadCellData(buf, ColumnOffsets[columnIndex], rowIndex, info.ElementSize);

		return info.ElementSize switch {
			1 => buf.ReadString(Encoding.UTF8),
			2 => MemoryMarshal.Cast<byte, ushort>(buf).ReadString(Encoding.Unicode),
			4 => MemoryMarshal.Cast<byte, ushort>(buf).ReadString(Encoding.UTF32),
			_ => throw new NotSupportedException(info.ToString())
		};
	}

	public Dictionary<HashId, object?> GetRow(int row, Dictionary<HashId, object?>? cells = null) {
		if (cells != null) {
			// shared object
			cells.Clear();
			cells.EnsureCapacity(ColumnIds.Length);
		} else {
			cells = [];
		}

		for (var i = 0; i < ColumnIds.Length; ++i) {
			cells[ColumnIds[i]] = ReadCell(i, row);
		}

		return cells;
	}

	public IEnumerable<Dictionary<HashId, object?>> GetRows(Dictionary<HashId, object?>? cells = null) {
		for (var i = 0; i < RowCount; ++i) {
			yield return GetRow(i, cells);
		}
	}

	public List<Dictionary<HashId, object?>> ToList() => GetRows().ToList();
}
