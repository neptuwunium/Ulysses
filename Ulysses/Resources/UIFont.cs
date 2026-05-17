// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Ulysses.Struct.FHM;

namespace Ulysses.Resources;

public class UIFont : Resource {
	public UIFont(FHMFile fhm, string name, FHMItemHeader item, bool leaveOpen = false) : base(fhm, name, leaveOpen) { }

	public override IEnumerable<RebuiltAsset>? Uncook() => null;
	public override RebuiltAsset? Uncook(int resourceIndex) => null;
	public override string? GetResourceName(int resourceIndex, string prefix) => null;
	public override bool UncookToStream(Stream stream, int resourceIndex) => false;
}
