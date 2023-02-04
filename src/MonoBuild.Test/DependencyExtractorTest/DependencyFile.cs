using System.Collections.ObjectModel;

namespace MonoBuild.Test.DependencyExtractorTest;

public class DependencyFile
{
    public const string ValidDependencyFile =
        "# Relative dependancy directory\r\n../Tests/\r\n# Relative dependancy directory windows\r\n..\\Busines\\Logic\\\r\n";

    public const string JunkInJunkOut = "badger\rdoger\rrover lives in a car";
    private const string EmptyLines = "\rdoger\r\r";


    [Fact]
    public void Can_process_valid_dependency_file()
    {
        //Arrange
        DepsFileExtractor sut = new DepsFileExtractor();

        //Act
        var result = sut.GetDependencyFor(ValidDependencyFile);

        //Act
        result.Select(r => r.Path).Should()
            .BeEquivalentTo(new Collection<string>
            { "../Tests/", "../Busines/Logic/" });
    }

    [Fact]
    public void Make_no_attempt_to_validate_strings_readIn()
    {

        //Arrange
        DepsFileExtractor sut = new DepsFileExtractor();

        //Act
        var result = sut.GetDependencyFor(JunkInJunkOut);

        //Act
        result.Select(r => r.Path).Should().BeEquivalentTo(new Collection<string>
            { "badger/","doger/","rover lives in a car/" });
    
    }

    [Fact]
    public void Empty_lines_are_dropped()
    {
        //Arrange
        DepsFileExtractor sut = new DepsFileExtractor();

        //Act
        var result = sut.GetDependencyFor(EmptyLines);

        //Act
        result.Select(r => r.Path).Should().BeEquivalentTo(new Collection<string>
            { "doger/" });
    }
}