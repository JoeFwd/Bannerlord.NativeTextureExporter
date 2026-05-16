using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Application.Port;

public interface ITpacToolWrapper
{
    public void Load(string folderPath);

    public Dictionary<string, Material> GetLoadedMaterials();

    /// <summary>
    ///     Returns every <see cref="Texture" /> that was loaded in the most recent
    ///     <see cref="Load" /> call, keyed by GUID string.
    ///     Unlike <see cref="GetLoadedMaterials" />, this includes textures that
    ///     exist as standalone packed assets without an accompanying material
    ///     (e.g. Bannerlord's <c>AssetPackages/</c> tpac files).
    /// </summary>
    public Dictionary<string, Texture> GetLoadedTextures();

    public string ExportTexture(Texture texture, string targetDir);
}