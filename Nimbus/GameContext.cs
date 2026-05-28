// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Threading;
using Nimbus.DplDto;
using Pluto;
using Ulysses;
using Ulysses.Resources;
using Ulysses.Struct;

namespace Nimbus;

public static partial class GameContext {
	public static ResourceManager? Manager { get; set; }

	public static Dictionary<string, string?>? Localization { get; set; }
	public static Dictionary<int, PlaneInformation>? PlaneInformation { get; set; }

	[GeneratedRegex(@"\p{IsPrivateUse}")]
	private static partial Regex RemovePrivateUse { get; }

	public static void Load(string path) {
		ObjectPool<Dictionary<int, PlaneInformation>>.Return(PlaneInformation);
		ObjectPool<Dictionary<string, string?>>.Return(Localization);
		PlaneInformation = null;
		Localization = null;

		Manager?.Unmount();
		Manager = ResourceManager.Instance;
		Manager.Mount(path);
		Manager.Collect();

		Dispatcher.UIThread.Invoke(() => {
			LoadPlaneInformation();
			LoadLocalization();
			LoadOldLocalization();
			LoadRootLocalization();
			LoadMenuText();
		});
	}

	private static void LoadLocalization() {
		using var textFhm = Manager?.ReadFile("DPL_UI_COMMON_TEXT_US");
		using var containerFhm = textFhm?.GetChildItem(0);
		if (containerFhm is null) {
			return;
		}

		LoadText(containerFhm, 0, "DPL_UI_COMMON_TEXT_US");
	}

	private static void LoadOldLocalization() {
		var id = new HashId("DPL_UI_COMMON_TEXT_US");
		foreach (var (_, dpl) in Manager?.DPL ?? []) {
			if (!dpl.FHMTable.TryGetValue(id, out var info) || info.Header.IsDeleted) {
				continue;
			}

			using var fhmBuffer = dpl.ReadFile(id);
			if (fhmBuffer is null) {
				continue;
			}

			using var textFhm = new FHMFile(fhmBuffer, 0, info.Header);
			using var containerFhm = textFhm.GetChildItem(0);
			if (containerFhm is null) {
				continue;
			}

			LoadText(containerFhm, 0, "DPL_UI_COMMON_TEXT_US");
		}
	}

	private static void LoadRootLocalization() {
		var id = new HashId("DPL_UI_COMMON");
		foreach (var (_, dpl) in Manager?.DPL ?? []) {
			if (!dpl.FHMTable.TryGetValue(id, out var info) || info.Header.IsDeleted) {
				continue;
			}

			using var fhmBuffer = dpl.ReadFile(id);
			if (fhmBuffer is null) {
				continue;
			}

			using var textFhm = new FHMFile(fhmBuffer, 0, info.Header);
			using var containerFhm = textFhm.GetChildItem(0);
			if (containerFhm is null) {
				continue;
			}

			LoadText(containerFhm, 3, "DPL_UI_COMMON");
		}
	}

	private static void LoadText(FHMFile fhm, int index, string name) {
		using var info = new ACEText(fhm, fhm.GetItemHeader(index), name);
		if (info.Data is not { } data) {
			return;
		}

		Localization ??= ObjectPool<Dictionary<string, string?>>.Rent();
		foreach (var (hash, hashInfo) in data.Hashes) {
			var text = data.GetStringForLanguage(hash, 0);
			if (string.IsNullOrEmpty(text)) {
				continue;
			}

			Localization.TryAdd(hashInfo.Label, RemovePrivateUse.Replace(text, ""));
		}
	}

	private static void LoadMenuText() {
		using var menuFhm = Manager?.ReadFile("DPL_UI_MENU");
		using var containerFhm = menuFhm?.GetChildItem(0);
		if (containerFhm is null) {
			return;
		}

		LoadText(containerFhm, 155, "DPL_UI_MENU");
		LoadText(containerFhm, 160, "DPL_UI_MENU");
	}

	private static void LoadPlaneInformation() {
		using var infoFhm = Manager?.ReadFile("DPL_INFORMATION");
		if (infoFhm is null) {
			return;
		}

		using var info = new ACETable(infoFhm, infoFhm.GetItemHeader(6), "DPL_INFORMATION");
		if (info.Data is not { } data) {
			return;
		}

		PlaneInformation = ObjectPool<Dictionary<int, PlaneInformation>>.Rent();
		foreach (var row in data.GetRows()) {
			var dto = new PlaneInformation(row);
			PlaneInformation[dto.AircraftId] = dto;
		}

		foreach (var plane in PlaneInformation.Values) {
			if (plane.BaseAircraftId != plane.AircraftId && PlaneInformation.TryGetValue(plane.BaseAircraftId, out var basePlane)) {
				plane.BaseAircraftName = basePlane.AircraftName;
			}
		}
	}
}
