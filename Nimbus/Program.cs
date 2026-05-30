// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.IO;
using Avalonia;
using Serilog;

namespace Nimbus;

internal static class Program {
	[STAThread]
	public static void Main(string[] args) {
		if (File.Exists("Nimbus.log")) {
			File.Delete("Nimbus.log");
		}

		Log.Logger = new LoggerConfiguration()
					 .MinimumLevel.Debug()
					 .WriteTo.File("Nimbus.log")
					 .CreateLogger();

		AppDomain.CurrentDomain.UnhandledException += (_, e) => {
			Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception");
		};

		AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.CloseAndFlush();

		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
					 .UsePlatformDetect()
					 .LogToTrace();
}
