using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;

namespace MonoBuild.Test.BuildTests;

public class Load
{
    private Mock<ILoadBuildDirectory> _loadDirectoryMock;

    public Load()
    {
        _loadDirectoryMock = new Mock<ILoadBuildDirectory>();
    }

    [Fact]
    public async Task Can_construct_for_build_with_no_dependencies()
    {
        //Arrange
        RepositoryTarget buildDirectory = "src/MonoPro";
        AbsoluteTarget target = new AbsoluteTarget(buildDirectory, "C:/TestRepository");
        _loadDirectoryMock.Setup(ld => ld.Load(It.IsAny<AbsoluteTarget>())).ReturnsAsync(new DirectoryLoadResult(
            new Collection<Glob> { "*.md", "*.doc" },
            new Collection<DependencyLocation>()
        ));

        //Act
        var result = await Build.LoadAsync(_loadDirectoryMock.Object, target);

        //Assert
        var expected = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {
                buildDirectory,
                new BuildDirectory(buildDirectory, buildDirectory.IgnoreLocalFilesOfType("*.md", "*.doc"), Build.TARGET)
            }
        };
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("MonoPro.Test.csproj", "can load project files")]
    [InlineData(".monobuild.deps", "can load dependency files")]
    [InlineData("", "can load raw directory")]
    public async Task Can_construct_for_build_with_dependency(
        string dependencyName,
        string reason)
    {
        //Arrange
        RepositoryTarget buildDirectory = "src/MonoPro";
        string dependencyTarget = $"../MonoPro.Test/{dependencyName}";
        AbsoluteTarget target = new AbsoluteTarget(buildDirectory, "C:/TestRepository");
        _loadDirectoryMock.SetupSequence(ld => ld.Load(It.IsAny<AbsoluteTarget>()))
            .ReturnsAsync(LocatedDependencies(dependencyTarget)).ReturnsAsync(EmptyDependency());

        //Act
        var result = await Build.LoadAsync(_loadDirectoryMock.Object, target);

        //Assert 
        var dependencyRepositoryTarget = "src/MonoPro.Test";
        var markdownAndDocumentExclusions = buildDirectory.IgnoreLocalFilesOfType("*.md", "*.doc");
        var expected = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {
                buildDirectory,
                new BuildDirectory(buildDirectory, markdownAndDocumentExclusions, Build.TARGET)
            },
            {
                dependencyRepositoryTarget,
                new BuildDirectory(dependencyRepositoryTarget, markdownAndDocumentExclusions, buildDirectory)
            }
        };
        result.Should().BeEquivalentTo(expected, reason);
    }

    [Fact]
    public async Task Can_load_multiple_dependencies_for_a_given_file()
    {
        //Arrange
        RepositoryTarget buildDirectory = "src/MonoPro";
        string dependencyTarget1 = $"../MonoPro.Test/";
        string dependencytarget2 = "../MonoPro.Core/";
        AbsoluteTarget target = new AbsoluteTarget(buildDirectory, "C:/TestRepository");
        _loadDirectoryMock.SetupSequence(ld => ld.Load(It.IsAny<AbsoluteTarget>()))
            .ReturnsAsync(LocatedDependencies(dependencyTarget1, dependencytarget2)).ReturnsAsync(EmptyDependency())
            .ReturnsAsync(EmptyDependency());

        //Act
        var result = await Build.LoadAsync(_loadDirectoryMock.Object, target);

        //Assert 
        var dependencyRepositoryTarget1 = "src/MonoPro.Test";
        var dependencyRepositoryTarget2 = "src/MonoPro.Core";
        var markdownAndDocumentExclusions = buildDirectory.IgnoreLocalFilesOfType("*.md", "*.doc");
        var expected = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {
                buildDirectory,
                new BuildDirectory(buildDirectory, markdownAndDocumentExclusions, Build.TARGET)
            },
            {
                dependencyRepositoryTarget1,
                new BuildDirectory(dependencyRepositoryTarget1, markdownAndDocumentExclusions, buildDirectory)
            },
            {
                dependencyRepositoryTarget2,
                new BuildDirectory(dependencyRepositoryTarget2, markdownAndDocumentExclusions, buildDirectory)
            }
        };
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Cannot_load_build_with_a_circular_dependency()
    {
        //Arrange
        RepositoryTarget buildDirectory = "src/MonoPro";
        AbsoluteTarget target = new AbsoluteTarget(buildDirectory, "C:/TestRepository");

        _loadDirectoryMock.SetupSequence(ld => ld.Load(It.IsAny<AbsoluteTarget>()))
            .ReturnsAsync(LocatedDependencies("../DependancyA/")).ReturnsAsync(LocatedDependencies("../DependencyB/"))
            .ReturnsAsync(LocatedDependencies("../DependencyC/")).ReturnsAsync(LocatedDependencies("../DependencyD/"))
            .ReturnsAsync(LocatedDependencies("../DependencyB/"));

        //Act
        Func<Task> call = async () => await Build.LoadAsync(_loadDirectoryMock.Object, target);

        //Assert
        await call.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Directory_can_have_multiple_dependencies()
    {
        //Arrange
        var (fileSystem, target) = BuildWhereDependencyHasTwoParents();
        var loadDirectory =
            new LoadBuildDirectory(
                new Collection<IDependencyExtractor> { new DepsFileExtractor(), new ProjDependencyExtractor("*.csproj") },
                new DepsFileExtractor(), fileSystem);

        //Act
        var results = await Build.LoadAsync(loadDirectory, target);

        //Assert
        results.Should().Contain(kv => kv.Value.Parents.Length == 2);
    }



    /// <summary>
    /// Build directory has two dependencies a and b, dependency a also has a dependency on b.
    /// So b has two dependencies, but no circular dependencies.
    /// </summary>
    /// <returns></returns>
    private static (IFileSystem fileSystem, AbsoluteTarget target) BuildWhereDependencyHasTwoParents()
    {
        var buildDirectory = "src/MonoBuild";
        var repositoryDirectory = "C:/MonoBuild";
        var directoryA = "/MonoBuild.A";
        var directoryb = "/MonoBuild.B";

        var mockFileData = new Dictionary<string, MockFileData>
        {

            {
                $"{repositoryDirectory}/{buildDirectory}/MonoBuild.csproj",
                new MockFileData(
                    $"<ProjectReference Include=\"../{directoryA}\\MonoBuild.A.csproj\" />{Environment.NewLine}<ProjectReference Include=\"../{directoryb}\\MonoBuild.B.csproj\" />")
            },
            {
                $"{repositoryDirectory}/src/{directoryA}/MonoBuild.A.csproj",
                new MockFileData($"<ProjectReference Include=\"../{directoryb}\\MonoBuild.B.csproj\" />")
            },
            {
                $"{repositoryDirectory}/src/{directoryb}/MonoBuild.B.csproj", new MockFileData("")
            },
        };
        IFileSystem fileSystem = new MockFileSystem(mockFileData);
        return (fileSystem, new AbsoluteTarget(buildDirectory, repositoryDirectory));
    }


    private static DirectoryLoadResult EmptyDependency()
    {
        return new DirectoryLoadResult(
            new Collection<Glob> { "*.md", "*.doc" },
            new Collection<DependencyLocation>()
        );
    }

    private static DirectoryLoadResult LocatedDependencies(
        params string[] dependancies)
    {
        var dependencyCollection =
            new Collection<DependencyLocation>(dependancies.Select(target => new DependencyLocation(target)).ToList());
        return new DirectoryLoadResult(
            new Collection<Glob> { "*.md", "*.doc" },
            dependencyCollection
        );
    }
}