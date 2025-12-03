using Bannerlord.NativeTextureExporter.Application.Dto;
using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Application.Util;
using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Application;

public class ExportTexturesUseCase : IExportTexturesUseCase
{
    private readonly IArgumentValidator _argumentValidator;
    private readonly GetExportTextureFolderPathUtil _getExportTextureFolderPathUtil;
    private readonly SceneTextureExtractorUtil _sceneTextureExtractorUtil;
    private readonly ITpacToolWrapper _tpacWrapper;

    public ExportTexturesUseCase(
        ITpacToolWrapper tpacWrapper,
        SceneTextureExtractorUtil sceneTextureExtractorUtil,
        GetExportTextureFolderPathUtil getExportTextureFolderPathUtil, IArgumentValidator argumentValidator)
    {
        _tpacWrapper = tpacWrapper;
        _sceneTextureExtractorUtil = sceneTextureExtractorUtil;
        _getExportTextureFolderPathUtil = getExportTextureFolderPathUtil;
        _argumentValidator = argumentValidator;
    }

    public void ExportTextures(ExportTextureRequest exportTextureRequest)
    {
        if (!_argumentValidator.ValidateArguments(exportTextureRequest)) return;

        var nativeFolder = exportTextureRequest.NativeAssetFolderPath;
        var modFolder = exportTextureRequest.ModAssetFolderPath;

        _tpacWrapper.Load(nativeFolder);
        Dictionary<string, Material> nativeMaterials = _tpacWrapper.GetLoadedMaterials();
        _tpacWrapper.Load(modFolder);

        Console.WriteLine($"Finding native textures used in materials in folder '{modFolder}'");

        Dictionary<string, Material> allMaterialByGuids = _tpacWrapper.GetLoadedMaterials();
        ISet<string> textureGuids = GetNativeTexturesUsedInModMaterials(allMaterialByGuids, nativeMaterials);

        ISet<string> textureGuidsToExport = new HashSet<string>(textureGuids);

        if (exportTextureRequest.ScanScene)
            textureGuidsToExport.UnionWith(
                _sceneTextureExtractorUtil.ExtractNativeTextures(modFolder, nativeMaterials));

        ExportSelectedNativeTextures(GetTexturesByGuids(nativeMaterials), textureGuidsToExport, modFolder);

        Console.WriteLine("All textures have been exported!");
    }

    private static Dictionary<string, Texture> GetTexturesByGuids(
        Dictionary<string, Material> nativeMaterialByGuids)
    {
        return nativeMaterialByGuids
            .Values.SelectMany(material => material.Textures)
            .GroupBy(texture => texture.Key)
            .ToDictionary(kv => kv.Key, kv => kv.First().Value);
    }

    private void ExportSelectedNativeTextures(
        Dictionary<string, Texture> nativeAssetByGuids,
        IEnumerable<string> textureGuids,
        string modFolderPath)
    {
        foreach (var guid in textureGuids)
        {
            var exportTextureFolderPath =
                _getExportTextureFolderPathUtil.GetExportTextureFolderPath(nativeAssetByGuids[guid],
                    modFolderPath);

            if (!nativeAssetByGuids.TryGetValue(guid, out var texture))
                continue;

            if (HasAlreadyBeenExported(exportTextureFolderPath, texture.Name))
            {
                Console.WriteLine($"Skipping: {texture.Name} has already been exported");
                continue;
            }

            _tpacWrapper.ExportTexture(texture, exportTextureFolderPath);
        }
    }

    private static bool HasAlreadyBeenExported(string targetDir, string textureName)
    {
        if (Directory.Exists(targetDir))
        {
            var existingFiles = Directory.GetFiles(targetDir, $"{textureName}.*");
            if (existingFiles.Length > 0)
                return true;
        }

        return false;
    }

    private static ISet<string> GetNativeTexturesUsedInModMaterials(
        Dictionary<string, Material> allAssetsByGuids,
        Dictionary<string, Material> nativeMaterialByGuids)
    {
        List<Material> modMaterials = allAssetsByGuids.Values
            .Where(m => !nativeMaterialByGuids.ContainsKey(m.Guid.ToString())).ToList();

        modMaterials.ForEach(m => Console.WriteLine($"Found material '{m.Name}'"));

        Dictionary<string, Texture> nativeTextures = nativeMaterialByGuids.Values
            .SelectMany(material => material.Textures.Values)
            .GroupBy(texture => texture.Guid.ToString())
            .ToDictionary(tex => tex.Key, tex => tex.First());

        ISet<string> textureGuids = modMaterials
            .SelectMany(m => m.Textures.Values)
            .Select(tex => tex.Guid.ToString())
            .Where(guid => nativeTextures.ContainsKey(guid))
            .ToHashSet();

        return textureGuids;
    }
}