namespace Bannerlord.NativeTextureExporter.Domain
{
    /// <summary>
    /// Base class for domain asset items.
    /// </summary>
    public abstract class AssetItem
    {
        public Guid Guid { get; init; }
        public string Name { get; init; } = string.Empty;

        protected AssetItem(Guid guid, string name)
        {
            Guid = guid;
            Name = name;
        }
    }
}