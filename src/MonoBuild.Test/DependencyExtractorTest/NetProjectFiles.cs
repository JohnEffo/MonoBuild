using System.Collections.ObjectModel;

namespace MonoBuild.Test.DependencyExtractorTest;

public class NetProjectFiles
{
    public const string ValidProjectFileWithDependencyCSharp =
        "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n\r\n  <PropertyGroup>\r\n    <TargetFramework>net7.0</TargetFramework>\r\n    <ImplicitUsings>enable</ImplicitUsings>\r\n    <Nullable>enable</Nullable>\r\n\r\n    <IsPackable>false</IsPackable>\r\n  </PropertyGroup>\r\n\r\n  <ItemGroup>\r\n    <PackageReference Include=\"FluentAssertions\" Version=\"6.9.0\" />\r\n    <PackageReference Include=\"FluentResults\" Version=\"3.15.0\" />\r\n    <PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"17.3.2\" />\r\n    <PackageReference Include=\"Moq\" Version=\"4.18.4\" />\r\n    <PackageReference Include=\"System.IO.Abstractions\" Version=\"19.1.13\" />\r\n    <PackageReference Include=\"System.IO.Abstractions.TestingHelpers\" Version=\"19.1.13\" />\r\n    <PackageReference Include=\"xunit\" Version=\"2.4.2\" />\r\n    <PackageReference Include=\"xunit.runner.visualstudio\" Version=\"2.4.5\">\r\n      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>\r\n      <PrivateAssets>all</PrivateAssets>\r\n    </PackageReference>\r\n    <PackageReference Include=\"coverlet.collector\" Version=\"3.1.2\">\r\n      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>\r\n      <PrivateAssets>all</PrivateAssets>\r\n    </PackageReference>\r\n  </ItemGroup>\r\n\r\n  <ItemGroup>\r\n    <ProjectReference Include=\"..\\MonoBuild.Core\\MonoBuild.Core.csproj\" />\r\n  </ItemGroup>\r\n\r\n</Project>\r\n";

    /// <summary>
    /// https://github.com/fscheck/FsCheck/blob/master/tests/FsCheck.Test/FsCheck.Test.fsproj
    /// </summary>
    public const string ValidProjectFileFSharp =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Project Sdk=\"Microsoft.NET.Sdk\">\r\n  <PropertyGroup>\r\n    <AssemblyName>FsCheck.Test</AssemblyName>\r\n    <TargetFramework>net5.0</TargetFramework>\r\n    <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>\r\n  </PropertyGroup>\r\n  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|AnyCPU'\">\r\n    <DefineConstants>DEBUG</DefineConstants>\r\n  </PropertyGroup>\r\n  <ItemGroup>\r\n    <Compile Include=\"Fscheck.XUnit\\PropertyAttributeTests.fs\" />\r\n    <Compile Include=\"Helpers.fs\" />\r\n    <Compile Include=\"Random.fs\" />\r\n    <Compile Include=\"TypeClass.fs\" />\r\n    <Compile Include=\"Gen.fs\" />\r\n    <Compile Include=\"GenExtensions.fs\" />\r\n    <Compile Include=\"Arbitrary.fs\" />\r\n    <Compile Include=\"Property.fs\" />\r\n    <Compile Include=\"Commands.fs\" />\r\n    <Compile Include=\"Runner.fs\" />\r\n    <Compile Include=\"StateMachine.fs\" />\r\n    <None Include=\"paket.references\" />\r\n    <Content Include=\"App.config\" />\r\n  </ItemGroup>\r\n  <ItemGroup>\r\n    <ProjectReference Include=\"../../src/FsCheck.Xunit/FsCheck.Xunit.fsproj\" />\r\n    <ProjectReference Include=\"..\\FsCheck.Test.CSharp\\FsCheck.Test.CSharp.csproj\" />\r\n  </ItemGroup>\r\n  <Import Project=\"..\\..\\.paket\\Paket.Restore.targets\" />\r\n</Project>";

    public const string ValidProjectFileNoDependancies =
        "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n\r\n  <PropertyGroup>\r\n    <TargetFramework>net7.0</TargetFramework>\r\n    <ImplicitUsings>enable</ImplicitUsings>\r\n    <Nullable>enable</Nullable>\r\n  </PropertyGroup>\r\n\r\n  <ItemGroup>\r\n    <PackageReference Include=\"FluentResults\" Version=\"3.15.0\" />\r\n    <PackageReference Include=\"LibGit2Sharp\" Version=\"0.26.2\" />\r\n    <PackageReference Include=\"Microsoft.Extensions.FileSystemGlobbing\" Version=\"7.0.0\" />\r\n    <PackageReference Include=\"System.IO.Abstractions\" Version=\"19.1.13\" />\r\n  </ItemGroup>\r\n\r\n</Project>";

    [Fact]
    public void Can_can_retrieve_dependencies_from_valid_project_file_CSharp()
    {
        //Arrange
        var sut = new ProjDependencyExtractor("*.csproj");

        //Act
        var result = sut.GetDependencyFor(ValidProjectFileWithDependencyCSharp);

        //Assert
        result
            .Select(r => r.Path)
            .Should().BeEquivalentTo(new Collection<string> { "../MonoBuild.Core/MonoBuild.Core.csproj" });
    }

    [Fact]
    public void Can_can_retrieve_dependencies_from_valid_project_file_Fsharp()
    {
        //Arrange
        var sut = new ProjDependencyExtractor("*.fsproj");

        //Act
        var result = sut.GetDependencyFor(ValidProjectFileFSharp);

        //Assert
        result
            .Select(r => r.Path)
            .Should()
            .BeEquivalentTo(new Collection<string> { "../../src/FsCheck.Xunit/FsCheck.Xunit.fsproj", "../FsCheck.Test.CSharp/FsCheck.Test.CSharp.csproj" });
    }

    [Fact]
    public void Can_can_retrieve_dependencies_from_valid_project_file_with_no_dependencies()
    {
        //Arrange
        var sut = new ProjDependencyExtractor("*.csproj");

        //Act
        var result = sut.GetDependencyFor(ValidProjectFileNoDependancies);

        //Assert
        result.Select(r => r.Path).Should().BeEquivalentTo(new Collection<string> {  });
    }

}