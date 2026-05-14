// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Pluto.IO.Binary;

namespace Ulysses.Struct.ACT;

public readonly record struct ACTTextRef(string Label, IRentedArray<int> Text) : IDisposable {
	public void Dispose() => Text.Dispose();
}
