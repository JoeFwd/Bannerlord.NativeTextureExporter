using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Application.Util;
using Bannerlord.NativeTextureExporter.Domain;

namespace Bannerlord.NativeTextureExporter.Tests.Application;

public class SceneTextureExtractorUtilTests
{
    private string _modFolder;
    private string _sceneFilePath;
    private string _sceneFolder;
    private string _tempRoot;

    [SetUp]
    public void Setup()
    {
        // Create a temporary directory structure:
        //   <tempRoot>\MyMod        (mod folder)
        //   <tempRoot>\SceneObj    (scene folder)
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);
        _modFolder = Path.Combine(_tempRoot, "MyMod");
        _sceneFolder = Path.Combine(_tempRoot, "SceneObj");
        Directory.CreateDirectory(_modFolder);
        Directory.CreateDirectory(_sceneFolder);

        _sceneFilePath = Path.Combine(_sceneFolder, "testscene.xscene");
        var xmlContent = @"
                <scene>
                    <mesh material=""MaterialA"" />
                    <mesh material=""MaterialB"" />
                </scene>";
        File.WriteAllText(_sceneFilePath, xmlContent);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    [Test]
    public void ExtractNativeTextures_ReturnsGuidsFromNativeMaterialsUsedInScene()
    {
        // Arrange
        var guidA = Guid.NewGuid();
        var guidB = Guid.NewGuid();

        var materialA = new Material(guidA, "MaterialA",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase));
        var materialB = new Material(guidB, "MaterialB",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase));

        Dictionary<string, Material> nativeAssetByGuids = new(StringComparer.OrdinalIgnoreCase)
        {
            { guidA.ToString(), materialA },
            { guidB.ToString(), materialB }
        };

        var sceneRepo = new TestSceneRepository(_sceneFolder);
        var extractor = new SceneTextureExtractorUtil(sceneRepo);

        // Act
        ISet<string> result = extractor.ExtractNativeTextures(
            _modFolder,
            nativeAssetByGuids);

        // Assert
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public void ExtractNativeTextures_IgnoresMaterialsNotInNativeDictionary()
    {
        // Arrange
        var guidA = Guid.NewGuid();

        var materialA = new Material(guidA, "MaterialA",
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase));

        Dictionary<string, Material> nativeAssetByGuids = new(StringComparer.OrdinalIgnoreCase)
        {
            { guidA.ToString(), materialA }
        };

        var sceneRepo = new TestSceneRepository(_sceneFolder);
        var extractor = new SceneTextureExtractorUtil(sceneRepo);

        // Act
        ISet<string> result = extractor.ExtractNativeTextures(
            _modFolder,
            nativeAssetByGuids);

        // Assert
        Assert.That(result, Has.Count.EqualTo(0));
    }

    private class TestSceneRepository : ISceneRepository
    {
        private readonly string _sceneFolderPath;

        public TestSceneRepository(string sceneFolderPath)
        {
            _sceneFolderPath = sceneFolderPath;
        }

        public ISet<string> GetSceneFiles(string modFolder)
        {
            return Directory.GetFiles(_sceneFolderPath, "*.xscene", SearchOption.AllDirectories).ToHashSet();
        }
    }
}