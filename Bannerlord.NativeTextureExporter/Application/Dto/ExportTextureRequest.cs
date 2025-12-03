namespace Bannerlord.NativeTextureExporter.Application.Dto;

public record ExportTextureRequest(
    string NativeAssetFolderPath,
    string ModAssetFolderPath,
    bool ScanScene = false);