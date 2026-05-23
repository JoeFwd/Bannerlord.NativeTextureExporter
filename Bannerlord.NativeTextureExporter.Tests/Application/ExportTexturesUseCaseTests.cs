using Bannerlord.NativeTextureExporter.Application;
using Bannerlord.NativeTextureExporter.Application.Dto;
using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Application.Util;
using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Tests.Application;

/// <summary>
///     Unit tests for <see cref="ExportTexturesUseCase" /> focused on the native-texture
///     override filter: a native texture referenced by a mod material must NOT be exported
///     when the mod also ships a texture with the same name.
/// </summary>
[TestFixture]
public class ExportTexturesUseCaseTests
{
    // ── fake folder path constants ─────────────────────────────────────────────
    private const string NativePath = "fake_native";
    private const string ModPath    = "fake_mod";

    // ── shared GUIDs ──────────────────────────────────────────────────────────
    private static readonly Guid GuidNativeMaterial = Guid.NewGuid();
    private static readonly Guid GuidModMaterial    = Guid.NewGuid();
    private static readonly Guid GuidNativeTexA     = Guid.NewGuid();
    private static readonly Guid GuidModTexB        = Guid.NewGuid();

    // ── helpers ───────────────────────────────────────────────────────────────

    private static ExportTexturesUseCase BuildUseCase(
        AccumulatingTpacFake stub,
        ISceneRepository? sceneRepository = null) =>
        new(
            stub,
            new SceneTextureExtractorUtil(sceneRepository ?? new EmptySceneRepository()),
            new GetExportTextureFolderPathUtil(),
            new NativeTextureOverrideFilterUtil(),
            new AlwaysValidArgumentValidator());

    private static string CaptureConsole(Action action)
    {
        var sw  = new StringWriter();
        var old = Console.Out;
        Console.SetOut(sw);
        try   { action(); }
        finally { Console.SetOut(old); }
        return sw.ToString();
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    ///     When the mod does NOT have a texture with the same name as the native texture
    ///     referenced by a custom material, the native texture must be exported.
    /// </summary>
    [Test]
    public void ExportTextures_WhenNativeTextureIsNotOverridden_ExportsIt()
    {
        // Arrange
        var nativeTexA = new Texture(GuidNativeTexA, "native_tex_a", "AssetSources/textures/native_tex_a.dds");

        // Native material references native_tex_a
        var nativeMaterial = new Material(
            GuidNativeMaterial, "native_mat",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase)
            {
                { GuidNativeTexA.ToString(), nativeTexA }
            });

        // Mod material also references native_tex_a (by the same GUID)
        var modMaterial = new Material(
            GuidModMaterial, "mod_mat",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase)
            {
                { GuidNativeTexA.ToString(), nativeTexA }
            });

        // Mod owns a texture with a DIFFERENT name — not an override
        var modTexB = new Texture(GuidModTexB, "mod_tex_b", "AssetSources/textures/mod_tex_b.dds");

        var stub = new AccumulatingTpacFake(
            nativeMaterials: new Dictionary<string, Material> { { GuidNativeMaterial.ToString(), nativeMaterial } },
            nativeTextures:  new Dictionary<string, Texture>  { { GuidNativeTexA.ToString(), nativeTexA } },
            modMaterials:    new Dictionary<string, Material> { { GuidModMaterial.ToString(), modMaterial } },
            modTextures:     new Dictionary<string, Texture>  { { GuidModTexB.ToString(), modTexB } });

        var useCase = BuildUseCase(stub);
        var request = new ExportTextureRequest(NativePath, ModPath, ScanScene: false);

        // Act
        CaptureConsole(() => useCase.ExportTextures(request));

