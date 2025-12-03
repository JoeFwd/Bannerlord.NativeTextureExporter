using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Application.Port;

public interface ITpacToolWrapper
{
    public void Load(string folderPath);

    public Dictionary<string, Material> GetLoadedMaterials();

    public string ExportTexture(Texture texture, string targetDir);
}