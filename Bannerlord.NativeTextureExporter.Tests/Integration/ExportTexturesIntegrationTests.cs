using Bannerlord.NativeTextureExporter.Application;
using Bannerlord.NativeTextureExporter.Application.Dto;
using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Application.Util;
using Bannerlord.NativeTextureExporter.Infrastrucuture.Spi;

namespace Bannerlord.NativeTextureExporter.Tests.Integration;

/// <summary>
///     Smoke tests for boundaries that unit tests cannot cover: real TPAC loading,
///     real scene-file discovery, and actual texture export when pixel data exists.
/// </summary>
[TestFixture]
public class ExportTexturesIntegrationTests
{
    private static readonly string FixtureRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "Integration", "TestFixtures");

    private static readonly string FixtureNativeAssets = Path.Combine(FixtureRoot, "NativeAssets");
    private static readonly string FixtureModAssets = Path.Combine(FixtureRoot, "ModAssets");

    private const string FakeDadgModuleDir =
        @"D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord-v1.3\Modules\FakeDellarteDellaGuerra";

    private const string FakeDadg2ModuleDir =
        @"D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord-v1.3\Modules\FakeDellarteDellaGuerra2";

    private string _tempRoot = null!;

    [SetUp]
    public void SetUp()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"BNTE_Integration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    [Test]
    public void AssetFolderAnalysis_WithRealTpacFixtures_LoadsMaterialsAndCompletes()
    {
        var nativeDir = NativeDir(_tempRoot);
        var modDir = ModDir(_tempRoot);

        CopyFilesFlat(FixtureNativeAssets, nativeDir, "*.tpac");
        CopyFilesFlat(FixtureModAssets, modDir, "*.tpac");

        var nativePreflight = new TpacToolWrapper();
        nativePreflight.Load(nativeDir);
        Assume.That(nativePreflight.GetLoadedMaterials(), Is.Not.Empty,
            "Native fixture TPAC files must be loadable.");

        var output = CaptureConsole(() =>
            BuildUseCase().ExportTextures(new ExportTextureRequest(nativeDir, modDir)));

        Assert.That(output, Does.Contain("Found material"),
            "The real mod fixture should expose at least one non-native material.");
        Assert.That(output, Does.Contain("All textures have been exported!"));
    }

    [Test]
    public void SceneFolderAnalysis_WithRealSceneRepository_FindsSceneMaterialAndCreatesExportDirectory()
    {
        var nativeDir = NativeDir(_tempRoot);
        var modDir = ModDir(_tempRoot);
        var sceneDir = SceneDir(_tempRoot);

        CopyFilesFlat(FixtureNativeAssets, nativeDir, "*.tpac");
        Directory.CreateDirectory(modDir);
        Directory.CreateDirectory(sceneDir);

        var discovery = new TpacToolWrapper();
        discovery.Load(nativeDir);
        var nativeMaterial = discovery.GetLoadedMaterials().Values.FirstOrDefault();
        Assume.That(nativeMaterial, Is.Not.Null, "Native fixture TPAC files must expose a material.");

        File.WriteAllText(Path.Combine(sceneDir, "test_scene.xscene"),
            $"""
             <?xml version="1.0"?>
             <scene>
               <mesh material="{nativeMaterial!.Name}" />
             </scene>
             """);

        var output = CaptureConsole(() =>
            BuildUseCase().ExportTextures(new ExportTextureRequest(nativeDir, modDir, ScanScene: true)));

        Assert.That(output, Does.Contain("test_scene.xscene"));
        Assert.That(output, Does.Contain(nativeMaterial.Name));

        var reimportsDir = Path.GetFullPath(
            Path.Combine(modDir, "..", "AssetSources", "vanilla_texture_reimports"));
        Assert.That(Directory.Exists(reimportsDir), Is.True,
            "Scene-driven export should reach the real TpacToolWrapper export path.");
    }

    [Test]
    public void FullPipeline_WithPackedNativeTexture_ExportsActualDdsToModAssetSources()
    {
        Assume.That(Directory.Exists(FakeDadgModuleDir), Is.True,
            $"FakeDellarteDellaGuerra not found at {FakeDadgModuleDir} - test skipped.");
        Assume.That(Directory.Exists(FakeDadg2ModuleDir), Is.True,
            $"FakeDellarteDellaGuerra2 not found at {FakeDadg2ModuleDir} - test skipped.");

        var nativeDir = NativeDir(_tempRoot);
        var modDir = ModDir(_tempRoot);

        CopyFilesFlat(Path.Combine(FakeDadgModuleDir, "AssetPackages"), nativeDir, "*.tpac");
        CopyFilesFlat(Path.Combine(FakeDadg2ModuleDir, "Assets", "banners"), modDir, "*.tpac");

        var preflight = new TpacToolWrapper();
        preflight.Load(nativeDir);
        Assume.That(preflight.GetLoadedTextures().Values.Any(t => t.Name == "banner1_d"), Is.True,
            "Developer fixture must expose packed texture 'banner1_d'.");

        var output = CaptureConsole(() =>
            BuildUseCase().ExportTextures(new ExportTextureRequest(nativeDir, modDir)));

        Assert.That(output, Does.Contain("All textures have been exported!"));

        var exportedFile = Path.GetFullPath(Path.Combine(
            modDir,
            "..",
            "AssetSources",
            "vanilla_texture_reimports",
            "banners",
            "banner1_d.dds"));
        Assert.That(File.Exists(exportedFile), Is.True);
    }

    private static IExportTexturesUseCase BuildUseCase() =>
        new ExportTexturesUseCase(
            new TpacToolWrapper(),
            new SceneTextureExtractorUtil(new FileSystemSceneRepository()),
            new GetExportTextureFolderPathUtil(),
            new NativeTextureOverrideFilterUtil(),
            new ArgumentValidator());

    private static string CaptureConsole(Action action)
    {
        var capture = new StringWriter();
        var original = Console.Out;
        Console.SetOut(capture);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return capture.ToString();
    }

    private static string NativeDir(string root) => Path.Combine(root, "native");
    private static string ModDir(string root) => Path.Combine(root, "mod");
    private static string SceneDir(string root) => Path.Combine(root, "SceneObj");

    private static void CopyFilesFlat(string sourceDir, string targetDir, string pattern)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir, pattern))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);
    }
}
