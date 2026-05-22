// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Text.Json.Serialization;
using Pluto.SourceGen.ReverseEndiannessGenerator;
using Pluto.SourceGen.TransparentStructGenerator;
using Ulysses.Json;

namespace Ulysses.Struct;

[EndianSwappable, TransparentStruct<uint>, JsonConverter(typeof(ACEDateConverter))]
public partial struct ACEDate {
	public DateTimeOffset DateTime {
		get {
			if (this == default) {
				return default;
			}

			var year = (int) (Value / 10000);
			var month = (int) (Value / 100 % 100);
			var day = (int) (Value % 100);
			return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.FromHours(9));
		}
	}

	public override string ToString() => DateTime.ToString("yyyy-MM-dd");
}

[EndianSwappable, TransparentStruct<uint>, JsonConverter(typeof(ACEDateConverter))]
public partial struct ACEDateRev {
	public DateTimeOffset DateTime {
		get {
			if (this == default) {
				return default;
			}

			var v = Value / 100;
			var year = (int) (v / 10000);
			var month = (int) (v / 100 % 100);
			var day = (int) (v % 100);
			return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.FromHours(9));
		}
	}

	public int Revision => (int) (Value % 100);

	public override string ToString() => $"{DateTime:yyyy-MM-dd} v{Revision}";
}
