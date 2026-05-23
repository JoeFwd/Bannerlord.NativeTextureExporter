using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Tests.Application;

/// <summary>
///     Simulates TpacToolWrapper's accumulating AssetManager.
///     Data for the native folder is added on the first Load call;
///     data for the mod folder is accumulated on the second call.
/// </summary>
internal sealed class AccumulatingTpacFake : ITpacToolWrapper
{
    private readonly Dictionary<string, Material> _nativeMaterials;
    private readonly Dictionary<string, Texture> _nativeTextures;
    private readonly Dictionary<string, Material> _modMaterials;
    private readonly Dictionary<string, Texture> _modTextures;

    private readonly Dictionary<string, Material> _accumulated = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture> _accumulatedTex = new(StringComparer.OrdinalIgnoreCase);

    private int _loadCallCount;

    public List<Texture> ExportedTextures { get; } = [];

    public AccumulatingTpacFake(
        Dictionary<string, Material> nativeMaterials,
        Dictionary<string, Texture> nativeTextures,
        Dictionary<string, Material> modMaterials,
        Dictionary<string, Texture> modTextures)
    {
        _nativeMaterials = nativeMaterials;
        _nativeTextures = nativeTextures;
        _modMaterials = modMaterials;
        _modTextures = modTextures;
    }

    public void Load(string folderPath)
    {
        _loadCallCount++;
        var (mats, texs) = _loadCallCount == 1
            ? (_nativeMaterials, _nativeTextures)
            : (_modMaterials, _modTextures);

        foreach (var kv in mats) _accumulated[kv.Key] = kv.Value;
        foreach (var kv in texs) _accumulatedTex[kv.Key] = kv.Value;
    }

    public Dictionary<string, Material> GetLoadedMaterials() =>
        new(_accumulated, StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Texture> GetLoadedTextures() =>
        new(_accumulatedTex, StringComparer.OrdinalIgnoreCase);

    public string ExportTexture(Texture texture, string targetDir)
    {
        ExportedTextures.Add(texture);
        return targetDir;
    }
}
