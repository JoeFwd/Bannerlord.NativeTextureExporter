using System.Text.RegularExpressions;
using TpacTool.IO;
using TpacTool.Lib;

namespace Bannerlord.NativeTextureExporter
{
    public static class Program
    {
        private const string AssetSourcesFolderName = "AssetSources";
        private const string TextureExportFolder = "vanilla_texture_reimports";

        public static void Main(string[] args)
        {
            if (!ValidateArguments(args, out var nativeFolder, out var modFolder))
                return;
        
            var assetManager = new AssetManager();
            assetManager.Load(nativeFolder);
        
            Dictionary<string, AssetItem> nativeAssetByGuids = GetAssetByGuids(assetManager);
        
            assetManager.Load(modFolder);
        
            Console.WriteLine($"Finding materials in folder '{modFolder}'");
        
            var textureGuids = GetNativeTexturesUsedInModMaterials(assetManager, nativeAssetByGuids);
        
            ExportSelectedNativeTextures(GetTexturesByGuids(nativeAssetByGuids), textureGuids, modFolder);
            
            Console.WriteLine($"All textures have been exported!");
        }

        private static Dictionary<string, AssetItem> GetAssetByGuids(AssetManager assetManager)
        {
            return assetManager.LoadedAssets
                .GroupBy(t => t.Guid.ToString())
                .ToDictionary(g => g.Key, g => g.First());
        }
        
        private static Dictionary<string, Texture> GetTexturesByGuids(Dictionary<string, AssetItem> nativeAssetByGuids)
        {
            return nativeAssetByGuids
                .Where(kv => kv.Value is Texture)
                .ToDictionary(kv => kv.Key, kv => (Texture)kv.Value);
        }

        private static void ExportSelectedNativeTextures(
            Dictionary<string, Texture> nativeAssetByGuids,
            IEnumerable<string> textureGuids,
            DirectoryInfo targetFolder)
        {
            string assetSourcesFolderPath = GetAssetSourcesFolderPath(targetFolder.FullName);

            foreach (var guid in textureGuids)
            {
                if (!nativeAssetByGuids.TryGetValue(guid, out var texture))
                    continue;

                string relativeNativeTextureDir = GetRelativeNativeTextureFolderPath(texture, assetSourcesFolderPath);
                string textureExportFolderPath = GetTextureExportFolderPath(assetSourcesFolderPath, relativeNativeTextureDir);

                if (HasAlreadyBeenExported(textureExportFolderPath, texture.Name))
                {
                    Console.WriteLine($"Skipping: {texture.Name} has already been exported");
                    continue;
                }
                    

                ExportTexture(texture, textureExportFolderPath);
            }
        }

        private static string GetAssetSourcesFolderPath(string exportBasePath) =>
            Path.Combine(exportBasePath, "..", AssetSourcesFolderName);

        private static string GetRelativeNativeTextureFolderPath(Texture texture, string assetSourcesRoot)
        {
            try
            {
                var match = Regex.Match(
                    texture.Source,
                    $@"{AssetSourcesFolderName}[\\/](.+)");
                if (match.Success)
                {
                    return Path.GetDirectoryName(match.Groups[1].Value) ?? string.Empty;
                }
            }
            catch { }

            var textureSourceDir = Path.GetDirectoryName(texture.Source);

            return textureSourceDir is null
                ? assetSourcesRoot
                : Path.GetRelativePath(assetSourcesRoot, textureSourceDir);
        }

        private static string GetTextureExportFolderPath(string assetSourcesRoot, string relativeDir) =>
            Path.Combine(assetSourcesRoot, TextureExportFolder, relativeDir);

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

        private static void ExportTexture(Texture texture, string targetDir)
        {
            try
            {
                Console.WriteLine($"Exporting texture '{texture.Name}' into {targetDir}");
                if (texture.Source.EndsWith("dds"))
                {
                    TextureExporter.ExportToFolder<DdsExporter>(targetDir, texture);
                }
                else
                {
                    try
                    {
                        TextureExporter.ExportToFolder<PngExporter>(targetDir, texture);
                    }
                    catch
                    {
                        TextureExporter.ExportToFolder<DdsExporter>(targetDir, texture);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to export texture '{texture.Name}': {ex.Message}");
            }
        }
        private static bool ValidateArguments(string[] args, out DirectoryInfo nativeFolder, out DirectoryInfo modFolder)
        {
            nativeFolder = null!;
            modFolder = null!;

            if (args.Length < 2)
            {
                Console.WriteLine("Please provide the paths to the native assets folder and the custom mod assets folder.");
                return false;
            }

            nativeFolder = new DirectoryInfo(args[0]);
            modFolder = new DirectoryInfo(args[1]);

            if (!nativeFolder.Exists)
            {
                Console.WriteLine($"Native assets folder does not exist: {nativeFolder.FullName}");
                return false;
            }

            if (!modFolder.Exists)
            {
                Console.WriteLine($"Mod assets folder does not exist: {modFolder.FullName}");
                return false;
            }

            return true;
        }

        private static List<string> GetNativeTexturesUsedInModMaterials(AssetManager assetManager, Dictionary<string, AssetItem> nativeAssetByGuids)
        {
            return assetManager.LoadedAssets
                .Where(asset => asset is Material && !nativeAssetByGuids.ContainsKey(asset.Guid.ToString()))
                .Select(asset =>
                {
                    Console.WriteLine($"Found material '{asset.Name}'");
                    return asset as Material;
                })
                .SelectMany(material => material.Textures.Values)
                .Select(tex => tex.Guid.ToString())
                .Where(guid => nativeAssetByGuids.ContainsKey(guid))
                .ToList();
        }
    }
}