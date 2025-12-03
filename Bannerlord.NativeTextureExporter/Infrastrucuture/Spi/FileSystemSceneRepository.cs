using System.Collections.Immutable;
using Bannerlord.NativeTextureExporter.Application.Port;

namespace Bannerlord.NativeTextureExporter.Infrastrucuture.Spi;

/// <summary>
///     Concrete implementation of <see cref="ISceneRepository" /> that works with the real file system.
///     It looks for the sibling <c>SceneObj</c> folder of the provided mod folder,
///     returns all <c>.xscene</c> files while ignoring any path that contains “backup”
///     (case‑insensitive).
/// </summary>
public sealed class FileSystemSceneRepository : ISceneRepository
{
    /// <inheritdoc />
    public ISet<string> GetSceneFiles(string modFolder)
    {
        var sceneFolder = Path.Combine(modFolder, "..", "SceneObj");

        if (!Directory.Exists(sceneFolder))
            return ImmutableHashSet<string>.Empty;

        var allSceneFiles = Directory.GetFiles(sceneFolder, "*.xscene", SearchOption.AllDirectories);

        HashSet<string> filtered = allSceneFiles
            .Where(f =>
            {
                var dir = Path.GetDirectoryName(f)!;
                return !dir.Contains("backup", StringComparison.OrdinalIgnoreCase) &&
                       !dir.Contains("main_map", StringComparison.OrdinalIgnoreCase) &&
                       !dir.Contains("modded_map", StringComparison.OrdinalIgnoreCase);
            })
            .ToHashSet();

        return filtered;
    }
}