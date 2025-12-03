using Bannerlord.NativeTextureExporter.Application.Dto;

namespace Bannerlord.NativeTextureExporter.Application.Port;

public interface IArgumentValidator
{
    bool ValidateArguments(ExportTextureRequest exportTextureRequest);
}