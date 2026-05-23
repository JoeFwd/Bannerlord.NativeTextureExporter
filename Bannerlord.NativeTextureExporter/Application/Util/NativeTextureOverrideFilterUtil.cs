using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Application.Util;

public class NativeTextureOverrideFilterUtil
{
    /// <summary>
    ///     Keeps only native texture GUIDs whose texture names are not provided by the mod
    ///     with a different GUID.
    /// </summary>
    public ISet<string> FilterOutModOverriddenTextures(
        ISet<string> textureGuids,
        Dictionary<string, Texture> nativeTextures,
        Dictionary<string, Texture> allTextures,
        Dictionary<string, Texture> mergedNativeTextures)
    {
        ISet<string> modTextureNames = allTextures
            .Where(kv => !nativeTextures.ContainsKey(kv.Key))
            .Select(kv => kv.Value.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filtered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var guid in textureGuids)
        {
            if (!mergedNativeTextures.TryGetValue(guid, out var texture))
            {
                filtered.Add(guid);
                continue;
            }

            if (modTextureNames.Contains(texture.Name))
            {
                Console.WriteLine($"Skipping native texture '{texture.Name}': overridden by the mod.");
                continue;
            }

            filtered.Add(guid);
        }

        return filtered;
    }
}
