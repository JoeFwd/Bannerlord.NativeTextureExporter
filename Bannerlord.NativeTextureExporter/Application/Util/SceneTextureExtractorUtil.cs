using System.Xml.Linq;
using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Application.Util;

/// <summary>
///     Handles extraction of native texture GUIDs from .xscene files.
/// </summary>
public class SceneTextureExtractorUtil
{
    private readonly ISceneRepository _sceneRepo;

    public SceneTextureExtractorUtil(ISceneRepository sceneRepo)
    {
        _sceneRepo = sceneRepo;
    }

    /// <summary>
    ///     Returns distinct texture GUIDs referenced by native materials used in .xscene files.
    /// </summary>
    /// <param name="modFolder">Root folder of the mod (contains AssetSources folder).</param>
    /// <param name="nativeAssetByGuids">Dictionary of native assets keyed by GUID.</param>
    /// <returns>HashSet of unique texture GUID strings.</returns>
    public ISet<string> ExtractNativeTextures(
        string modFolder,
        Dictionary<string, Material> nativeAssetByGuids)
    {
        Dictionary<string, Material> nativeMaterials = nativeAssetByGuids.Values
            .ToDictionary(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);

        HashSet<string> textureGuids = new(StringComparer.OrdinalIgnoreCase);

        ISet<string> sceneFiles = _sceneRepo.GetSceneFiles(modFolder);

        foreach (var sceneFile in sceneFiles)
        {
            Console.WriteLine($"Checking for materials in '{sceneFile}'.");

            try
            {
                XDocument.Load(sceneFile)
                    .Descendants("mesh")
                    .Select(m => m.Attribute("material")?.Value)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList()
                    .ForEach(materialName =>
                    {
                        if (!nativeMaterials.TryGetValue(materialName!, out var material) ||
                            !nativeAssetByGuids.ContainsKey(material.Guid.ToString()))
                            return;

                        Console.WriteLine($"Found material '{materialName}'");

                        IEnumerable<string> guids = material.Textures.Values
                            .Select(tex => tex.Guid.ToString());
                        textureGuids.UnionWith(guids);
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process scene file '{sceneFile}': {ex.Message}");
            }
        }

        return textureGuids;
    }
}