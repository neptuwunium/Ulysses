// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Pluto.CommandLine;

namespace Ulysses.DPLUnpack;

public record ProgramFlags : CommandLineFlags {
	[Flag("input", Positional = 0, IsRequired = true)]
	public string InputPath { get; set; } = null!;

	[Flag("output", Positional = 1, IsRequired = true)]
	public string OutputPath { get; set; } = null!;

	[Flag("convert")]
	public bool Convert { get; set; }

	[Flag("only-convert")]
	public bool OnlyConvert { get; set; }

	[Flag("convert-or-raw")]
	public bool ConvertOrRaw { get; set; }

	[Flag("fhm")]
	public bool SaveFHM { get; set; }

	[Flag("only-fhm")]
	public bool OnlyFHM { get; set; }

	[Flag("fhm-shape")]
	public bool FHMShape { get; set; }

	[Flag("merged-pac")]
	public bool Merged { get; set; }
}
