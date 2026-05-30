// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers.Binary;
using System.Collections.Generic;
using Ulysses.Struct;

namespace Nimbus.DplDto;

public class ColorInformation {
	public ColorInformation(Dictionary<HashId, object?> row) {
		Version = row.GetCellOrDefault("ver", Version);
		AircraftId = row.GetCellOrDefault("AcId", default(HashId)).ToString();
		ColorNo = row.GetCellOrDefault("ColorNo", ColorNo);
		PaletteNum = row.GetCellOrDefault("PaletteNum", PaletteNum);
		ColorId = row.GetCellOrDefault("ColorId", ColorId);
		ItemId = row.GetCellOrDefault("ItemId", ItemId);
		Primary = BinaryPrimitives.ReverseEndianness((uint) row.GetCellOrDefault(0xd7a1e5ec, default(int)));
		Secondary = BinaryPrimitives.ReverseEndianness((uint) row.GetCellOrDefault(0xfef914c9, default(int)));
	}

	public int Version { get; set; }
	public string AircraftId { get; set; }
	public int ColorNo { get; set; }
	public int PaletteNum { get; set; }
	public int ColorId { get; set; }
	public int ItemId { get; set; }
	public uint Primary { get; set; }
	public uint Secondary { get; set; }
}
