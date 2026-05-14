// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

namespace Ulysses.Struct.LVST;

public enum LVSTColumnType : byte {
	None = 0x00,
	String = 0x10,
	Float = 0x30,
	Int = 0x40,
	DPLId = 0x41,
	Date = 0x91,
	Null = 0xff,
}
