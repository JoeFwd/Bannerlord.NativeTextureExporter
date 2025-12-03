using Bannerlord.NativeTextureExporter.Application.Dto;
using Bannerlord.NativeTextureExporter.Application.Port;

namespace Bannerlord.NativeTextureExporter.Infrastrucuture.Spi;

public class ArgumentValidator : IArgumentValidator
{
    public bool ValidateArguments(ExportTextureRequest exportTextureRequest)
    {
        var nativeFolder = new DirectoryInfo(exportTextureRequest.NativeAssetFolderPath);
        var modFolder = new DirectoryInfo(exportTextureRequest.ModAssetFolderPath);

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
}