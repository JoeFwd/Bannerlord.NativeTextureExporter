namespace Bannerlord.NativeTextureExporter.Application.Port;

/// <summary>
///     Abstraction over filesystem access for scene files.
///     Allows the extraction logic to be unit‑tested without touching the real disk.
/// </summary>
public interface ISceneRepository
{
    /// <summary>
    ///     Returns absolute paths of all .xscene files that should be processed.
    ///     Implementations should apply the same folder and backup‑exclusion logic as the original code.
    /// </summary>
    ISet<string> GetSceneFiles(string modFolder);
}