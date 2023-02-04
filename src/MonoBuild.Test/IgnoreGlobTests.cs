using System.Collections.ObjectModel;

namespace MonoBuild.Test;

public class IgnoreGlobTests
{
    [Fact]
    public void Can_make_local_glob()
    {
        //Arrange
        RepositoryTarget currentBuildDirectory = new RepositoryTarget("src/buildDir");
        var expectedGlob = "*.txt";
        //Act
        var result = IgnoreGlob.Construct(new Glob(expectedGlob), currentBuildDirectory, new Collection<RepositoryTarget>()) as IgnoreGlob.Local;

        //Assert
        result.Glob.Pattern.Should().Be(expectedGlob);
        
    }

    [Theory]
    [InlineData("**/*", "should handle arbitrary directory glob")]
    [InlineData("*", "should handle any file glob")]
    [InlineData("", "should handle no file after directory")]
    [InlineData("subdirectory/*.txt", "should handle any text in sub-directory")]


    public void Can_ignore_relative_files_which_are_part_of_another_dependency(string globEnd, string description)
    { 
        //Arrange
        RepositoryTarget currentBuildDirectory = new RepositoryTarget("src/buildDir");
        var dependencies = new Collection<RepositoryTarget> { new RepositoryTarget("src/otherBuildDir") };
        
        //Act
        var result = IgnoreGlob.Construct(new Glob($"../otherBuildDir/.iac/{globEnd}"), currentBuildDirectory, dependencies) as IgnoreGlob.Relative;

        //Assert
        result.Target.Directory.Should().Be("src/otherbuilddir");
        result.Glob.Pattern.Should().Be($"src/otherbuilddir/.iac/{globEnd}", description);

    }
    [Fact]
    public void
        When_constructing_relative_glob_the_longest_dependent_directory_which_the_glob_matches_should_be_chosen()
    {
        //Arrange
        RepositoryTarget currentBuildDirectory = new RepositoryTarget("src/buildDir");
        var dependencies = new Collection<RepositoryTarget>
        {
            new RepositoryTarget("src/otherBuildDir"),
            new RepositoryTarget("src/otherBuildDir/sub"),
            new RepositoryTarget("src/otherBuildDir/sub/sub2"),
            new RepositoryTarget("src/otherBuildDir/sub/sub2/sub3"),
            new RepositoryTarget("src/otherBuildDir/sub/sub2/sub3/sub4"),
            new RepositoryTarget("src/otherBuildDir/sub/sub2/sub3/sub4V2"),
            new RepositoryTarget("src/otherBuildDir/sub/sub2/sub3/sub4/deeper1"),
            new RepositoryTarget("src/otherBuildDir/sub/sub2/sub3/sub4/deeper2"),
        };
       

        //Act
        var result = IgnoreGlob.Construct(new Glob($"../otherBuildDir/sub/sub2/sub3/sub4/*.xml"), currentBuildDirectory, dependencies) as IgnoreGlob.Relative;

        //Assert
        result.Target.Directory.Should().Be("src/otherbuilddir/sub/sub2/sub3/sub4");

    }


    [Fact]
    public void Cannot_ignore_files_which_are_not_part_of_another_dependency()
    {
        //Arrange
        RepositoryTarget currentBuildDirectory = new RepositoryTarget("src/buildDir");
        var dependencies = new Collection<RepositoryTarget> { new RepositoryTarget("src/otherBuildDir") };

        //Act
        Action execution =()=>  IgnoreGlob.Construct(new Glob("../thirdDependnance/.iac/*"), currentBuildDirectory, dependencies);

        //Assert
        execution.Should().Throw<ArgumentException>();

    }


}