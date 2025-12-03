using Bannerlord.NativeTextureExporter.Application.Port;
using Bannerlord.NativeTextureExporter.Infrastrucuture.Spi;

namespace Bannerlord.NativeTextureExporter.Tests.Infrastructure;

[TestFixture]
public class FileSystemSceneRepositoryTests
{
    [SetUp]
    public void Setup()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);

        var modFolder = Path.Combine(_tempRoot, "MyMod");
        Directory.CreateDirectory(modFolder);

        var sceneFolder = Path.Combine(_tempRoot, "SceneObj");
        Directory.CreateDirectory(sceneFolder);

        File.WriteAllText(Path.Combine(sceneFolder, "scene1.xscene"), "<scene></scene>");

        var backupFolder = Path.Combine(sceneFolder, "Backup");
        Directory.CreateDirectory(backupFolder);
        File.WriteAllText(Path.Combine(backupFolder, "scene_backup.xscene"), "<scene></scene>");
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    private string _tempRoot;

    [Test]
    public void GetSceneFiles_ReturnsOnlyNonBackupFiles()
    {
        var modFolder = Path.Combine(_tempRoot, "MyMod");

        var repo = new FileSystemSceneRepository();
        List<string> files = repo.GetSceneFiles(modFolder).ToList();

        Assert.That(files.Count, Is.EqualTo(1));
        Assert.IsTrue(files[0].EndsWith("scene1.xscene", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void GetSceneFiles_ExcludesBackupFolderRegardlessOfCase()
    {
        var backupFolder = Path.Combine(_tempRoot, "SceneObj", "bAcKuP");
        Directory.CreateDirectory(backupFolder);
        File.WriteAllText(Path.Combine(backupFolder, "scene_backup2.xscene"), "<scene></scene>");

        var modFolder = Path.Combine(_tempRoot, "MyMod");
        var repo = new FileSystemSceneRepository();
        List<string> files = repo.GetSceneFiles(modFolder).ToList();

        Assert.That(files.Count, Is.EqualTo(1));
        Assert.IsTrue(files[0].EndsWith("scene1.xscene", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void GetSceneFiles_IncludesFileNamedBackupWhenNotInBackupFolder()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "SceneObj", "backup_scene.xscene"), "<scene></scene>");

        var modFolder = Path.Combine(_tempRoot, "MyMod");
        var repo = new FileSystemSceneRepository();
        List<string> files = repo.GetSceneFiles(modFolder).ToList();

        Assert.That(files.Count, Is.EqualTo(2));
        Assert.IsTrue(files.Any(f => f.EndsWith("scene1.xscene", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(files.Any(f => f.EndsWith("backup_scene.xscene", StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    public void GetSceneFiles_ReturnsEmptyWhenSceneFolderMissing()
    {
        var sceneFolderPath = Path.Combine(_tempRoot, "SceneObj");
        if (Directory.Exists(sceneFolderPath))
            Directory.Delete(sceneFolderPath, true);

        var modFolder = Path.Combine(_tempRoot, "MyMod");
        var repo = new FileSystemSceneRepository();
        List<string> files = repo.GetSceneFiles(modFolder).ToList();

        Assert.That(files.Count, Is.EqualTo(0));
    }

    [Test]
    public void MockTpacToolWrapper_CanBeInstantiated()
    {
        ITpacToolWrapper mockWrapper = new MockTpacToolWrapper();
        Assert.NotNull(mockWrapper);
    }

    // New tests for main_map and modded_map exclusions/inclusions
    [Test]
    public void GetSceneFiles_ExcludesMainMapFolderRegardlessOfCase()
    {
        var mainMapFolder = Path.Combine(_tempRoot, "SceneObj", "MaIn_MaP");
        Directory.CreateDirectory(mainMapFolder);
        File.WriteAllText(Path.Combine(mainMapFolder, "scene_mainmap.xscene"), "<scene></scene>");

        var modFolder = Path.Combine(_tempRoot, "MyMod");
        var repo = new FileSystemSceneRepository();
        List<string> files = repo.GetSceneFiles(modFolder).ToList();

        // Only the original scene1.xscene should be returned
        Assert.That(files.Count, Is.EqualTo(1));
        Assert.IsTrue(files[0].EndsWith("scene1.xscene", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void GetSceneFiles_ExcludesModdedMapFolderRegardlessOfCase()
    {
        var moddedMapFolder = Path.Combine(_tempRoot, "SceneObj", "MoDdEd_MaP");
        Directory.CreateDirectory(moddedMapFolder);
        File.WriteAllText(Path.Combine(moddedMapFolder, "scene_moddedmap.xscene"), "<scene></scene>");

        var modFolder = Path.Combine(_tempRoot, "MyMod");
        var repo = new FileSystemSceneRepository();
        List<string> files = repo.GetSceneFiles(modFolder).ToList();

        // Only the original scene1.xscene should be returned
        Assert.That(files.Count, Is.EqualTo(1));
        Assert.IsTrue(files[0].EndsWith("scene1.xscene", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void GetSceneFiles_IncludesFileNamedMainMapWhenNotInMainMapFolder()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "SceneObj", "main_map_scene.xscene"), "<scene></scene>");

        var modFolder = Path.Combine(_tempRoot, "MyMod");
        var repo = new FileSystemSceneRepository();
        List<string> files = repo.GetSceneFiles(modFolder).ToList();

        Assert.That(files.Count, Is.EqualTo(2));
        Assert.IsTrue(files.Any(f => f.EndsWith("scene1.xscene", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(files.Any(f => f.EndsWith("main_map_scene.xscene", StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    public void GetSceneFiles_IncludesFileNamedModdedMapWhenNotInModdedMapFolder()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "SceneObj", "modded_map_scene.xscene"), "<scene></scene>");

        var modFolder = Path.Combine(_tempRoot, "MyMod");
        var repo = new FileSystemSceneRepository();
        List<string> files = repo.GetSceneFiles(modFolder).ToList();

        Assert.That(files.Count, Is.EqualTo(2));
        Assert.IsTrue(files.Any(f => f.EndsWith("scene1.xscene", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(files.Any(f => f.EndsWith("modded_map_scene.xscene", StringComparison.OrdinalIgnoreCase)));
    }
}