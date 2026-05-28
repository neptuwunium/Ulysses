// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Nimbus.Converters;

public class InvertBooleanConverter : IValueConverter {
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => !(bool) value!;
	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => !(bool) value!;
}
