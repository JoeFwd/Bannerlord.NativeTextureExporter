using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Tests.Infrastructure;

/// <summary>
///     Simple mock implementation of <see cref="ITpacToolWrapper" /> for unit testing.
///     Provides no‑op behavior and empty collections, sufficient for tests that do not depend on TPAC data.
/// </summary>
public class MockTpacToolWrapper : ITpacToolWrapper
{
    public void Load(string folderPath)
    {
    }

    public Dictionary<string, Material> GetLoadedMaterials()
    {
        return new Dictionary<string, Material>();
    }

    public string ExportTexture(Texture texture, string targetDir)
    {
        return "";
    }
}