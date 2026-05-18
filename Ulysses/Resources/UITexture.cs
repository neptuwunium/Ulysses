// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Ulysses.Struct.FHM;

namespace Ulysses.Resources;

public class UITexture : Resource {
	public UITexture(FHMFile fhm, FHMItemHeader item, string name, bool leaveOpen = false) : base(fhm, name, leaveOpen) { }

	public override string? GetResourceName(int resourceIndex, string baseName) => null;
	public override bool Save(Stream stream, int resourceIndex) => false;
}
