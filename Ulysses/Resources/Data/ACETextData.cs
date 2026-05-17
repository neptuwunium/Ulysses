// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using Pluto;
using Pluto.Extensions;
using Pluto.IO.Binary;
using Ulysses.Json;
using Ulysses.Struct;
using Ulysses.Struct.ACT;

namespace Ulysses.Resources.Data;

[JsonConverter(typeof(ACTextConverter))]
public sealed class ACETextData : IDisposable {
	public ACETextData(IRentedArray<byte> buffer, bool leaveOpen = false) {
		Buffer = buffer;
		LeaveOpen = leaveOpen;

		using var reader = new ArrayPoolBinaryReader(buffer, true);

		Header = reader.Read<ACTHeader>().ReverseEndianness();

		Languages = ObjectPool<Dictionary<string, int>>.Rent();
		Languages.Clear();
		Languages.EnsureCapacity(Header.LanguageCount);

		Texts = ObjectPool<List<ACTTextRef>>.Rent();
		Texts.Clear();
		Texts.EnsureCapacity(Header.TextCount);

		Hashes = ObjectPool<Dictionary<HashId, ACTHash>>.Rent();
		Hashes.Clear();
		Hashes.EnsureCapacity(Header.HashCount);

		reader.Position = Header.LanguageTableOffset;
		for (var index = 0; index < Header.LanguageCount; ++index) {
			Languages[reader.ReadCString<byte>(Encoding.ASCII, 2, true)] = BinaryPrimitives.ReverseEndianness(reader.Read<ushort>());
		}

		reader.Position = Header.TextTableOffset;
		var textOffsets = reader.ReadShared<int>(Header.TextCount);
		var textOffsetsSp = textOffsets.Span;
		textOffsetsSp.ReverseEndianness();

		foreach (var offset in textOffsetsSp) {
			reader.Position = offset;
			var labelOffset = BinaryPrimitives.ReverseEndianness(reader.Read<int>());
			var offsets = reader.ReadShared<int>(Header.LanguageCount);
			offsets.Span.ReverseEndianness();
			Texts.Add(new ACTTextRef(ReadStringAt<byte>(Encoding.UTF8, labelOffset), offsets));
		}

		reader.Position = Header.HashTableOffset;
		var hashMeta = reader.ReadShared<ACTHashMeta>(Header.HashCount);
		var hashMetaSp = hashMeta.Span;
		hashMetaSp.ReverseEndianness();
		foreach (var meta in hashMetaSp) {
			var v = Hashes[meta.Hash] = new ACTHash(meta.Index, ReadStringAt<byte>(Encoding.UTF8, meta.Offset));
			IdRegistry.Register(meta.Hash, v.Label);
		}
	}

	public bool LeaveOpen { get; }
	public ACTHeader Header { get; }
	public IRentedArray<byte> Buffer { get; }
	public Dictionary<string, int> Languages { get; set; }
	public List<ACTTextRef> Texts { get; set; }
	public Dictionary<HashId, ACTHash> Hashes { get; set; }

	public void Dispose() {
		Languages.Clear();
		ObjectPool<Dictionary<string, int>>.Return(Languages);
		Languages = null!;

		foreach (var text in Texts) {
			text.Dispose();
		}

		Texts.Clear();
		ObjectPool<List<ACTTextRef>>.Return(Texts);
		Texts = null!;

		Hashes.Clear();
		ObjectPool<Dictionary<HashId, ACTHash>>.Return(Hashes);
		Hashes = null!;

		if (LeaveOpen) {
			return;
		}

		Buffer.Dispose();
	}

	private string ReadStringAt<T>(Encoding encoding, int offset) where T : unmanaged {
		if (offset == 0) {
			return string.Empty;
		}

		var codepoints = MemoryMarshal.Cast<byte, T>(Buffer.Span[offset..]);
		var zero = codepoints.IndexOf(default(T));
		if (zero == 0) {
			return string.Empty;
		}

		if (zero < 0) {
			zero = codepoints.Length;
		}

		return encoding.GetString(MemoryMarshal.AsBytes(codepoints[..zero]));
	}

	public string? GetStringForLanguage(ACTTextRef textRef, int languageIndex) => languageIndex < Languages.Count ? ReadStringAt<ushort>(Encoding.BigEndianUnicode, textRef.Text[languageIndex]) : null;

	public string? GetStringForLanguage(int textIndex, int languageIndex) {
		if (textIndex >= Texts.Count) {
			return null;
		}

		var text = Texts[textIndex];
		return GetStringForLanguage(text, languageIndex);
	}

	public string? GetStringForLanguage(HashId id, string language) {
		if (!Languages.TryGetValue(language, out var languageIndex)) {
			return null;
		}

		return Hashes.TryGetValue(id, out var hashInfo) ? GetStringForLanguage(hashInfo.Index, languageIndex) : null;
	}
}
