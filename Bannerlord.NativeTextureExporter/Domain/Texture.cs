namespace Bannerlord.NativeTextureExporter.Domain
{
    /// <summary>
    /// Domain representation of a texture asset. Mirrors the properties used from
    /// <c>TpacTool.Lib.Texture</c> but does not expose any TPAC types.
    /// </summary>
    public sealed class Texture : AssetItem
    {
        /// <summary>
        /// Source path of the texture file as reported by TPAC.
        /// </summary>
        public string Source { get; }

        public Texture(Guid guid, string name, string source)
            : base(guid, name)
        {
            Source = source;
        }
    }
}