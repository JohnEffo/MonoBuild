using System.Collections.ObjectModel;

namespace MonoBuild.Test.Unit.DependencyExtractorTest;

public class DependencyFile
{
    public const string ValidDependencyFile =
        "# Local dependancy\r\n*.md\r\n# Relative dependancy directory\r\n../Tests/\r\n# Relative dependancy directory windows\r\n..\\Busines\\Logic\\\r\n# Relative dependancy file\r\n../Business/Magic/Magic.csproj";

    public const string JunkInJunkOut = "badger\rdoger\rrover lives in a car";
public const string EmptyLines = "\rdoger\r\r";

    [Fact]
    public void Can_process_valid_dependency_file()
    {
        //Arrange
        DepsFileExtractor sut = new DepsFileExtractor();

        //Act
        var result = sut.GetDependencyFor(ValidDependencyFile);

        //Act
        result.Should().BeEquivalentTo(new Collection<string>
            { "*.md", "../Tests/", "../Busines/Logic/", "../Business/Magic/Magic.csproj" });
    }

    [Fact]
    public void Make_no_attempt_to_validate_strings_readIn()
    {

        //Arrange
        DepsFileExtractor sut = new DepsFileExtractor();

        //Act
        var result = sut.GetDependencyFor(JunkInJunkOut);

        //Act
        result.Should().BeEquivalentTo(new Collection<string>
            { "badger","doger","rover lives in a car" });
    
    }

[Fact]
    public void Empty_lines_are_dropped()
    {

        //Arrange
        DepsFileExtractor sut = new DepsFileExtractor();

        //Act
        var result = sut.GetDependencyFor(EmptyLines);

        //Act
        result.Should().BeEquivalentTo(new Collection<string>
            { "doger" });
    
    }
}