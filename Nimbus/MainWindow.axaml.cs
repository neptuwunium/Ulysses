// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Nimbus.DplDto;
using Pluto.Extensions;
using SukiUI.Controls;
using Triton.Encoder;
using Ulysses.Resources;
using Ulysses.Struct.Nu;

namespace Nimbus;

public partial class MainWindow : SukiWindow {
	public static readonly DirectProperty<MainWindow, string> StatusProperty = AvaloniaProperty.RegisterDirect<MainWindow, string>("Status", o => o.Status, (o, v) => o.Status = v);
	public static readonly DirectProperty<MainWindow, bool> AllowButtonsProperty = AvaloniaProperty.RegisterDirect<MainWindow, bool>("AllowButtons", o => o.AllowButtons, (o, v) => o.AllowButtons = v);
	public static readonly DirectProperty<MainWindow, bool> PNGSelectedProperty = AvaloniaProperty.RegisterDirect<MainWindow, bool>("PNGSelected", o => o.PNGSelected, (o, v) => o.PNGSelected = v);
	public static readonly DirectProperty<MainWindow, bool> TIFFSelectedProperty = AvaloniaProperty.RegisterDirect<MainWindow, bool>("TIFFSelected", o => o.PNGSelected, (o, v) => o.PNGSelected = v);
	public static readonly DirectProperty<MainWindow, bool> DDSSelectedProperty = AvaloniaProperty.RegisterDirect<MainWindow, bool>("DDSSelected", o => o.PNGSelected, (o, v) => o.PNGSelected = v);
	public static readonly DirectProperty<MainWindow, Dictionary<int, PlaneInformation>?> PlanesProperty = AvaloniaProperty.RegisterDirect<MainWindow, Dictionary<int, PlaneInformation>?>("Planes", o => o.Planes, (o, v) => o.Planes = v);

	public MainWindow() {
		AllowButtons = true;
		Status = string.Empty;
		InitializeComponent();
		Dispatcher.AwaitWithPriority(Reset(), DispatcherPriority.Normal);
	}

	public Dictionary<int, PlaneInformation>? Planes {
		get;
		set => SetAndRaise(PlanesProperty, ref field, value);
	}

	public static PlaneInformation[] EmptyPlanes => [];

	public bool PNGSelected {
		get;
		set {
			if (value) {
				NuTexture.ExportFormat = NuExportFormat.PNG;
			}

			SetAndRaise(PNGSelectedProperty, ref field, value);
		}
	}

	public bool TIFFSelected {
		get;
		set {
			if (value) {
				NuTexture.ExportFormat = NuExportFormat.TIFF;
			}

			SetAndRaise(TIFFSelectedProperty, ref field, value);
		}
	}

	public bool DDSSelected {
		get;
		set {
			if (value) {
				NuTexture.ExportFormat = NuExportFormat.DDS;
			}

			SetAndRaise(DDSSelectedProperty, ref field, value);
		}
	}

	public bool AllowButtons {
		get;
		set => SetAndRaise(AllowButtonsProperty, ref field, value);
	}

	public string Status {
		get;
		set => SetAndRaise(StatusProperty, ref field, value);
	}

	private IStorageFolder? LastFolder { get; set; }
	public static bool PNGSupported => PNGEncoder.IsAvailable;
	public static bool TIFFSupported => TIFFEncoder.IsAvailable;

	public async Task Reset() {
		await Task.Delay(200);

		if (NuTexture.ExportFormat == NuExportFormat.PNG) {
			PNGSelected = true;
		} else {
			DDSSelected = true;
		}

		Status = "Ready";
	}

	private void ToggleAll(object? sender, RoutedEventArgs routedEventArgs) {
		if (Planes == null) {
			return;
		}

		var selected = (sender as CheckBox)?.IsChecked ?? false;
		foreach (var plane in Planes.Values) {
			plane.Extract = selected;
		}
	}

	private void LoadGame(object? sender, RoutedEventArgs e) => Dispatcher.AwaitWithPriority(LoadGameAsync(), DispatcherPriority.Normal);

	private async Task LoadGameAsync() {
		var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
			Title = "Open ACE COMBAT INFINITY USRDIR",
			SuggestedFileName = "USRDIR",
			SuggestedStartLocation = LastFolder ?? await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop),
			AllowMultiple = false,
		});

		if (result.Count == 0 || result[0].TryGetLocalPath() is not { } path) {
			return;
		}

		LastFolder = result[0];

		new Thread(() => {
			try {
				Dispatcher.UIThread.Invoke(() => {
					Status = "Loading...";
					AllowButtons = false;
				});
				GameContext.Load(path);
			} finally {
				Dispatcher.UIThread.Invoke(() => {
					Planes = GameContext.PlaneInformation;
					Status = string.Empty;
					AllowButtons = true;
				});
			}
		}) { Name = "LoadDPL"}.Start();
	}

	private void ExtractGame(object? sender, RoutedEventArgs e) => Dispatcher.AwaitWithPriority(ExtractGameAsync(), DispatcherPriority.Normal);

	private async Task ExtractGameAsync() {
		if (Planes == null || GameContext.Manager is not { } manager) {
			return;
		}

		var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
			Title = "Extract to...",
			SuggestedStartLocation = LastFolder ?? await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop),
			AllowMultiple = false,
		});


		if (result.Count == 0 || result[0].TryGetLocalPath() is not { } path) {
			return;
		}

		LastFolder = result[0];

		new Thread(() => {
			try {
				var toExtract = Planes.Values.Where(x => x.Extract);
				foreach (var plane in toExtract) {
					Dispatcher.UIThread.Invoke(() => {
						Status = $"Extracting {plane.FriendlyName}...";
						AllowButtons = false;
					});

					var planePath = Path.Combine(path, plane.FriendlyName.SanitizeFilename().Replace("\\", "", StringComparison.Ordinal).Replace("/", "", StringComparison.Ordinal));

					foreach (var (prefix, name) in PlaneExtractor.Parts) {
						var dplName = $"{prefix}{plane.AircraftName.ToUpperInvariant()}";
						using var fhm = manager.ReadFile(dplName);
						if (fhm == null) {
							continue;
						}

						var partPath = Path.Combine(planePath, name);

						PlaneExtractor.SavePlane(fhm, partPath);
						PlaneExtractor.SaveEmblem($"{dplName}_EMBLEM", manager, Path.Combine(partPath, "emblem"));

						for (var index = 0; index < 40; ++index) {
							PlaneExtractor.SaveSkin($"{dplName}_T{index:D2}", manager, Path.Combine(partPath, $"t{index:D2}"));
							PlaneExtractor.SaveSkin($"{dplName}_E{index:D2}", manager, Path.Combine(partPath, $"e{index:D2}"));
						}
					}
				}
			} finally {
				Dispatcher.UIThread.Invoke(() => {
					Status = string.Empty;
					AllowButtons = true;

					foreach (var plane in Planes.Values) {
						plane.Extract = false;
					}
				});
			}
		}) { Name = "ExtractPlane" }.Start();
	}
}
