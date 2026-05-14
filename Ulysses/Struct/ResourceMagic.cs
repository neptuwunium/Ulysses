// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Pluto.SourceGen.MagicGenerator;

namespace Ulysses.Struct;

[GenerateMagic("ResourceMagic", altPrint: "Ext")]
[Magic("\eLua", "LuaBytecode", ".lua", false)]
[Magic("@UTF", "CriWare", ".cpk", false)]
[Magic("#sys", "Script", ".txt", false)]
[Magic("ACF\0", "Font", ".acf", false)]
[Magic("ACS\0", "TextStyle", ".acs", false)]
[Magic("ACT\0", "Text", ".act", false)]
[Magic("BCF ", "BinaryCollector", ".bcf", false)]
[Magic("BFX ", "BinaryEffect", ".bfx", false)]
[Magic("BDD ", "BinaryCloud", ".bdd", false)]
[Magic("BSO ", "BinarySprite", ".bso", false)]
[Magic("COLH", "Collision", ".coll", false)]
[Magic("CPM\0", "Credits", ".cpm", false)]
[Magic("LAR ", "LAR", ".lar", false)]
[Magic("LVST", "Table", ".lvst", false)]
[Magic("MATE", "Material", ".mat", false)]
[Magic("MNT\0", "ModelNodeTree", ".mnt", false)]
[Magic("MOP2", "MOP", ".mop", false)]
[Magic("NFIC", "Scene", ".scene", false)]
[Magic("NME ", "Effect", ".nme", false)]
[Magic("NDP3", "NuModel", ".nud", false)]
[Magic("NSP3", "NuShader", ".nus", false)]
[Magic("NTP3", "NuTexture", ".nut", false)]
[Magic("PCP ", "CameraPass", ".pcp", false)]
[Magic("SAVE", "Save", ".save", false)]
[Magic("TWSA", "TWSA", ".tws", false)]
[Magic("UI2D", "UIImage", ".ui", false)]
[Magic("UITX", "UITexture", ".uitx", false)]
[Magic("wfdc", "WorldFileData", ".wfd", false)]
[Magic("wpdc", "WorldPlaceData", ".wpd", false)]
[Magic("TPS\0", "Tips", ".tips", false)]
[Magic("UIF\0", "Objectives", ".objectives", false)]
[Magic("ACH\0", "HCA", ".hca", false)]
[Magic("SKI\0", "Skill", ".skill", false)]
[Magic("SEF\0", "Skill2", ".skill", false)]
[Magic("PMM\0", "Maneuver", ".maneuver", false)]
[Magic("AFS\0", "AWB", ".awb", false)]
[Magic("FHM\x01", "FHM", ".fhm", false)]
[Magic("DPL\x01", "DPL", ".pac", false)]
public static partial class ResourceMagicExtensions;
