// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pluto.IO.Binary;
using Ulysses.Struct;
using Ulysses.Struct.FHM;

namespace Ulysses.Resources;

public abstract class Resource : IDisposable {
	protected Resource(FHMFile fhm, string name, bool leaveOpen = false) {
		FHM = fhm;
		Name = name;
		LeaveOpen = leaveOpen;
		ResourceCount = 0;
	}

	public static JsonSerializerOptions JsonSettings { get; } = new() {
		WriteIndented = true,
		NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public static bool RebuildActors { get; set; } = true;
	public FHMFile FHM { get; private set; }
	public string Name { get; private set; }
	public bool LeaveOpen { get; }
	public virtual int ResourceCount { get; }
	public bool IsFullyUtilized { get; protected set; }

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~Resource() => Dispose(false);
	public abstract string? GetResourceName(int resourceIndex, string baseName);
	public abstract bool Save(Stream stream, int resourceIndex);

	protected virtual void Dispose(bool disposing) {
		if (!disposing) {
			return;
		}

		if (LeaveOpen) {
			return;
		}

		FHM.Dispose();
		FHM = null!;
	}

	public static Resource? Construct(FHMFile fhm, FHMItemHeader fhmItem, int fhmIndex, string name, bool leaveOpen = false) {
		if (TryConstructViaShape(fhm, fhmItem, fhmIndex, name, out var instance, leaveOpen) ||
			TryConstructViaMagic(fhm, fhmItem, fhmIndex, name, out instance, leaveOpen)) {
			return instance;
		}

		return null;
	}

	public static bool TryConstructViaShape(FHMFile fhm, FHMItemHeader item, int index, string name, [MaybeNullWhen(false)] out Resource instance, bool leaveOpen = false) {
		instance = null;
		if (index == 0) {
			if (TryConstructActor(fhm, name, out instance, leaveOpen)) {
				return true;
			}
		}

		return false;
	}

	public static bool TryConstructActor(FHMFile fhm, string name, [MaybeNullWhen(false)] out Resource instance, bool leaveOpen = false) {
		instance = null;

		if (fhm.Count < 9) {
			return false;
		}

		if (!fhm.CheckMagic(0, ResourceMagic.NuModelPS3)) {
			return false;
		}

		if (fhm.GetItemDataHeader(^2).Size != 0x30) {
			return false;
		}

		if (fhm.GetItemDataHeader(^3).Size != 0x400) {
			return false;
		}

		if (!fhm.IsDataOnly) {
			return false;
		}

		instance = RebuildActors ? new MAGEActor(fhm, name, leaveOpen) : new ACEActor(fhm, name, leaveOpen);
		return true;
	}

	public static bool TryConstructViaMagic(FHMFile fhm, FHMItemHeader item, int index, string name, [MaybeNullWhen(false)] out Resource instance, bool leaveOpen = false) {
		using var buf = fhm[item];
		instance = null;

		if (buf.Length < 4) {
			return false;
		}

		var magic = MemoryMarshal.Read<ResourceMagic>(buf.Span);

		// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
		switch (magic) {
			case ResourceMagic.NuTexturePS3 when index == 0:
				instance = new NuTexture(fhm, item, 0, name, leaveOpen);
				return true;
			case ResourceMagic.UITexture:
				instance = new UITexture(fhm, item, name, leaveOpen);
			 	return true;
			// case ResourceMagic.UIImage:
			//	instance = new UI2DImage(fhm, item, name, leaveOpen);
			//	return true;
			case ResourceMagic.UIFont:
				instance = new UIFont(fhm, item, name, leaveOpen);
				return true;
			case ResourceMagic.ACEText:
				instance = new ACEText(fhm, item, name, leaveOpen);
				return true;
			case ResourceMagic.ACETable:
				instance = new ACETable(fhm, item, name, leaveOpen);
				return true;
		}

		return false;
	}
}

public readonly record struct RebuiltAsset(IRentedArray<byte> Buffer, string Name) : IDisposable {
	public void Dispose() => Buffer.Dispose();
}
