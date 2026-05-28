// SPDX-FileCopyrightText: 2026 Neptuwunium
//
// SPDX-License-Identifier: EUPL-1.2

using Pluto;
using Pluto.Extensions;
using Pluto.IO.Binary;
using Pluto.Maths;
using Triton;
using Triton.Pixel.Formats;
using Ulysses.Struct.FHM;
using Ulysses.Struct.Nu;
using Ulysses.Struct.UI;

namespace Ulysses.Resources;

public class UITexture : Resource {
	public UITexture(FHMFile fhm, FHMItemHeader item, string name, bool leaveOpen = false) : base(fhm, name, leaveOpen) {
		CropHosts = ObjectPool<List<IImageBuffer?>>.Rent();
		CropHosts.Clear();
		Textures = ObjectPool<List<UITXTexture>>.Rent();
		Textures.Clear();
		VariableTextures = ObjectPool<List<UITXVariableTexture>>.Rent();
		VariableTextures.Clear();

		using var data = fhm[item];
		if (data.Length == 0) {
			return;
		}

		using var reader = new ArrayPoolBinaryReader(data);
		var header = reader.Read<UITXHeader>().ReverseEndianness();

		reader.Position = header.VariableOffset;

		if (header.VariableCount > 0) {
			var buf = (stackalloc UITXVariableTexture[header.VariableCount]);
			reader.Read(buf);
			buf.ReverseEndianness();
			VariableTextures.AddRange(buf);
		}

		reader.Position = header.TextureOffset;
		if (header.TextureCount > 0) {
			NuContainerFHM = fhm.GetChildItem(1);
			NuTextureFHM = NuContainerFHM?.GetChildItem(0);

			if (NuTextureFHM == null) {
				return;
			}

			NuTextureData = new NuTexture(NuTextureFHM, NuTextureFHM.GetItemHeader(0), 0, name, true);
			foreach (var surface in NuTextureData.Surfaces) {
				CropHosts.Add(NuTextureData.DecompressSurface(surface));
			}

			var buf = (stackalloc UITXTexture[header.TextureCount]);
			reader.Read(buf);
			buf.ReverseEndianness();
			Textures.AddRange(buf);
		}
	}

	public override int ResourceCount => Textures.Count;
	public FHMFile? NuContainerFHM { get; set; }
	public FHMFile? NuTextureFHM { get; set; }
	public NuTexture? NuTextureData { get; set; }
	public List<IImageBuffer?> CropHosts { get; set; }
	public List<UITXTexture> Textures { get; set; }
	public List<UITXVariableTexture> VariableTextures { get; set; }

	public override string? GetResourceName(int resourceIndex, string baseName) {
		if (resourceIndex < 0 || resourceIndex > ResourceCount) {
			return null;
		}

		return $"{baseName}/{resourceIndex}.{NuTexture.ExportFormat.ToString().ToLower()}";
	}


	public override bool Save(Stream stream, int resourceIndex) {
		if (resourceIndex < 0 || resourceIndex > ResourceCount) {
			return false;
		}

		var texture = Textures[resourceIndex];
		if (texture.SurfaceIndex >= CropHosts.Count) {
			return false;
		}

		var host = CropHosts[texture.SurfaceIndex];
		if (host == null) {
			return false;
		}

		using var image = new ImageBuffer<ColorRGBA<byte>, byte>(texture.Size.X, texture.Size.Y);
		image.Draw(host, Point<int>.Zero, new Rect<int>(texture.TopLeft.X, texture.TopLeft.Y, texture.Size.X, texture.Size.Y));
		NuTexture.SaveImage(stream, image, NuExportFormat.PNG);
		return true;
	}

	protected override void Dispose(bool disposing) {
		foreach (var buffer in CropHosts) {
			buffer?.Dispose();
		}

		NuTextureData?.Dispose();
		NuTextureFHM?.Dispose();
		NuContainerFHM?.Dispose();

		CropHosts.Clear();
		ObjectPool<List<IImageBuffer?>>.Return(CropHosts);
		CropHosts = null!;

		Textures.Clear();
		ObjectPool<List<UITXTexture>>.Return(Textures);
		Textures = null!;

		VariableTextures.Clear();
		ObjectPool<List<UITXVariableTexture>>.Return(VariableTextures);
		VariableTextures = null!;
	}
}
