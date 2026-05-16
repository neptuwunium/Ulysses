// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Pluto;
using Pluto.IO.Binary;
using Ulysses.Struct;
using Ulysses.Struct.FHM;

namespace Ulysses;

public interface IResourceConverter {
	IEnumerable<RebuiltAsset>? Uncook(FHMFile fhm);
}

public readonly record struct RebuiltAsset(IRentedArray<byte> Buffer, string Extension) : IDisposable {
	public void Dispose() => Buffer.Dispose();
}

public sealed class TextureConverter : IResourceConverter {
	public IEnumerable<RebuiltAsset>? Uncook(FHMFile fhm) => null;
}

public sealed class UITextureConverter : IResourceConverter {
	public IEnumerable<RebuiltAsset>? Uncook(FHMFile fhm) => null;
}

public static class ResourceConverter {
	public delegate bool IsValidFor(FHMFile fhm);

	public static Dictionary<IsValidFor, IResourceConverter> ConverterFunclets { get; } = new() {
		[IsNuTextureAsset] = Singleton<TextureConverter>.Instance,
		[IsUIImageAsset] = Singleton<UITextureConverter>.Instance,
		[IsUITextureAsset] = Singleton<UITextureConverter>.Instance,
		[IsUIFontAsset] = Singleton<UITextureConverter>.Instance,
	};

	public static IResourceConverter? FindConverter(FHMFile file) {
		foreach (var (test, instance) in ConverterFunclets) {
			if (test(file)) {
				return instance;
			}
		}

		return null;
	}

	public static bool IsUIFontAsset(FHMFile fhm) {
		if (fhm.Count != 2) {
			return false;
		}

		// UIFont -> UIImage
		using var ui = fhm.GetChildItem(1);
		if (ui == null) {
			return false;
		}

		return fhm.CheckMagic(0, ResourceMagic.UIFont) && IsUIImageAsset(ui);
	}

	public static bool IsUIImageAsset(FHMFile fhm) {
		if (IdRegistry.Lookup.TryGetValue(fhm.Header.HashId, out var name) && name.StartsWith("DPL_2DIMAGE_", StringComparison.Ordinal)) {
			return true;
		}

		if (fhm.Count == 1) {
			if (fhm.GetChildItem(0) is { } nested) {
				fhm = nested;
			} else {
				return false;
			}
		}

		// UIImage -> UITexture -> NuImage[]
		using var ui = fhm.GetChildItem(0);
		if (ui is not { Count: 2 } || !ui.CheckMagic(0, ResourceMagic.UIImage)) {
			return false;
		}

		return ui.GetChildItem(1) is { } uiTex && IsUITextureAsset(uiTex);
	}

	public static bool IsUITextureAsset(FHMFile fhm) {
		if (fhm is not { Count: 2 } || !fhm.CheckMagic(0, ResourceMagic.UITexture)) {
			return false;
		}

		using var nuTexContainer = fhm.GetChildItem(1);
		if (nuTexContainer is not { Count: > 0 }) {
			return false;
		}

		using var nuTex0 = nuTexContainer.GetChildItem(0);
		return IsNuTextureAsset(nuTex0);
	}

	public static bool IsNuTextureAsset(FHMFile? fhm) {
		// nu always has at least 2 elements, one for the header, second for the data.
		if (fhm is not { Count: > 1 }) {
			return false;
		}

		return fhm.CheckMagic(0, ResourceMagic.NuTexturePS3) && fhm.ItemHeaders.All(header => header.Type == FHMItemType.Normal);
	}
}
