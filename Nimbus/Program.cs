// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System;
using Avalonia;

namespace Nimbus;

internal static class Program {
	[STAThread]
	public static void Main(string[] args) =>
		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);

	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
					 .UsePlatformDetect()
					 .LogToTrace();
}
