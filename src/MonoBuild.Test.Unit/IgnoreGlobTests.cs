using System.Collections.ObjectModel;

namespace MonoBuild.Test.Unit;

public class IgnoreGlobTests
{
    [Fact]
    public void Can_make_local_glob()
    {
        //Arrange
        RepositoryTarget currentBuildDirectory = "src/buildDir";
        var expectedGlob = "*.txt";
        //Act
        var result = IgnoreGlob.Construct(expectedGlob, currentBuildDirectory, new Collection<RepositoryTarget>()) as IgnoreGlob.Local;

        //Assert
        result.Glob.Pattern.Should().Be(expectedGlob);
        
    }

    [Theory]
    [InlineData("**/*", "should handle arbitrary directory glob")]
    [InlineData("*", "should handle any file glob")]
    [InlineData("", "should handle no file after directory")]
    [InlineData("subdirectory/*.txt", "should handle any text in sub-directory")]
    public void Can_ignore_files_which_are_part_of_another_dependency(string globEnd, string description)
    { 
        //Arrange
        RepositoryTarget currentBuildDirectory = "src/buildDir";
        var dependencies = new Collection<RepositoryTarget> { "src/otherBuildDir" };
        
        //Act
        var result = IgnoreGlob.Construct($"../otherBuildDir/.iac/{globEnd}", currentBuildDirectory, dependencies) as IgnoreGlob.Relative;

        //Assert
        result.Target.Directory.Should().Be("src/otherBuildDir");
        result.Glob.Pattern.Should().Be($"src/otherBuildDir/.iac/{globEnd}", description);

    }

    [Fact]
    public void Cannot_ignore_files_which_are_not_part_of_another_dependency()
    {
        //Arrange
        RepositoryTarget currentBuildDirectory = "src/buildDir";
        var dependencies = new Collection<RepositoryTarget> { "src/otherBuildDir" };

        //Act
        Action execution =()=>  IgnoreGlob.Construct("../thirdDependnance/.iac/*", currentBuildDirectory, dependencies);

        //Assert
        execution.Should().Throw<ArgumentException>();

    }


}