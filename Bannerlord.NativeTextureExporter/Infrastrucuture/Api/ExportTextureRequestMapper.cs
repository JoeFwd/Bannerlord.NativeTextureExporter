using Bannerlord.NativeTextureExporter.Application.Dto;

namespace Bannerlord.NativeTextureExporter.Infrastrucuture.Api;

public class ExportTextureRequestMapper
{
    public ExportTextureRequest Map(string[] args)
    {
        var enableScene = args.Any(a => a == "-s" || a == "--scene");
        var nativePath = args.Length > 0 ? args[0] : string.Empty;
        var modPath = args.Length > 1 ? args[1] : string.Empty;

        return new ExportTextureRequest(nativePath, modPath, enableScene);
    }
}