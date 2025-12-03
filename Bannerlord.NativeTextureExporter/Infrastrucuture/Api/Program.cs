using Bannerlord.NativeTextureExporter.Application;
using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Application.Util;
using Bannerlord.NativeTextureExporter.Infrastrucuture.Spi;
using Microsoft.Extensions.DependencyInjection;

namespace Bannerlord.NativeTextureExporter.Infrastrucuture.Api;

public static class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddSingleton<ITpacToolWrapper, TpacToolWrapper>();
        services.AddSingleton<ISceneRepository, FileSystemSceneRepository>();
        services.AddSingleton<IExportTexturesUseCase, ExportTexturesUseCase>();
        services.AddSingleton<IArgumentValidator, ArgumentValidator>();
        services.AddSingleton<GetExportTextureFolderPathUtil>();
        services.AddSingleton<SceneTextureExtractorUtil>();
        services.AddSingleton<ExportTextureRequestMapper>();

        var provider = services.BuildServiceProvider();

        var useCase = provider.GetRequiredService<IExportTexturesUseCase>();
        var mapper = provider.GetRequiredService<ExportTextureRequestMapper>();
        useCase.ExportTextures(mapper.Map(args));
    }
}