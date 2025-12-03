using Bannerlord.NativeTextureExporter.Application.Dto;

namespace Bannerlord.NativeTextureExporter.Application;

public interface IExportTexturesUseCase
{
    void ExportTextures(ExportTextureRequest exportTextureRequest);
}