        // Assert – native_tex_a is not overridden, so it must be exported exactly once
        Assert.That(stub.ExportedTextures, Has.Count.EqualTo(1),
            "Exactly one texture should have been exported.");
        Assert.That(stub.ExportedTextures[0].Name, Is.EqualTo("native_tex_a"),
            "The exported texture must be 'native_tex_a'.");
    }

    /// <summary>
    ///     When the mod provides a texture whose name matches a native texture referenced by
    ///     a custom material, the native texture must be skipped and a console message logged.
    /// </summary>
    [Test]
    public void ExportTextures_WhenNativeTextureIsOverriddenByMod_SkipsIt()
    {
        // Arrange
        var nativeTexA = new Texture(GuidNativeTexA, "native_tex_a", "AssetSources/textures/native_tex_a.dds");

        var nativeMaterial = new Material(
            GuidNativeMaterial, "native_mat",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase)
            {
                { GuidNativeTexA.ToString(), nativeTexA }
            });

        var modMaterial = new Material(
            GuidModMaterial, "mod_mat",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase)
            {
                { GuidNativeTexA.ToString(), nativeTexA }
            });

        // Mod owns a texture with the SAME name → override!
        var modTexSameName = new Texture(GuidModTexB, "native_tex_a", "AssetSources/textures/native_tex_a.dds");

        var stub = new AccumulatingTpacFake(
            nativeMaterials: new Dictionary<string, Material> { { GuidNativeMaterial.ToString(), nativeMaterial } },
            nativeTextures:  new Dictionary<string, Texture>  { { GuidNativeTexA.ToString(), nativeTexA } },
            modMaterials:    new Dictionary<string, Material> { { GuidModMaterial.ToString(), modMaterial } },
            modTextures:     new Dictionary<string, Texture>  { { GuidModTexB.ToString(), modTexSameName } });

        var useCase = BuildUseCase(stub);
        var request = new ExportTextureRequest(NativePath, ModPath, ScanScene: false);

        // Act
        var output = CaptureConsole(() => useCase.ExportTextures(request));

        // Assert – texture is overridden, so ExportTexture must never be called
        Assert.That(stub.ExportedTextures, Is.Empty,
            "No texture should be exported when the mod overrides the native texture.");

        // Assert – the skip must be announced in the console output
        Assert.That(output, Does.Contain("Skipping native texture 'native_tex_a'"),
            "A 'Skipping' message must be written to the console for the overridden texture.");
    }

    /// <summary>
    ///     When the mod overrides some native textures but not others, only the non-overridden
    ///     ones must be exported.
    /// </summary>
    [Test]
    public void ExportTextures_WhenSomeNativeTexturesOverridden_ExportsOnlyNonOverridden()
    {
        // Arrange – two native textures; mod overrides only the second one
        var guidNativeTexC = Guid.NewGuid();
        var guidModOverrideD = Guid.NewGuid();

        var nativeTexA = new Texture(GuidNativeTexA, "native_tex_a", "AssetSources/textures/native_tex_a.dds");
        var nativeTexC = new Texture(guidNativeTexC,  "native_tex_c", "AssetSources/textures/native_tex_c.dds");

        var nativeMaterial = new Material(
            GuidNativeMaterial, "native_mat",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase)
            {
                { GuidNativeTexA.ToString(), nativeTexA },
                { guidNativeTexC.ToString(), nativeTexC }
            });

        // Mod material references both native textures
        var modMaterial = new Material(
            GuidModMaterial, "mod_mat",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase)
            {
                { GuidNativeTexA.ToString(), nativeTexA },
                { guidNativeTexC.ToString(), nativeTexC }
            });

        // Mod overrides native_tex_c only
        var modOverrideC = new Texture(guidModOverrideD, "native_tex_c", "AssetSources/textures/native_tex_c.dds");

        var stub = new AccumulatingTpacFake(
            nativeMaterials: new Dictionary<string, Material> { { GuidNativeMaterial.ToString(), nativeMaterial } },
            nativeTextures: new Dictionary<string, Texture>
            {
                { GuidNativeTexA.ToString(), nativeTexA },
                { guidNativeTexC.ToString(), nativeTexC }
            },
            modMaterials: new Dictionary<string, Material> { { GuidModMaterial.ToString(), modMaterial } },
            modTextures:  new Dictionary<string, Texture>  { { guidModOverrideD.ToString(), modOverrideC } });

        var useCase = BuildUseCase(stub);
        var request = new ExportTextureRequest(NativePath, ModPath, ScanScene: false);

        // Act
        var output = CaptureConsole(() => useCase.ExportTextures(request));

        // Assert – only native_tex_a must be exported (native_tex_c is overridden)
        Assert.That(stub.ExportedTextures, Has.Count.EqualTo(1),
            "Only one texture should be exported.");
        Assert.That(stub.ExportedTextures[0].Name, Is.EqualTo("native_tex_a"),
            "Only 'native_tex_a' should be exported; 'native_tex_c' is overridden by the mod.");

        Assert.That(output, Does.Contain("Skipping native texture 'native_tex_c'"),
            "A 'Skipping' message must appear for 'native_tex_c'.");
    }

    /// <summary>
    ///     Name comparison for overrides is case-insensitive.
    /// </summary>
    [Test]
    public void ExportTextures_WhenModOverridesWithDifferentCaseName_SkipsIt()
    {
        // Arrange
        var nativeTexA = new Texture(GuidNativeTexA, "Native_Tex_A", "AssetSources/textures/Native_Tex_A.dds");

        var nativeMaterial = new Material(
            GuidNativeMaterial, "native_mat",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase)
            {
                { GuidNativeTexA.ToString(), nativeTexA }
            });

        var modMaterial = new Material(
            GuidModMaterial, "mod_mat",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase)
            {
                { GuidNativeTexA.ToString(), nativeTexA }
            });

        // Mod provides the same name but in all-lowercase
        var modTexLower = new Texture(GuidModTexB, "native_tex_a", "AssetSources/textures/native_tex_a.dds");

        var stub = new AccumulatingTpacFake(
            nativeMaterials: new Dictionary<string, Material> { { GuidNativeMaterial.ToString(), nativeMaterial } },
            nativeTextures:  new Dictionary<string, Texture>  { { GuidNativeTexA.ToString(), nativeTexA } },
            modMaterials:    new Dictionary<string, Material> { { GuidModMaterial.ToString(), modMaterial } },
            modTextures:     new Dictionary<string, Texture>  { { GuidModTexB.ToString(), modTexLower } });

        var useCase = BuildUseCase(stub);
        var request = new ExportTextureRequest(NativePath, ModPath, ScanScene: false);

        // Act
        CaptureConsole(() => useCase.ExportTextures(request));

        // Assert – case-insensitive match → must be skipped
        Assert.That(stub.ExportedTextures, Is.Empty,
            "Texture must be skipped even when the override uses a different name casing.");
    }

    /// <summary>
    ///     Scene scanning has a different purpose from custom material analysis: when a scene
    ///     references a native material, all textures from that native material must remain
    ///     eligible for export even if the mod owns textures with matching names.
    /// </summary>
    [Test]
    public void ExportTextures_WhenSceneScanFindsNativeMaterial_DoesNotFilterSameNameModTextures()
    {
        // Arrange
        var guidNativeTexC = Guid.NewGuid();
        var guidModOverrideA = Guid.NewGuid();
        var guidModOverrideC = Guid.NewGuid();

        var nativeTexA = new Texture(GuidNativeTexA, "native_tex_a", "AssetSources/textures/native_tex_a.dds");
        var nativeTexC = new Texture(guidNativeTexC, "native_tex_c", "AssetSources/textures/native_tex_c.dds");

        var nativeMaterial = new Material(
            GuidNativeMaterial, "native_scene_mat",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase)
            {
                { GuidNativeTexA.ToString(), nativeTexA },
                { guidNativeTexC.ToString(), nativeTexC }
            });

        var modOverrideA = new Texture(guidModOverrideA, "native_tex_a", "AssetSources/textures/native_tex_a.dds");
        var modOverrideC = new Texture(guidModOverrideC, "native_tex_c", "AssetSources/textures/native_tex_c.dds");

        var stub = new AccumulatingTpacFake(
            nativeMaterials: new Dictionary<string, Material> { { GuidNativeMaterial.ToString(), nativeMaterial } },
            nativeTextures: new Dictionary<string, Texture>
            {
                { GuidNativeTexA.ToString(), nativeTexA },
                { guidNativeTexC.ToString(), nativeTexC }
            },
            modMaterials: new Dictionary<string, Material>(),
            modTextures: new Dictionary<string, Texture>
            {
                { guidModOverrideA.ToString(), modOverrideA },
                { guidModOverrideC.ToString(), modOverrideC }
            });

        var sceneFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xscene");
        try
        {
            File.WriteAllText(sceneFile,
                """
                <scene>
                    <mesh material="native_scene_mat" />
                </scene>
                """);

            var useCase = BuildUseCase(stub, new SingleSceneRepository(sceneFile));
            var request = new ExportTextureRequest(NativePath, ModPath, ScanScene: true);

            // Act
            CaptureConsole(() => useCase.ExportTextures(request));

            // Assert
            Assert.That(stub.ExportedTextures.Select(t => t.Name), Is.EquivalentTo(new[]
                {
                    "native_tex_a",
                    "native_tex_c"
                }),
                "Scene-derived native textures must not be removed by the mod override filter.");
        }
        finally
        {
            if (File.Exists(sceneFile))
                File.Delete(sceneFile);
        }
    }

    // ── test doubles ──────────────────────────────────────────────────────────

    /// <summary>
    ///     Simulates <see cref="TpacToolWrapper" />'s accumulating <c>AssetManager</c>.
    ///     Data for the native folder is added on the first <see cref="Load" /> call;
    ///     data for the mod folder is accumulated on the second call.
    /// </summary>
    private sealed class AccumulatingTpacFake : ITpacToolWrapper
    {
        private readonly Dictionary<string, Material> _nativeMaterials;
        private readonly Dictionary<string, Texture>  _nativeTextures;
        private readonly Dictionary<string, Material> _modMaterials;
        private readonly Dictionary<string, Texture>  _modTextures;

        private readonly Dictionary<string, Material> _accumulated = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Texture>  _accumulatedTex = new(StringComparer.OrdinalIgnoreCase);

        private int _loadCallCount;

        public List<Texture> ExportedTextures { get; } = [];

        public AccumulatingTpacFake(
            Dictionary<string, Material> nativeMaterials,
            Dictionary<string, Texture>  nativeTextures,
            Dictionary<string, Material> modMaterials,
            Dictionary<string, Texture>  modTextures)
        {
            _nativeMaterials = nativeMaterials;
            _nativeTextures  = nativeTextures;
            _modMaterials    = modMaterials;
            _modTextures     = modTextures;
        }

        public void Load(string folderPath)
        {
            _loadCallCount++;
            var (mats, texs) = _loadCallCount == 1
                ? (_nativeMaterials, _nativeTextures)
                : (_modMaterials,    _modTextures);

            foreach (var kv in mats) _accumulated[kv.Key]    = kv.Value;
            foreach (var kv in texs) _accumulatedTex[kv.Key] = kv.Value;
        }

        public Dictionary<string, Material> GetLoadedMaterials() =>
            new Dictionary<string, Material>(_accumulated, StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, Texture> GetLoadedTextures() =>
            new Dictionary<string, Texture>(_accumulatedTex, StringComparer.OrdinalIgnoreCase);

        public string ExportTexture(Texture texture, string targetDir)
        {
            ExportedTextures.Add(texture);
            return targetDir;
        }
    }

    /// <summary>Bypasses folder-existence checks so tests don't need real directories.</summary>
    private sealed class AlwaysValidArgumentValidator : IArgumentValidator
    {
        public bool ValidateArguments(ExportTextureRequest exportTextureRequest) => true;
    }

    /// <summary>Returns an empty scene file set so scene scanning never fires.</summary>
    private sealed class EmptySceneRepository : ISceneRepository
    {
        public ISet<string> GetSceneFiles(string modFolder) => new HashSet<string>();
    }

    private sealed class SingleSceneRepository : ISceneRepository
    {
        private readonly string _sceneFile;

        public SingleSceneRepository(string sceneFile)
        {
            _sceneFile = sceneFile;
        }

        public ISet<string> GetSceneFiles(string modFolder) => new HashSet<string> { _sceneFile };
    }
}
