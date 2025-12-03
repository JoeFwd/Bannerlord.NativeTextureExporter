using System;
using System.Collections.Generic;

namespace Bannerlord.NativeTextureExporter.Domain
{
    /// <summary>
    /// Domain representation of a material asset. Mirrors the properties used from
    /// <c>TpacTool.Lib.Material</c> but does not expose any TpacTool types.
    /// </summary>
    public sealed class Material : AssetItem
    {
        /// <summary>
        /// Textures referenced by this material, keyed by their texture name.
        /// </summary>
        public Dictionary<string, Texture> Textures { get; }

        public Material(Guid guid, string name, Dictionary<string, Texture> textures)
            : base(guid, name)
        {
            Textures = textures ?? new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);
        }
    }
}