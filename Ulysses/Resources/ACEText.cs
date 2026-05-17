// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text.Json;
using Ulysses.Resources.Data;
using Ulysses.Struct.FHM;

namespace Ulysses.Resources;

public class ACEText : Resource {
	public ACEText(FHMFile fhm, string name, FHMItemHeader item, bool leaveOpen = false) : base(fhm, name, leaveOpen) {
		if (fhm.GetItemData(item) is not { } data) {
			return;
		}

		ResourceCount = 1;
		Data = new ACETextData(fhm.GetItemData(item));
		IsFullyUtilized = fhm.Count == 1;
	}

	public ACETextData? Data { get; private set; }

	public override IEnumerable<RebuiltAsset>? Uncook() => null;
	public override RebuiltAsset? Uncook(int resourceIndex) => null;
	public override string? GetResourceName(int resourceIndex, string prefix) => Data != null ? Name + prefix + ".json" : null;

	public override bool UncookToStream(Stream stream, int resourceIndex) {
		if (Data == null) {
			return false;
		}

		JsonSerializer.Serialize(stream, Data, JsonSettings);
		return true;
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			Data?.Dispose();
			ResourceCount = 0;
			Data = null;
		}

		base.Dispose(disposing);
	}
}

public class ACETable : Resource {
	public ACETable(FHMFile fhm, string name, FHMItemHeader item, bool leaveOpen = false) : base(fhm, name, leaveOpen) {
		if (fhm.GetItemData(item) is not { } data) {
			return;
		}

		ResourceCount = 1;
		Data = new ACETableData(fhm.GetItemData(item));
		IsFullyUtilized = fhm.Count == 1;
	}

	public ACETableData? Data { get; private set; }

	public override IEnumerable<RebuiltAsset>? Uncook() => null;
	public override RebuiltAsset? Uncook(int resourceIndex) => null;
	public override string? GetResourceName(int resourceIndex, string prefix) => Data != null ? prefix + Name + ".json" : null;

	public override bool UncookToStream(Stream stream, int resourceIndex) {
		if (Data == null) {
			return false;
		}

		JsonSerializer.Serialize(stream, Data, JsonSettings);
		return true;
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			Data?.Dispose();
			ResourceCount = 0;
			Data = null;
		}

		base.Dispose(disposing);
	}
}
