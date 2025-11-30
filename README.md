# NativeTextureExporter

Exports native Bannerlord textures referenced in custom material files of a mod.

A command‑line utility that extracts native game textures referenced by custom material files in a mod, saving them to a folder for import. After running the tool, use the usual modding workflow to scan for changes and import the exported textures into the mod. Primarily intended as a quick fix for black textures that appear in custom scenes.

## What it does

- Reads native asset files from the game's native assets directory.
- Scans the specified mod folder for material definitions.
- Identifies texture references in those materials that come from the native assets.
- Copies the required native textures (DDS or PNG) into `<ModFolder>/AssetSources/vanilla_texture_reimports`.
- Ignores textures that have already been exported to avoid duplicates.

## Prerequisites

- .NET 8 SDK (or newer) installed – the runtime is required to execute the tool.
- Use the provided `NativeTextureExporter.dll` directly or run the project with `dotnet run`.
- Provide two arguments: the path to the game's native assets folder and the path to the mod's **Assets** folder or an **AssetPackages** directory.

## Dependencies

- The tool relies on the TpacTool binaries (`TpacTool.IO.dll`, `TpacTool.Lib.dll`) which are built from the [TpacTool project](https://github.com/hunharibo/TpacTool/commit/481a8445ae1bb565d87fe4a971cfbed338b2e734). The script uses it as an API.

## Building

```bash
dotnet build -c Release
```

## Running

```bash
dotnet run -- <NativeAssetsPath> <ModAssetsPath>
# Note: <ModAssetsPath> should point to the mod's **Assets** folder or an **AssetPackages** directory.
```

Or, after building:

```bash
dotnet bin/Debug/net8.0/NativeTextureExporter.dll -- <NativeAssetsPath> <ModAssetsPath>
# Note: <ModAssetsPath> should point to the mod's **Assets** folder or an **AssetPackages** directory.
```

### Example

```bash
dotnet run -- "D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\Native\EmAssetPackages" "D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\MyMod\Assets"
```

The tool will output log messages indicating progress and where textures are exported.

## License

[MIT License](LICENSE)