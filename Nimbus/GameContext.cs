// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Threading;
using Nimbus.DplDto;
using Pluto;
using Serilog;
using Ulysses;
using Ulysses.Resources;
using Ulysses.Struct;

namespace Nimbus;

public static partial class GameContext {
	public static ResourceManager? Manager { get; set; }

	public static Dictionary<string, string?>? Localization { get; set; }
	public static Dictionary<int, PlaneInformation>? PlaneInformation { get; set; }
	public static Dictionary<string, List<ColorInformation>>? ColorInformation { get; set; }

	[GeneratedRegex(@"\p{IsPrivateUse}")]
	private static partial Regex RemovePrivateUse { get; }

	public static void Load(string path) {
		if (ColorInformation is not null) {
			foreach (var color in ColorInformation.Values) {
				color.Clear();
				ObjectPool<List<ColorInformation>>.Return(color);
			}
		}

		PlaneInformation?.Clear();
		ObjectPool<Dictionary<int, PlaneInformation>>.Return(PlaneInformation);
		PlaneInformation = null;

		ColorInformation?.Clear();
		ObjectPool<Dictionary<string, List<ColorInformation>>>.Return(ColorInformation);
		ColorInformation = null!;

		Localization?.Clear();
		ObjectPool<Dictionary<string, string?>>.Return(Localization);
		Localization = null;

		Log.Information("Mounting {Path}", path);
		Manager?.Unmount();
		Manager = ResourceManager.Instance;
		Manager.Mount(path);
		Manager.Collect();

		Dispatcher.UIThread.Invoke(() => {
			Localization = ObjectPool<Dictionary<string, string?>>.Rent();
			Localization.Clear();


			Log.Information("Reading Tables...");
			LoadDPLInformation();
			Log.Information("Reading Localization...");
			LoadLocalization();
			Log.Information("Reading Old Localization...");
			LoadOldLocalization();
			Log.Information("Reading Root Localization...");
			LoadRootLocalization();
			Log.Information("Reading Menu Localization...");
			LoadMenuText();
			Log.Information("Done!");
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
		try {
			using var info = new ACEText(fhm, fhm.GetItemHeader(index), name, true);
			if (info.Data is not { } data) {
				return;
			}

			foreach (var (hash, hashInfo) in data.Hashes) {
				var text = data.GetStringForLanguage(hash, 0);
				if (string.IsNullOrEmpty(text)) {
					continue;
				}

				Localization!.TryAdd(hashInfo.Label, RemovePrivateUse.Replace(text, ""));
			}
		} catch (Exception ex) {
			Log.Error(ex, "Cannot load text...");
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

	private static void LoadDPLInformation() {
		using var infoFhm = Manager?.ReadFile("DPL_INFORMATION");
		if (infoFhm is null) {
			return;
		}

		try {
			Log.Information("Loading Plane Information...");
			using (var info = new ACETable(infoFhm, infoFhm.GetItemHeader(6), "DPL_INFORMATION", true)) {
				if (info.Data is { } data) {
					PlaneInformation = ObjectPool<Dictionary<int, PlaneInformation>>.Rent();
					PlaneInformation.Clear();

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
		} catch (Exception ex) {
			Log.Error(ex, "Cannot load plane information...");
		}

		try {
			Log.Information("Loading Color Information...");
			using (var info = new ACETable(infoFhm, infoFhm.GetItemHeader(7), "DPL_INFORMATION", true)) {
				if (info.Data is { } data) {
					ColorInformation = ObjectPool<Dictionary<string, List<ColorInformation>>>.Rent();
					ColorInformation.Clear();

					foreach (var row in data.GetRows()) {
						var dto = new ColorInformation(row);

						if (!ColorInformation.TryGetValue(dto.AircraftId, out var planeColor)) {
							planeColor = ColorInformation[dto.AircraftId] = ObjectPool<List<ColorInformation>>.Rent();
							planeColor.Clear();
						}

						planeColor.Add(dto);
					}
				}
			}
		} catch (Exception ex) {
			Log.Error(ex, "Cannot load color information...");
		}
	}
}
