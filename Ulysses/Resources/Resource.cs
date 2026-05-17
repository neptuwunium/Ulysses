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
	}

	public static JsonSerializerOptions JsonSettings { get; } = new() {
		WriteIndented = true,
		NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public FHMFile FHM { get; private set; }
	public string Name { get; private set; }
	public bool LeaveOpen { get; }
	public int ResourceCount { get; protected set; }
	public bool IsFullyUtilized { get; protected set; }

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~Resource() => Dispose(false);
	public abstract IEnumerable<RebuiltAsset>? Uncook();
	public abstract RebuiltAsset? Uncook(int resourceIndex);
	public abstract string? GetResourceName(int resourceIndex, string prefix);
	public abstract bool UncookToStream(Stream stream, int resourceIndex);

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

	public static Resource? Construct(FHMFile fhm, string name, FHMItemHeader item, bool leaveOpen = false) {
		if (TryConstructViaShape(fhm, name, item, out var instance, leaveOpen) ||
			TryConstructViaMagic(fhm, name, item, out instance, leaveOpen)) {
			return instance;
		}

		return null;
	}

	public static bool TryConstructViaShape(FHMFile fhm, string name, FHMItemHeader item, [MaybeNullWhen(false)] out Resource instance, bool leaveOpen = false) {
		instance = null;
		return false;
	}

	public static bool TryConstructViaMagic(FHMFile fhm, string name, FHMItemHeader item, [MaybeNullWhen(false)] out Resource instance, bool leaveOpen = false) {
		using var buf = fhm.GetItemData(item);
		instance = null;

		if (buf.Length < 4) {
			return false;
		}

		var magic = MemoryMarshal.Read<ResourceMagic>(buf.Span);

		// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
		switch (magic) {
			case ResourceMagic.NuTexturePS3:
				instance = new NuTexture(fhm, name, item, leaveOpen);
				return true;
			case ResourceMagic.UITexture:
				instance = new UITexture(fhm, name, item, leaveOpen);
				return true;
			case ResourceMagic.UIImage:
				instance = new UI2DImage(fhm, name, item, leaveOpen);
				return true;
			case ResourceMagic.UIFont:
				instance = new UIFont(fhm, name, item, leaveOpen);
				return true;
			case ResourceMagic.ACEText:
				instance = new ACEText(fhm, name, item, leaveOpen);
				return true;
			case ResourceMagic.ACETable:
				instance = new ACETable(fhm, name, item, leaveOpen);
				return true;
		}

		return false;
	}
}

public readonly record struct RebuiltAsset(IRentedArray<byte> Buffer, string Name) : IDisposable {
	public void Dispose() => Buffer.Dispose();
}
