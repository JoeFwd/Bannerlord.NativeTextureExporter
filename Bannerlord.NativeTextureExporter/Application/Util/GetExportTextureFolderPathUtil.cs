using System.Text.RegularExpressions;
using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Application.Util;

public class GetExportTextureFolderPathUtil
{
    private const string AssetSourcesFolderName = "AssetSources";
    private const string TextureExportFolder = "vanilla_texture_reimports";

    public string GetExportTextureFolderPath(Texture texture, string modAssetsFolder)
    {
        var modAssetSourceFolderPath = GetAssetSourcesFolderPath(modAssetsFolder);

        var match = Regex.Match(
            texture.Source,
            $@"{AssetSourcesFolderName}[\\/](.+)");
        if (match.Success)
        {
            var relativeNativeTextureDir = Path.GetDirectoryName(match.Groups[1].Value) ?? string.Empty;

            return GetTextureExportFolderPath(modAssetSourceFolderPath, relativeNativeTextureDir);
        }

        var textureSourceDir = Path.GetDirectoryName(texture.Source);

        return textureSourceDir is null
            ? modAssetSourceFolderPath
            : Path.GetRelativePath(modAssetSourceFolderPath, textureSourceDir);
    }

    private string GetAssetSourcesFolderPath(string exportBasePath)
    {
        return Path.Combine(exportBasePath, "..", AssetSourcesFolderName);
    }

    private string GetTextureExportFolderPath(string assetSourcesRoot, string relativeDir)
    {
        return Path.Combine(assetSourcesRoot, TextureExportFolder, relativeDir);
    }
}