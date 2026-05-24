// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text.Json;
using Ulysses.Resources.Data;
using Ulysses.Struct.FHM;

namespace Ulysses.Resources;

public class ACEText : Resource {
	public ACEText(FHMFile fhm, FHMItemHeader item, string name, bool leaveOpen = false) : base(fhm, name, leaveOpen) {
		if (fhm[item] is not { Length: > 0 } data) {
			return;
		}

		Data = new ACETextData(data);
		IsFullyUtilized = fhm.Count == 1;
	}

	public ACETextData? Data { get; private set; }
	public override int ResourceCount => Data != null ? 1 : 0;

	public override string? GetResourceName(int resourceIndex, string baseName) => Data != null ? baseName + ".json" : null;

	public override bool Save(Stream stream, int resourceIndex) {
		if (Data == null) {
			return false;
		}

		JsonSerializer.Serialize(stream, Data, JsonSettings);
		return true;
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			Data?.Dispose();
			Data = null;
		}

		base.Dispose(disposing);
	}
}
