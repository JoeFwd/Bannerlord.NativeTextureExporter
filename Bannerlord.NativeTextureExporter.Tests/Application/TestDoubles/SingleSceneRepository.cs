using Bannerlord.NativeTextureExporter.Application.Port;

namespace Bannerlord.NativeTextureExporter.Tests.Application;

internal sealed class SingleSceneRepository : ISceneRepository
{
    private readonly string _sceneFile;

    public SingleSceneRepository(string sceneFile)
    {
        _sceneFile = sceneFile;
    }

    public ISet<string> GetSceneFiles(string modFolder) => new HashSet<string> { _sceneFile };
}
