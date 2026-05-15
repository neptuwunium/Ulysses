// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Pluto;
using Pluto.IO.Binary;
using Ulysses.Struct;
using Ulysses.Struct.FHM;

namespace Ulysses;

public interface IResourceConverter {
	string Extension { get; }
	IRentedArray<byte>? Uncook(FHMFile fhm);
}

public sealed class TextureConverter : IResourceConverter {
	public string Extension => ".png";
	public IRentedArray<byte> Uncook(FHMFile fhm) => throw new NotImplementedException();
}

public sealed class UITextureConverter : IResourceConverter {
	public string Extension => ".png";
	public IRentedArray<byte> Uncook(FHMFile fhm) => throw new NotImplementedException();
}

public static class ResourceConverter {
	public delegate bool IsValidFor(FHMFile fhm);

	public static Dictionary<IsValidFor, IResourceConverter> ConverterFunclets { get; } = new() {
		[IsNuTextureAsset] = Singleton<TextureConverter>.Instance,
		[IsUIManagerAsset] = Singleton<UITextureConverter>.Instance,
	};

	public static IResourceConverter? FindConverter(FHMFile file) {
		foreach (var (test, instance) in ConverterFunclets) {
			if (test(file)) {
				return instance;
			}
		}

		return null;
	}

	private static bool IsUIManagerAsset(FHMFile fhm) {
		if (IdRegistry.Lookup.TryGetValue(fhm.Header.HashId, out var name) && name.StartsWith("DPL_2DIMAGE_", StringComparison.Ordinal)) {
			return true;
		}

		if (fhm.Count != 1) {
			return false;
		}

		// UIImage -> UITexture -> NuImage[]
		using var ui = fhm.GetChildItem(0);
		if (ui is not { Count: 2 } || !ui.CheckMagic(0, ResourceMagic.UIImage)) {
			return false;
		}

		using var uiTex = ui.GetChildItem(1);
		if (uiTex is not { Count: 2 } || !uiTex.CheckMagic(0, ResourceMagic.UITexture)) {
			return false;
		}

		using var nuTexContainer = uiTex.GetChildItem(1);
		if (nuTexContainer is not { Count: > 0 }) {
			return false;
		}

		using var nuTex0 = nuTexContainer.GetChildItem(0);
		return IsNuTextureAsset(nuTex0);
	}

	private static bool IsNuTextureAsset(FHMFile? fhm) {
		// nu always has at least 2 elements, one for the header, second for the data.
		if (fhm is not { Count: > 1 }) {
			return false;
		}

		return fhm.CheckMagic(0, ResourceMagic.NuTexture) && fhm.ItemHeaders.All(header => header.Type == FHMItemType.Normal);
	}
}
