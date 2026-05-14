// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Pluto.SourceGen.ReverseEndiannessGenerator;

namespace Ulysses.Struct.ACT;

[EndianSwappable, StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0xC)]
public readonly partial record struct ACTHashMeta(HashId Hash, int Offset, int Index);
