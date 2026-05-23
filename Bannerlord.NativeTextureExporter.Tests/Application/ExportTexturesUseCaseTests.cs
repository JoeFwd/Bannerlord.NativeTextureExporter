using Bannerlord.NativeTextureExporter.Application;
using Bannerlord.NativeTextureExporter.Application.Dto;
using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Application.Util;
using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Tests.Application;

[TestFixture]
public class ExportTexturesUseCaseTests
{
    private const string NativePath = "fake_native";
    private const string ModPath = "fake_mod";

    [Test]
    public void ExportTextures_WhenNativeTextureIsNotOverridden_ExportsIt()
    {
        var nativeTex = Texture("native_tex_a");
        var fake = BuildFake(
            nativeTextures: [nativeTex],
            modMaterialTextures: [nativeTex],
            modTextures: [Texture("mod_tex_b")]);

        CaptureConsole(() => BuildUseCase(fake).ExportTextures(Request()));

        Assert.That(ExportedNames(fake), Is.EqualTo(new[] { "native_tex_a" }));
    }

    [Test]
    public void ExportTextures_WhenNativeTextureIsOverriddenByMod_SkipsIt()
    {
        var nativeTex = Texture("native_tex_a");
        var fake = BuildFake(
            nativeTextures: [nativeTex],
            modMaterialTextures: [nativeTex],
            modTextures: [Texture("native_tex_a")]);

        var output = CaptureConsole(() => BuildUseCase(fake).ExportTextures(Request()));

        Assert.That(fake.ExportedTextures, Is.Empty);
        Assert.That(output, Does.Contain("Skipping native texture 'native_tex_a'"));
    }

    [Test]
    public void ExportTextures_WhenSomeNativeTexturesOverridden_ExportsOnlyNonOverridden()
    {
        var nativeTexA = Texture("native_tex_a");
        var nativeTexC = Texture("native_tex_c");
        var fake = BuildFake(
            nativeTextures: [nativeTexA, nativeTexC],
            modMaterialTextures: [nativeTexA, nativeTexC],
            modTextures: [Texture("native_tex_c")]);

        var output = CaptureConsole(() => BuildUseCase(fake).ExportTextures(Request()));

        Assert.That(ExportedNames(fake), Is.EqualTo(new[] { "native_tex_a" }));
        Assert.That(output, Does.Contain("Skipping native texture 'native_tex_c'"));
    }

    [Test]
    public void ExportTextures_WhenModOverridesWithDifferentCaseName_SkipsIt()
    {
        var nativeTex = Texture("Native_Tex_A");
        var fake = BuildFake(
            nativeTextures: [nativeTex],
            modMaterialTextures: [nativeTex],
            modTextures: [Texture("native_tex_a")]);

        CaptureConsole(() => BuildUseCase(fake).ExportTextures(Request()));

        Assert.That(fake.ExportedTextures, Is.Empty);
    }

    [Test]
    public void ExportTextures_WhenSceneScanFindsNativeMaterial_DoesNotFilterSameNameModTextures()
    {
        var nativeTexA = Texture("native_tex_a");
        var nativeTexC = Texture("native_tex_c");
        var fake = BuildFake(
            nativeMaterialName: "native_scene_mat",
            nativeTextures: [nativeTexA, nativeTexC],
            modMaterialTextures: [],
            modTextures: [Texture("native_tex_a"), Texture("native_tex_c")]);

        var sceneFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xscene");
        try
        {
            File.WriteAllText(sceneFile,
                """
                <scene>
                    <mesh material="native_scene_mat" />
                </scene>
                """);

            CaptureConsole(() => BuildUseCase(fake, new SingleSceneRepository(sceneFile))
                .ExportTextures(Request(scanScene: true)));

            Assert.That(ExportedNames(fake), Is.EquivalentTo(new[]
            {
                "native_tex_a",
                "native_tex_c"
            }));
        }
        finally
        {
            if (File.Exists(sceneFile))
                File.Delete(sceneFile);
        }
    }

    private static ExportTexturesUseCase BuildUseCase(
        AccumulatingTpacFake fake,
        ISceneRepository? sceneRepository = null) =>
        new(
            fake,
            new SceneTextureExtractorUtil(sceneRepository ?? new EmptySceneRepository()),
            new GetExportTextureFolderPathUtil(),
            new NativeTextureOverrideFilterUtil(),
            new AlwaysValidArgumentValidator());

    private static AccumulatingTpacFake BuildFake(
        IReadOnlyCollection<Texture> nativeTextures,
        IReadOnlyCollection<Texture> modMaterialTextures,
        IReadOnlyCollection<Texture> modTextures,
        string nativeMaterialName = "native_mat")
    {
        var nativeMaterial = Material(nativeMaterialName, nativeTextures);
        var modMaterials = modMaterialTextures.Count == 0
            ? new Dictionary<string, Material>()
            : ToMaterialMap(Material("mod_mat", modMaterialTextures));

        return new AccumulatingTpacFake(
            nativeMaterials: ToMaterialMap(nativeMaterial),
            nativeTextures: ToTextureMap(nativeTextures),
            modMaterials: modMaterials,
            modTextures: ToTextureMap(modTextures));
    }

    private static ExportTextureRequest Request(bool scanScene = false) =>
        new(NativePath, ModPath, scanScene);

    private static Texture Texture(string name) =>
        new(Guid.NewGuid(), name, $"AssetSources/textures/{name}.dds");

    private static Material Material(string name, IReadOnlyCollection<Texture> textures) =>
        new(Guid.NewGuid(), name, ToTextureMap(textures));

    private static Dictionary<string, Material> ToMaterialMap(Material material) =>
        new(StringComparer.OrdinalIgnoreCase) { { material.Guid.ToString(), material } };

    private static Dictionary<string, Texture> ToTextureMap(IEnumerable<Texture> textures) =>
        textures.ToDictionary(t => t.Guid.ToString(), t => t, StringComparer.OrdinalIgnoreCase);

    private static string[] ExportedNames(AccumulatingTpacFake fake) =>
        fake.ExportedTextures.Select(t => t.Name).ToArray();

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
}
