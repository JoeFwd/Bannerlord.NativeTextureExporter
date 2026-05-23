using Bannerlord.NativeTextureExporter.Application.Port;

namespace Bannerlord.NativeTextureExporter.Tests.Application;

internal sealed class EmptySceneRepository : ISceneRepository
{
    public ISet<string> GetSceneFiles(string modFolder) => new HashSet<string>();
}
