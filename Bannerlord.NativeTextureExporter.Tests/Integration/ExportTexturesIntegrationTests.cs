using Bannerlord.NativeTextureExporter.Application;
using Bannerlord.NativeTextureExporter.Application.Dto;
using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Application.Util;
using Bannerlord.NativeTextureExporter.Domain;
using Bannerlord.NativeTextureExporter.Infrastrucuture.Spi;

namespace Bannerlord.NativeTextureExporter.Tests.Integration;

/// <summary>
///     Integration tests that exercise the full <see cref="ExportTexturesUseCase" /> pipeline
///     against real DADG tpac asset files (no proprietary Taleworlds assets are used).
///
///     Fixture layout (shipped with the test assembly):
///     <code>
///     Integration/TestFixtures/
///       NativeAssets/
///         english_banners_mtl.tpac   – DADG material, simulates a "native" material
///         banner1_d_tex.tpac         – DADG texture stubs referenced by the material
///         banner1_n_tex.tpac           (no embedded pixel data – see export assertion notes)
///         banner1_s_tex.tpac
///       ModAssets/
///         flags_small_mtl.tpac       – a different DADG material (simulates a "mod" material)
///     </code>
///
///     <b>Why DADG stubs instead of native textures?</b>
///     Native Taleworlds texture packages contain proprietary pixel data that cannot be pushed
///     to an open-source repository.  DADG's own tpac stubs are ~480 bytes of metadata only
///     (no embedded pixel data), so TpacTool can load them for GUID/name lookups but will
///     fail the actual pixel-write step with "no pixel data".  The filesystem assertions
///     therefore target <em>directory creation</em> (which happens before the pixel-write
///     attempt) rather than actual image files.
/// </summary>
[TestFixture]
public class ExportTexturesIntegrationTests
{
    // Resolved once per test run against the compiled output directory.
    private static readonly string FixtureRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "Integration", "TestFixtures");

    private static readonly string FixtureNativeAssets = Path.Combine(FixtureRoot, "NativeAssets");
    private static readonly string FixtureModAssets    = Path.Combine(FixtureRoot, "ModAssets");

    /// <summary>
    ///     Absolute path to the FakeDellarteDellaGuerra module shipped with the Bannerlord
    ///     installation.  Only present on developer machines; tests that require it are guarded
    ///     by <c>Assume.That(Directory.Exists(FakeDadgModuleDir))</c>.
    /// </summary>
    private const string FakeDadgModuleDir =
        @"D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord-v1.3\Modules\FakeDellarteDellaGuerra";

    private const string FakeDadg2ModuleDir =
        @"D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord-v1.3\Modules\FakeDellarteDellaGuerra2";

    private string _tempRoot = null!;

    [SetUp]
    public void SetUp()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            $"BNTE_Integration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    ///     Constructs the use case with all real (non-mock) collaborators, mirroring
    ///     the wiring in <c>Program.cs</c> but without a DI container.
    /// </summary>
    private static IExportTexturesUseCase BuildUseCase() =>
        new ExportTexturesUseCase(
            new TpacToolWrapper(),
            new SceneTextureExtractorUtil(new FileSystemSceneRepository()),
            new GetExportTextureFolderPathUtil(),
            new ArgumentValidator());

    /// <summary>
    ///     Redirects <see cref="Console.Out" /> while <paramref name="action" /> runs,
    ///     then restores the original writer and returns everything that was printed.
    /// </summary>
    private static string CaptureConsole(Action action)
    {
        var capture  = new StringWriter();
        var original = Console.Out;
        Console.SetOut(capture);
        try   { action(); }
        finally { Console.SetOut(original); }
        return capture.ToString();
    }

    private static string NativeDir(string root) => Path.Combine(root, "native");
    private static string ModDir(string root)    => Path.Combine(root, "mod");
    private static string SceneDir(string root)  => Path.Combine(root, "SceneObj");

    private static void CopyFixtures(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir, "*.tpac"))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));
    }

    // ─── test 1: asset folder analysis ────────────────────────────────────────

    /// <summary>
    ///     When the mod assets folder contains a custom material, the use case must:
    ///     <list type="bullet">
    ///       <item>load both native and mod assets via TpacTool (real files, no mocks)</item>
    ///       <item>identify the mod material as custom (log it)</item>
    ///       <item>not export — and therefore not create — the reimports directory, because
    ///             the mod material's textures are not present in the native fixture</item>
    ///       <item>run to completion without throwing</item>
    ///     </list>
    ///
    ///     Layout used inside the temp dir:
    ///     <code>
    ///     native/  ←  english_banners_mtl + banner1_* texture stubs (simulated native package)
    ///     mod/     ←  flags_small_mtl  (custom mod material, different GUID)
    ///     </code>
    ///
    ///     <b>Why no reimports directory?</b>
    ///     <c>flags_small_mtl</c> references the native utility texture <c>white</c>, whose
    ///     tpac is not present in our fixture native folder.  TpacTool therefore cannot resolve
    ///     it into a domain <see cref="Bannerlord.NativeTextureExporter.Domain.Texture"/> object,
    ///     so no GUID overlap exists between the mod material's textures and the native texture
    ///     set.  The export step is never reached and the output directory must not be created —
    ///     proving the native-texture filter works correctly.
    /// </summary>
    [Test]
    public void AssetFolderAnalysis_WithCustomMaterialReferencingNativeTextures_IdentifiesModMaterialAndCompletes()
    {
        // Arrange
        var nativeDir = NativeDir(_tempRoot);
        var modDir    = ModDir(_tempRoot);

        CopyFixtures(FixtureNativeAssets, nativeDir);
        CopyFixtures(FixtureModAssets,    modDir);

        // Pre-condition: fixtures must be loadable by TpacTool
        var preflight = new TpacToolWrapper();
        preflight.Load(nativeDir);
        var preflightMaterials = preflight.GetLoadedMaterials();
        Assume.That(preflightMaterials, Is.Not.Empty,
            "NativeAssets fixture must contain at least one material – check that " +
            "english_banners_mtl.tpac was copied correctly.");

        var useCase = BuildUseCase();
        var request = new ExportTextureRequest(nativeDir, modDir, ScanScene: false);

        // Act
        var output = CaptureConsole(() => useCase.ExportTextures(request));
        TestContext.Out.WriteLine("=== captured output ===");
        TestContext.Out.WriteLine(output);

        // Assert – the custom material in the mod folder was recognised and logged
        Assert.That(output, Does.Contain("Found material"),
            "At least one custom (mod) material should have been identified. " +
            "Output:\n" + output);

        // Assert – workflow reached completion
        Assert.That(output, Does.Contain("All textures have been exported!"),
            "Use case must reach its final log statement.");

        // Assert (filesystem) – the reimports directory must NOT have been created.
        // flags_small's only texture reference (native 'white') is absent from the fixture
        // native folder, so the native-texture filter correctly produces an empty export set
        // and ExportTexture is never called.  Any file under vanilla_texture_reimports would
        // indicate a spurious export.
        var reimportsDir = Path.GetFullPath(
            Path.Combine(modDir, "..", "AssetSources", "vanilla_texture_reimports"));
        Assert.That(Directory.Exists(reimportsDir), Is.False,
            $"No textures should have been exported, so the reimports directory must not " +
            $"exist. Unexpected path: {reimportsDir}");
    }

    // ─── test 2: empty mod assets + scene folder ──────────────────────────────

    /// <summary>
    ///     When the mod assets folder is empty but scene files reference native materials,
    ///     the use case (with <c>--scene</c> / <c>ScanScene = true</c>) must:
    ///     <list type="bullet">
    ///       <item>scan the xscene files found in the sibling SceneObj folder</item>
    ///       <item>recognise the native material referenced by the scene</item>
    ///       <item>call <see cref="ITpacToolWrapper.ExportTexture"/> for each texture,
    ///             which creates the destination directory tree even when the DADG stubs
    ///             contain no embedded pixel data</item>
    ///       <item>run to completion without throwing</item>
    ///     </list>
    ///
    ///     Layout used inside the temp dir:
    ///     <code>
    ///     native/   ←  english_banners_mtl + banner1_* texture stubs (all treated as native)
    ///     mod/      ←  empty
    ///     SceneObj/
    ///       test_scene.xscene  ←  minimal scene referencing the native material
    ///     AssetSources/
    ///       vanilla_texture_reimports/
    ///         banners/           ←  created by ExportTexture before the pixel-write attempt
    ///     </code>
    ///
    ///     The material name is discovered dynamically from the loaded native assets so the
    ///     test remains correct even if the fixture file is replaced.
    /// </summary>
    [Test]
    public void SceneFolderAnalysis_WithEmptyModAssetsAndSceneReferencingNativeMaterial_ScansSceneAndCompletes()
    {
        // Arrange
        var nativeDir = NativeDir(_tempRoot);
        var modDir    = ModDir(_tempRoot);
        var sceneDir  = SceneDir(_tempRoot);   // FileSystemSceneRepository resolves <modDir>/../SceneObj

        CopyFixtures(FixtureNativeAssets, nativeDir);
        Directory.CreateDirectory(modDir);    // intentionally empty
        Directory.CreateDirectory(sceneDir);

        // Discover the native material name from the fixture so the xscene reference is correct.
        var discovery = new TpacToolWrapper();
        discovery.Load(nativeDir);
        var nativeMaterials = discovery.GetLoadedMaterials();
        Assume.That(nativeMaterials, Is.Not.Empty,
            "NativeAssets fixture must contain at least one material.");

        var nativeMaterialName = nativeMaterials.Values.First().Name;

        // Write a minimal well-formed xscene that references the discovered native material.
        var xsceneContent =
            $"""
             <?xml version="1.0"?>
             <scene name="test_scene">
               <entities>
                 <game_entity name="test_entity">
                   <components>
                     <meta_mesh_component>
                       <mesh name="test_mesh" material="{nativeMaterialName}" />
                     </meta_mesh_component>
                   </components>
                 </game_entity>
               </entities>
             </scene>
             """;
        File.WriteAllText(Path.Combine(sceneDir, "test_scene.xscene"), xsceneContent);

        var useCase = BuildUseCase();
        var request = new ExportTextureRequest(nativeDir, modDir, ScanScene: true);

        // Act
        var output = CaptureConsole(() => useCase.ExportTextures(request));
        TestContext.Out.WriteLine("=== captured output ===");
        TestContext.Out.WriteLine(output);

        // Assert – the scene file was picked up and logged
        Assert.That(output, Does.Contain("test_scene.xscene"),
            "The xscene file must have been processed by the scene extractor.");

        // Assert – the native material referenced in the scene was found
        Assert.That(output, Does.Contain(nativeMaterialName),
            $"Native material '{nativeMaterialName}' referenced in the scene must be logged. " +
            $"Output:\n{output}");

        // Assert – workflow reached completion
        Assert.That(output, Does.Contain("All textures have been exported!"),
            "Use case must reach its final log statement.");

        // Assert (filesystem) – ExportTexture was invoked and created the destination
        // directory tree inside the mod's AssetSources folder.
        //
        // TpacToolWrapper.ExportTexture always calls Directory.CreateDirectory(targetDir)
        // before attempting the pixel write.  The DADG texture stubs have no embedded pixel
        // data, so the write itself fails ("no pixel data"), but the directory is guaranteed
        // to exist if the use case correctly reached the export step.
        //
        // The expected sub-path comes from GetExportTextureFolderPathUtil: the stub source
        // field contains "AssetSources/banners/banner1_d.dds", so the relative texture
        // directory is "banners" and the output folder is:
        //   <modDir>/../AssetSources/vanilla_texture_reimports/banners/
        var nativeMaterial      = nativeMaterials.Values.First();
        var firstTexture        = nativeMaterial.Textures.Values.First();
        var assetSourcesDir     = Path.GetFullPath(Path.Combine(modDir, "..", "AssetSources"));
        var reimportsDir        = Path.Combine(assetSourcesDir, "vanilla_texture_reimports");

        Assert.That(Directory.Exists(reimportsDir), Is.True,
            $"vanilla_texture_reimports directory must have been created under " +
            $"{assetSourcesDir} when ExportTexture was called for the scene-referenced " +
            $"material '{nativeMaterialName}'. Output:\n{output}");

        // Verify the texture-specific subdirectory was also created, proving the export
        // path was computed correctly for the individual texture.
        var textureExportDir = new GetExportTextureFolderPathUtil()
            .GetExportTextureFolderPath(firstTexture, modDir);
        Assert.That(Directory.Exists(textureExportDir), Is.True,
            $"Per-texture export directory '{textureExportDir}' must exist after " +
            $"ExportTexture was called for texture '{firstTexture.Name}'.");
    }

    // ─── test 3: full pipeline with packed native tpac ────────────────────────

    /// <summary>
    ///     End-to-end integration test that exercises the complete
    ///     <see cref="ExportTexturesUseCase" /> pipeline with two developer-only fixture
    ///     modules that contain actual pixel data.
    ///
    ///     <b>Module layout (developer machine only)</b>
    ///     <code>
    ///     FakeDellarteDellaGuerra/
    ///       AssetPackages/
    ///         banner1_d_packed.tpac  – fully-packed tpac containing one texture "banner1_d"
    ///                                  with embedded pixel data (HasPixelData = true).
    ///                                  Simulates a native module's AssetPackages folder.
    ///
    ///     FakeDellarteDellaGuerra2/
    ///       Assets/banners/
    ///         banner1_mtl.tpac    – mod material "banner1" referencing banner1_d (native)
    ///                               plus banner1_n and banner1_s (custom mod textures).
    ///         banner1_n_tex.tpac  – mod-owned texture stub
    ///         banner1_s_tex.tpac  – mod-owned texture stub
    ///     </code>
    ///
    ///     <b>What the test proves</b>
    ///     <list type="bullet">
    ///       <item>
    ///         <see cref="ITpacToolWrapper.GetLoadedTextures" /> surfaces packed standalone
    ///         textures (no native material required) so that
    ///         <see cref="ExportTexturesUseCase" /> can identify them as native.
    ///       </item>
    ///       <item>
    ///         The mod material <c>banner1</c> is correctly identified as custom (its GUID is
    ///         absent from native materials).
    ///       </item>
    ///       <item>
    ///         <c>banner1_d</c> – the texture shared between the mod material and the native
    ///         packed tpac – is detected as a native texture reference and exported.
    ///       </item>
    ///       <item>
    ///         An actual <c>banner1_d.dds</c> file is written to
    ///         <c>mod/../AssetSources/vanilla_texture_reimports/banners/</c>, proving that
    ///         the pixel data embedded in the packed tpac was successfully written to disk.
    ///       </item>
    ///     </list>
    ///
    ///     Guarded by <c>Assume.That</c>: skipped automatically on machines where the
    ///     fake modules are not installed (CI, machines without the Bannerlord install).
    /// </summary>
    [Test]
    public void FullPipeline_WithPackedNativeTexture_ExportsActualDdsToModAssetSources()
    {
        Assume.That(Directory.Exists(FakeDadgModuleDir), Is.True,
            $"FakeDellarteDellaGuerra not found at {FakeDadgModuleDir} – test skipped.");
        Assume.That(Directory.Exists(FakeDadg2ModuleDir), Is.True,
            $"FakeDellarteDellaGuerra2 not found at {FakeDadg2ModuleDir} – test skipped.");

        // ── Arrange ──────────────────────────────────────────────────────────────
        //
        // Temp layout:
        //   _tempRoot/
        //     native/          ← TpacToolWrapper.Load(nativeDir)
        //       banner1_d_packed.tpac  (from FakeDellarteDellaGuerra/AssetPackages/ – has pixel data)
        //     mod/             ← TpacToolWrapper.Load(modDir)
        //       banner1_mtl.tpac, banner1_n_tex.tpac, banner1_s_tex.tpac
        //     AssetSources/vanilla_texture_reimports/banners/
        //       banner1_d.dds  ← expected output
        //
        var nativeDir = NativeDir(_tempRoot);
        var modDir    = ModDir(_tempRoot);

        CopyFilesFlat(Path.Combine(FakeDadgModuleDir,  "AssetPackages"),    nativeDir, "*.tpac");
        CopyFilesFlat(Path.Combine(FakeDadg2ModuleDir, "Assets", "banners"), modDir,   "*.tpac");

        // Pre-condition: packed tpac must expose pixel data
        var preflight = new TpacToolWrapper();
        preflight.Load(nativeDir);
        var preflightTextures = preflight.GetLoadedTextures();
        Assume.That(preflightTextures.Values.Any(t => t.Name == "banner1_d"), Is.True,
            "banner1_d_packed.tpac must expose a 'banner1_d' texture – verify FakeDellarteDellaGuerra/AssetPackages.");

        var useCase = BuildUseCase();
        var request = new ExportTextureRequest(nativeDir, modDir, ScanScene: false);

        // ── Act ───────────────────────────────────────────────────────────────────
        var output = CaptureConsole(() => useCase.ExportTextures(request));
        TestContext.Out.WriteLine("=== captured output ===");
        TestContext.Out.WriteLine(output);

        // ── Assert: console log ───────────────────────────────────────────────────
        Assert.That(output, Does.Contain("Found material"),
            "The mod material 'banner1' must be logged as a custom (non-native) material.");
        Assert.That(output, Does.Contain("All textures have been exported!"),
            "Use case must reach completion.");

        // ── Assert: actual DDS file written ──────────────────────────────────────
        //
        // GetExportTextureFolderPathUtil parses texture.Source for "AssetSources/<rel>/".
        // pack0.tpac embeds Source = "$BASE/.../AssetSources/banners/banner1_d.dds",
        // so the export folder is  <modDir>/../AssetSources/vanilla_texture_reimports/banners/.
        var reimportsRoot = Path.GetFullPath(
            Path.Combine(modDir, "..", "AssetSources", "vanilla_texture_reimports"));
        var bannersExportDir = Path.Combine(reimportsRoot, "banners");

        Assert.That(Directory.Exists(bannersExportDir), Is.True,
            $"The per-texture export directory '{bannersExportDir}' must have been created. " +
            $"Captured output:\n{output}");

        var exportedFile = Path.Combine(bannersExportDir, "banner1_d.dds");
        Assert.That(File.Exists(exportedFile), Is.True,
            $"'banner1_d.dds' must exist in '{bannersExportDir}' after the export. " +
            $"If the file is absent the GUID filter did not produce a match or " +
            $"banner1_d_packed.tpac pixel data was not written. Captured output:\n{output}");
    }

    // ─── helpers (local) ──────────────────────────────────────────────────────

    /// <summary>
    ///     Copies all files matching <paramref name="pattern" /> from
    ///     <paramref name="sourceDir" /> directly into <paramref name="targetDir" />,
    ///     creating the target directory if it does not exist.
    /// </summary>
    private static void CopyFilesFlat(string sourceDir, string targetDir, string pattern = "*")
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir, pattern))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);
    }
}
