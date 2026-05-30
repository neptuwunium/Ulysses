// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Ulysses.Struct;

namespace Nimbus.DplDto;

public enum PlaneCategory {
	Fighter,
	Multirole,
	Attacker,
	Bomber,
	PistonFighter,
}

public class PlaneInformation : AvaloniaObject {
	public static readonly DirectProperty<PlaneInformation, bool> ExtractProperty = AvaloniaProperty.RegisterDirect<PlaneInformation, bool>("Extract", o => o.Extract, (o, v) => o.Extract = v);

	public PlaneInformation(Dictionary<HashId, object?> row) {
		Version = row.GetCellOrDefault("ver", Version);
		AircraftName = row.GetCellOrDefault("AcName", AircraftName);
		AircraftId = row.GetCellOrDefault("AcId", AircraftId);
		BaseAircraftId = row.GetCellOrDefault(0x86f9c888, AircraftId);
		Category = (PlaneCategory) row.GetCellOrDefault("Category", 0);
		Rarity = row.GetCellOrDefault("Rarity", Rarity) + 1;
		Arms1 = row.GetCellOrDefault(0x16a75a36, default(HashId)).ToString();
		Arms2 = row.GetCellOrDefault(0x1be47cef, default(HashId)).ToString();
		Arms3 = row.GetCellOrDefault(0x1f256158, default(HashId)).ToString();
	}

	public int Version { get; set; }
	public string AircraftName { get; set; } = "none";
	public int AircraftId { get; set; }
	public int BaseAircraftId { get; set; }
	[field: MaybeNull] public string BaseAircraftName { get => field ?? (AircraftId != BaseAircraftId ? BaseAircraftId.ToString("D", null) : string.Empty); set; }
	public PlaneCategory Category { get; set; }
	public int Rarity { get; set; }
	public string Arms1 { get; set; }
	public string Arms2 { get; set; }
	public string Arms3 { get; set; }
	public string FriendlyName => GameContext.Localization?.GetValueOrDefault($"AcName_{AircraftName}") ?? AircraftName;

	public string FriendlyBaseName => AircraftId != BaseAircraftId ? GameContext.Localization?.GetValueOrDefault($"AcName_{BaseAircraftName}") ?? BaseAircraftName : "";

	public bool Extract {
		get;
		set => SetAndRaise(ExtractProperty, ref field, value);
	}
}
