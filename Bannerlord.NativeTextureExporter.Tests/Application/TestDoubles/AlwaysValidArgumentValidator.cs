using Bannerlord.NativeTextureExporter.Application.Dto;
using Bannerlord.NativeTextureExporter.Application.Port;

namespace Bannerlord.NativeTextureExporter.Tests.Application;

internal sealed class AlwaysValidArgumentValidator : IArgumentValidator
{
    public bool ValidateArguments(ExportTextureRequest exportTextureRequest) => true;
}
