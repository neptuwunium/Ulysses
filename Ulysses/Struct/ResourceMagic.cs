// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Pluto.SourceGen.MagicGenerator;

namespace Ulysses.Struct;

[GenerateMagic("ResourceMagic", altPrint: "Ext"), Magic("\eLua", "LuaBytecode", ".lua"), Magic("@UTF", "CriWare", ".cpk"), Magic("#sys", "Script", ".txt"), Magic("BCF ", "BinaryCollector", ".bcf"), Magic("BFX ", "BinaryEffect", ".bfx"), Magic("BDD ", "BinaryCloud", ".bdd"), Magic("BSO ", "BinarySprite", ".bso"), Magic("COLH", "Collision", ".coll"), Magic("CPM\0", "Credits", ".cpm"), Magic("LAR ", "LAR", ".lar"), Magic("MATE", "Material", ".mat"), Magic("MNT\0", "ModelNodeTree", ".mnt"), Magic("MOP2", "MOP", ".mop"), Magic("NFIC", "Scene", ".scene"), Magic("NME ", "Effect", ".nme"), Magic("PCP ", "CameraPass", ".pcp"), Magic("SAVE", "Save", ".save"), Magic("TWSA", "TWSA", ".tws"), Magic("UI2D", "UIImage", ".ui"), Magic("UITX", "UITexture", ".uitx"), Magic("ACF\0", "UIFont", ".uifont"), Magic("wfdc", "WorldFileData", ".wfd"), Magic("wpdc", "WorldPlaceData", ".wpd"), Magic("TPS\0", "Tips", ".tips"), Magic("UIF\0", "Objectives", ".objectives"), Magic("ACH\0", "HCA", ".hca"),
 Magic("SKI\0", "Skill", ".skill"), Magic("SEF\0", "Skill2", ".skill"), Magic("PMM\0", "Maneuver", ".maneuver"), Magic("AFS\0", "AWB", ".awb"), Magic("FHM\x01", "FHM", ".fhm"), Magic("DPL\x01", "DPL", ".pac"), Magic("ACS\0", "ACETextStyle", ".acs"), Magic("ACT\0", "ACEText", ".act"), Magic("LVST", "ACETable", ".lvst"), Magic("NDP3", "NuModelPS3", ".nud"), Magic("NSP3", "NuShaderPS3", ".nus"), Magic("NTP3", "NuTexturePS3", ".nut"), Magic("GIDX", "GlobalIndex", ".gidx")]
// handled:
public static partial class ResourceMagicExtensions;
