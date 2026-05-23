// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;

namespace Ulysses.Struct;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
public record struct ACEMagic {
	public byte B0 { get; set; }
	public byte B1 { get; set; }
	public byte B2 { get; set; }
	public bool IsBigEndian { get; set; }

	public uint Value {
		get => ((uint) B0 << 16) | ((uint) B1 << 8) | B2;
		set {
			B0 = (byte) (value >> 16);
			B1 = (byte) (value >> 8);
			B2 = (byte) (value >> 0);
		}
	}

	public override string ToString() {
		var magic = (stackalloc char[3]);
		magic[0] = (char) B0;
		magic[1] = (char) B1;
		magic[2] = (char) B2;
		return new string(magic) + (IsBigEndian ? "(Big)" : "(Little)");
	}
}
