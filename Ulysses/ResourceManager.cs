// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Diagnostics.CodeAnalysis;
using Pluto;
using Pluto.IO.Binary;
using Pluto.IO.FileSystem;
using Serilog;
using Ulysses.Struct;
using Ulysses.Struct.FHM;

namespace Ulysses;

public sealed class ResourceManager : IDisposable {
	private ResourceManager() {
		DPL = ObjectPool<Dictionary<HashId, DPLFile>>.Rent();
		FHM = ObjectPool<Dictionary<HashId, HashId>>.Rent();
	}

	[field: AllowNull, MaybeNull]
	public static ResourceManager Instance {
		get {
			field ??= new ResourceManager();
			return field;
		}
		private set;
	}

	public Dictionary<HashId, DPLFile> DPL { get; }
	public Dictionary<HashId, HashId> FHM { get; }

	public void Dispose() {
		foreach (var dpl in DPL.Values) {
			dpl.Dispose();
		}

		DPL.Clear();
		FHM.Clear();
		ObjectPool<Dictionary<HashId, DPLFile>>.Return(DPL);
		ObjectPool<Dictionary<HashId, HashId>>.Return(FHM);

		Instance = null!;
	}

	public void Mount(string path) {
		foreach (var pacPath in new FileEnumerator(path, new EnumerationOptions { RecurseSubdirectories = true }, "*.PAC")) {
			var priority = DPLFile.GetPriority(pacPath);
			if (priority < 0) {
				continue;
			}

			var dpl = new DPLFile(pacPath, priority);
			Log.Information("Mounted DPL {DPLName} (Version {Version}, Build Date {Build})", dpl.Name, dpl.Header.ACE.Version, dpl.Header.ACE.Date);
			DPL.Add(new HashId(dpl.Name), dpl);
		}
	}

	public void Collect() {
		foreach (var (dplId, dpl) in DPL.OrderByDescending(x => x.Value.Priority)) {
			foreach (var fhm in dpl.FHMTable.Keys) {
				FHM.TryAdd(fhm, dplId);
			}
		}
	}

	public IRentedArray<byte>? ReadFile(HashId id, out FHMHeader header) {
		header = default;

		if (!FHM.TryGetValue(id, out var dplId)) {
			return null;
		}

		if (DPL[dplId].FHMTable.TryGetValue(id, out var fhm)) {
			header = fhm.Header;
		}

		return DPL[dplId].ReadFile(id);
	}
}
