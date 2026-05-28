// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Collections.Generic;
using Ulysses.Struct;

namespace Nimbus.DplDto;

internal static class DictionaryExtensions {
	extension(Dictionary<HashId, object?> row) {
		public T GetCellOrDefault<T>(HashId id, T @default) => row.GetValueOrDefault(id) is T value ? value : @default;
	}
}
