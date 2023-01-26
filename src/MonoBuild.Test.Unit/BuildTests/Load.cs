using System.Collections.ObjectModel;

namespace MonoBuild.Test.Unit.BuildTests;

public class Load
{
    private Mock<ILoadBuildDirectory> _loadDirectoryMock;

    public Load()
    {
        _loadDirectoryMock = new Mock<ILoadBuildDirectory>();
    }

    [Fact]
    public void Can_construct_for_build_with_no_dependencies()
    {
        //Arrange
        RepositoryTarget buildDirectory = "src/MonoPro";
        AbsoluteTarget target = new AbsoluteTarget(buildDirectory, "C:/TestRepository");
        _loadDirectoryMock.Setup(ld => ld.Load(It.IsAny<AbsoluteTarget>())).Returns(new DirectoryLoadResult(
            new Collection<Glob> { "*.md", "*.doc" },
            new Collection<DependancyLocation>()
        ));

        //Act
        var result = Build.Load(_loadDirectoryMock.Object, target);

        //Assert
        var expected = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {
                buildDirectory, new BuildDirectory(buildDirectory, buildDirectory.IgnoreLocalFilesOfType("*.md", "*.doc"), Build.TARGET)
            }
        };
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("MonoPro.Test.csproj", "can load project files")]
    [InlineData("MonoPro.Test.deps", "can load dependency files")]
    [InlineData("", "can load raw directory")]
    public void Can_construct_for_build_with_dependency(string dependencyName,string reason)
    {
        //Arrange
        RepositoryTarget buildDirectory = "src/MonoPro";
        string dependencyTarget = $"../MonoPro.Test/{dependencyName}";
        AbsoluteTarget target = new AbsoluteTarget(buildDirectory, "C:/TestRepository");
        _loadDirectoryMock.SetupSequence(ld => ld.Load(It.IsAny<AbsoluteTarget>())).
            Returns(LocatedDependencies(dependencyTarget)).
            Returns(EmptyDependency());

        //Act
        var result = Build.Load(_loadDirectoryMock.Object, target);

        //Assert 
        var dependencyRepositoryTarget = "src/MonoPro.Test";
        var markdownAndDocumentExclusions = buildDirectory.IgnoreLocalFilesOfType("*.md", "*.doc");
        var expected = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {
                buildDirectory,
                new BuildDirectory(buildDirectory, markdownAndDocumentExclusions, Build.TARGET) },
            {dependencyRepositoryTarget,new BuildDirectory(dependencyRepositoryTarget,markdownAndDocumentExclusions, buildDirectory)}
        };
        result.Should().BeEquivalentTo(expected,reason);
    }

    [Fact]
    public void Can_load_multiple_dependencies_for_a_given_file()
    {
        //Arrange
        RepositoryTarget buildDirectory = "src/MonoPro";
        string dependencyTarget1 = $"../MonoPro.Test/";
        string dependencytarget2 = "../MonoPro.Core/";
        AbsoluteTarget target = new AbsoluteTarget(buildDirectory, "C:/TestRepository");
        _loadDirectoryMock.SetupSequence(ld => ld.Load(It.IsAny<AbsoluteTarget>())).
            Returns(LocatedDependencies( dependencyTarget1,dependencytarget2)).
            Returns(EmptyDependency()).
            Returns(EmptyDependency());

        //Act
        var result = Build.Load(_loadDirectoryMock.Object, target);

        //Assert 
        var dependencyRepositoryTarget1 = "src/MonoPro.Test";
        var dependencyRepositoryTarget2 = "src/MonoPro.Core";
        var markdownAndDocumentExclusions = buildDirectory.IgnoreLocalFilesOfType("*.md", "*.doc");
        var expected = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {
                buildDirectory,
                new BuildDirectory(buildDirectory, markdownAndDocumentExclusions, Build.TARGET) },
            {dependencyRepositoryTarget1,new BuildDirectory(dependencyRepositoryTarget1,markdownAndDocumentExclusions, buildDirectory)},
            {dependencyRepositoryTarget2,new BuildDirectory(dependencyRepositoryTarget2,markdownAndDocumentExclusions, buildDirectory)}
        };
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Cannot_load_build_with_a_circular_dependency()
    {
        //Arrange
        RepositoryTarget buildDirectory = "src/MonoPro";
        AbsoluteTarget target = new AbsoluteTarget(buildDirectory, "C:/TestRepository");
        
        _loadDirectoryMock.SetupSequence(ld => ld.Load(It.IsAny<AbsoluteTarget>()))
            .Returns(LocatedDependencies("../DependancyA/")).Returns(LocatedDependencies("../DependencyB/"))
            .Returns(LocatedDependencies("../DependencyC/")).Returns(LocatedDependencies("../DependencyD/"))
            .Returns(LocatedDependencies("../DependencyB/"));

        //Act
        Action call =()=> Build.Load(_loadDirectoryMock.Object, target);
        
        //Assert
        call.Should().Throw<InvalidOperationException>();

    }

    private static DirectoryLoadResult EmptyDependency()
    {
        return new DirectoryLoadResult(
            new Collection<Glob> { "*.md", "*.doc" },
            new Collection<DependancyLocation>()
        );
    }

    private static DirectoryLoadResult LocatedDependencies(
        params string[] dependancies)
    {
        var dependencyCollection =
            new Collection<DependancyLocation>(dependancies.Select(target => new DependancyLocation(target)).ToList());
        return new DirectoryLoadResult(
            new Collection<Glob> { "*.md", "*.doc" },
            dependencyCollection
        );
    }
}