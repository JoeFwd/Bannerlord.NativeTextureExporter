using Bannerlord.NativeTextureExporter.Application.Port;
using TpacTool.IO;
using TpacTool.Lib;
using Material = Bannerlord.NativeTextureExporter.Domain.Material;
using Texture = Bannerlord.NativeTextureExporter.Domain.Texture;

namespace Bannerlord.NativeTextureExporter.Infrastrucuture.Spi;

/// <summary>
///     Wrapper around the TpacTool API to simplify asset loading and material/texture queries.
/// </summary>
public class TpacToolWrapper : ITpacToolWrapper
{
    private readonly AssetManager _assetManager = new();

    /// <summary>
    ///     Loads assets from the specified folder.
    /// </summary>
    public void Load(string folderPath)
    {
        _assetManager.Load(new DirectoryInfo(folderPath));
    }

    public Dictionary<string, Material> GetLoadedMaterials()
    {
        Dictionary<Guid, Texture> loadedTexturesByGuid = _assetManager.LoadedAssets
            .OfType<TpacTool.Lib.Texture>()
            .Select(texture => MapEntityTextureToDomain(texture))
            .GroupBy(t => t.Guid)
            .ToDictionary(g => g.Key, g => g.First());

        return _assetManager.LoadedAssets
            .OfType<TpacTool.Lib.Material>()
            .Select(material => MapMaterial(loadedTexturesByGuid, material))
            .ToDictionary(kv => kv.Guid.ToString(), kv => kv);
    }

    public string ExportTexture(Texture texture, string targetDir)
    {
        try
        {
            Directory.CreateDirectory(targetDir);

            var tpacTexture = MapTexture(texture);

            if (texture.Source.EndsWith("dds", StringComparison.OrdinalIgnoreCase))
                TextureExporter.ExportToFolder<DdsExporter>(targetDir, tpacTexture);
            else
                try
                {
                    TextureExporter.ExportToFolder<PngExporter>(targetDir, tpacTexture);
                }
                catch
                {
                    TextureExporter.ExportToFolder<DdsExporter>(targetDir, tpacTexture);
                }

            Console.WriteLine($"Exporting texture '{texture.Name}' to {targetDir}");
            return targetDir;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to export texture '{texture.Name}': {ex.Message}");
            return string.Empty;
        }
    }

    private Material MapMaterial(Dictionary<Guid, Texture> texturesByGuid, TpacTool.Lib.Material material)
    {
        Dictionary<string, Texture?> mappedTextures = material.Textures.Values
            .Select(tex =>
                texturesByGuid.TryGetValue(tex.Guid, out var mappedTex)
                    ? mappedTex
                    : null)
            .Where(tex => tex != null)
            .GroupBy(t => t.Guid.ToString())
            .ToDictionary(g => g.Key, g => g.First());

        return new Material(material.Guid, material.Name, mappedTextures);
    }

    private TpacTool.Lib.Texture? MapTexture(Texture domainTexture)
    {
        return _assetManager.LoadedAssets.OfType<TpacTool.Lib.Texture>()
            .FirstOrDefault(texture => texture.Guid.Equals(domainTexture.Guid));
    }

    private static Texture MapEntityTextureToDomain(TpacTool.Lib.Texture tpacTexture)
    {
        return new Texture(tpacTexture.Guid, tpacTexture.Name, tpacTexture.Source);
    }
}