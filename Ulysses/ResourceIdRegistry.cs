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
		}

		for (var i = 0; i <= 64; i++) {
			var test = $"DPL_DLC_CATALOG_{i:D}";
			NameLookup[Hash(test)] = test;
		}

		NameLookup[Hash("DPL_DEVELOPMENT")] = "DPL_DEVELOPMENT";
		NameLookup[Hash("DPL_INFORMATION")] = "DPL_INFORMATION";
		NameLookup[Hash("DPL_TSS_MISC")] = "DPL_TSS_MISC";
		NameLookup[Hash("DPL_TSS_INFO")] = "DPL_TSS_INFO";
		NameLookup[Hash("DPL_TSS_DROP_ITEM")] = "DPL_TSS_DROP_ITEM";
		NameLookup[Hash("DPL_TSS_SALES_LIST")] = "DPL_TSS_SALES_LIST";
		NameLookup[Hash("DPL_TSS_ITEM")] = "DPL_TSS_ITEM";
		NameLookup[Hash("DPL_UI_LOADING_TIP1")] = "DPL_UI_LOADING_TIP1";
		NameLookup[Hash("DPL_UI_STARTUP")] = "DPL_UI_STARTUP";
		NameLookup[Hash("DPL_UI_TSS_COMMON")] = "DPL_UI_TSS_COMMON";
		NameLookup[Hash("DPL_UI_TSS_MENU")] = "DPL_UI_TSS_MENU";
	}

	public static Dictionary<uint, string> NameLookup { get; } = [];
}
