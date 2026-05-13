// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text;
using Charon.Hash;
using Charon.Hash.Basis;

namespace Ulysses;

public static class ResourceIdRegistry {
	public static uint Hash(string name) {
		var text = (stackalloc byte[Encoding.UTF8.GetMaxByteCount(name.Length)]);
		var n = Encoding.UTF8.GetBytes(name, text);
		return CRC.HashData(CRC32Variants.BZip2, text[..n]);
	}

	static ResourceIdRegistry() {
		for (var i = 0; i < 1000; i++) {
			var test = $"DPL_EMBLEM_{i:D3}";
			NameLookup[Hash(test)] = test;

			test = $"DPL_2DIMAGE_EMBL_{i:D3}";
			NameLookup[Hash(test)] = test;

			test = $"DPL_2DIMAGE_EMB_{i:D3}";
			NameLookup[Hash(test)] = test;
		}

		for (var i = 1; i < 9; i++) {
			var test = $"DPL_2DIMAGE_GUIDE_{i:D}";
			NameLookup[Hash(test)] = test;
		}

		for (var i = 1; i < 18; i++) {
			var test = $"DPL_2DIMAGE_PARTS_{i:D}";
			NameLookup[Hash(test)] = test;
		}

		for (var i = 1; i < 40; i++) {
			var test = $"DPL_2DIMAGE_GIFT_{i:D}";
			NameLookup[Hash(test)] = test;
		}

		for (var i = 1; i < 8; i++) {
			var test = $"DPL_2DIMAGE_ADS_COMMON_{i:D}";
			NameLookup[Hash(test)] = test;
		}

		NameLookup[Hash("DPL_DEVELOPMENT")] = "DPL_DEVELOPMENT";
		NameLookup[Hash("DPL_INFORMATION")] = "DPL_INFORMATION";
		NameLookup[Hash("DPL_UI_STARTUP")] = "DPL_UI_STARTUP";
	}

	public static Dictionary<uint, string> NameLookup { get; } = [];
}
