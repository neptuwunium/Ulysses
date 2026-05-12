// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Reflection;

namespace Ulysses.DPL;

public static class DPLCrypto {
	static DPLCrypto() {
		var bytes = new byte[0x800];
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ulysses.Resources.DPLXor.bin") ?? throw new FileNotFoundException();
		stream.ReadExactly(bytes);
		XorConst = bytes;
	}

	private static byte[] XorConst { get; }

	public static ReadOnlySpan<byte> GetXor(int seed) => seed == 0 ? ReadOnlySpan<byte>.Empty : XorConst.AsSpan((seed & 0xff) * 8, 8);
}
