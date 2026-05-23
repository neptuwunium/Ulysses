// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Pluto.CommandLine;

namespace Ulysses.DPLUnpack;

public record ProgramFlags : CommandLineFlags {
	[Flag("input", Positional = 0, IsRequired = true, Help = "Path to PAC file or directory that has PAC files")]
	public string InputPath { get; set; } = null!;

	[Flag("output", Positional = 1, IsRequired = true, Help = "Path where files are going to be exported")]
	public string OutputPath { get; set; } = null!;

	[Flag("convert", Help = "Whether or not to convert assets if possible")]
	public bool Convert { get; set; }

	[Flag("only-convert", Help = "Only convert assets, do not dump raw data. Needs --convert")]
	public bool OnlyConvert { get; set; }

	[Flag("merged-pac", Help = "Merge PAC files so only the most recent files are saved")]
	public bool Merged { get; set; }

	[Flag("dry", Help = "Do not write any files")]
	public bool Dry { get; set; }

	[Flag("fhm", Help = "Save the raw FHM file", Hidden = true)]
	public bool SaveFHM { get; set; }

	[Flag("fhm-buffer", Help = "Save FHM buffers when they are contiguous", Hidden = true)]
	public bool SaveFHMBuffer { get; set; }

	[Flag("only-fhm", Help = "Do not process FHM files, only save them. Needs --fhm", Hidden = true)]
	public bool OnlyFHM { get; set; }

	[Flag("fhm-shape", Help = "Save the FHM shape for debugging", Hidden = true)]
	public bool FHMShape { get; set; }

	[Flag("dump-strings", Help = "Dump text strings that may exist in the binaries", Hidden = true)]
	public bool DumpStrings { get; set; }

	[Flag("dump-hashes", Help = "Dump any hashes that exist in the files", Hidden = true)]
	public bool DumpHashes { get; set; }
}
