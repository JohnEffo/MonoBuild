using System.Collections.ObjectModel;

namespace MonoBuild.Test.DependencyExtractorTest;

public class DependencyFile
{
    public const string ValidDependencyFile =
        "# Local dependancy\r\n*.md\r\n# Relative dependancy directory\r\n../Tests/\r\n# Relative dependancy directory windows\r\n..\\Busines\\Logic\\\r\n# Relative dependancy file\r\n../Business/Magic/Magic.csproj";

    public const string JunkInJunkOut = "badger\rdoger\rrover lives in a car";
    private const string EmptyLines = "\rdoger\r\r";

    [Theory]
    [InlineData("dependencyDirectory/", true, "if dependency not set then it self parent defaults to true")]
    [InlineData("self:dependencyDirectory/", true, "if dependency  set self then self parent is true")]
    [InlineData("parent:dependencyDirectory/", false,
        "if dependency  set to parent then current build directory is parent")]
    public void Can_determine_self_reference_and_parent_dependencies(
        string dependancy,
        bool doesSelfParent,
        string reason)
    {
        //Arrange
        DepsFileExtractor sut = new DepsFileExtractor();

        //Act
        var result = sut.GetDependencyFor(dependancy);

        //Act
        result.Select(r => r.SelfParent).Should()
            .AllSatisfy(selfparent => selfparent.Should().Be(doesSelfParent), reason);

    }

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
        result.Select(r => r.Path).Should().BeEquivalentTo(new Collection<string>
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
        result.Select(r => r.Path).Should().BeEquivalentTo(new Collection<string>
            { "doger" });
    }
}