using System.Collections.ObjectModel;
using FluentResults;

namespace MonoBuild.Test.BuildTests;

public class IsRequired
{
    [Fact]
    public void Given_no_file_in_build_directory_changed_When_Test_Then_no_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = new("src/builddir") ;
        ISet<string> changes = new HashSet<string> { "src/nonBuildDire/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory,new Collection<IgnoreGlob>(), new RepositoryTarget(Build.TARGET) )}
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.No>();
    }

    [Fact]
    public void Given_file_changed_in_build_directory_And_file_type_not_ignored_When_Test_Then_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        ISet<string> changes = new HashSet<string> { $"{buildDirectory.Directory}/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(),new RepositoryTarget(Build.TARGET))}
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.Yes>();
    }

    [Fact]
    public void Given_files_changed_in_build_directory_And_not_all_files_ignored_When_Test_Then_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = new("src/builddir");
        ISet<string> changes = new HashSet<string> { $"{buildDirectory.Directory}/somefile.cs", $"{buildDirectory.Directory}/somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory,buildDirectory.IgnoreLocalFilesOfType("*.md"),new RepositoryTarget(Build.TARGET))}
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.Yes>();
    }

    [Theory]
    [InlineData("/somefile.cs","file in main directory of dependency")]
    [InlineData("/Subdirectory/somefile.cs","file in main sub-directory of dependency")]
    public void Given_file_changed_in_dependency_of_build_directory_And_file_type_not_ignored_When_Test_Then_build_required(string fileName,string reason)
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        RepositoryTarget dependencyDirectory = new RepositoryTarget("src/dependency");
        ISet<string> changes = new HashSet<string> { $"{dependencyDirectory.Directory}/{fileName}" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(),new RepositoryTarget(Build.TARGET))},
            {dependencyDirectory,new BuildDirectory(dependencyDirectory,new Collection<IgnoreGlob>(), buildDirectory) }
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.Yes>(reason);
    }

    [Theory]
    [InlineData("**/*.md","/quite/a/few/directory/levels/deep/","all directory blobs should match")]
    [InlineData("quite/a/few/directory/levels/deep/*.md", "/quite/a/few/directory/levels/deep/", "complete relative path should match")]
    public void Given_file_changed_in_build_directory_And_file_type_is_ignored_When_Test_Then_no_build_required(string blob,string subDirectory,string reason)
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        ISet<string> changes = new HashSet<string> { $"{buildDirectory.Directory}{subDirectory}somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory, buildDirectory.IgnoreLocalFilesOfType(blob), new RepositoryTarget(Build.TARGET)) }
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.No>("reason");
    }

    [Theory]
    [InlineData("quite/a/few/directory/levels/deep/*.md", "/quite/a/few/directory/levels/deeper/", "paths do not match blob has mis-mated final leaf 'deeper'" )]
    [InlineData("quite/a/few/directory/.md", "/quite/a/few/directory/levels/", "paths do not match blob has extra level")]

    public void Given_file_changed_in_build_directory_And_ignore_does_not_match_When_Test_Then_build_required(string blob, string subDirectory, string reason)
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        ISet<string> changes = new HashSet<string> { $"{buildDirectory.Directory}{subDirectory}somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory, buildDirectory.IgnoreLocalFilesOfType(blob), new RepositoryTarget(Build.TARGET)) }
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.Yes>("reason");
    }

    [Fact]
    public void Given_file_changed_in_dependency_of_build_directory_And_file_type_ignored_locally_by_dependent_directory_When_Test_Then_no_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        RepositoryTarget dependencyDirectory = new RepositoryTarget("src/dependency");
        ISet<string> changes = new HashSet<string> { $"{dependencyDirectory.Directory}/somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(),new RepositoryTarget(Build.TARGET))},
            {dependencyDirectory,new BuildDirectory(dependencyDirectory,dependencyDirectory.IgnoreLocalFilesOfType("*.md"), buildDirectory) }
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.No>();
    }

    [Fact]
    public void Given_file_changed_in_dependency_of_build_directory_And_file_type_ignored_using_relative_glob_in_build_directory_When_Test_Then_no_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        var dependencySubdirectory = "dependency";
        RepositoryTarget dependencyDirectory =new RepositoryTarget( $"src/{dependencySubdirectory}");
        ISet<string> changes = new HashSet<string> { $"{dependencyDirectory.Directory}/somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {
                buildDirectory, new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>
                {
                    IgnoreGlob.Construct(new Glob($"../{dependencySubdirectory}/**/*.md"), buildDirectory,
                        new Collection<RepositoryTarget> { dependencyDirectory })
                }, new RepositoryTarget(Build.TARGET))
            },
            { dependencyDirectory, new BuildDirectory(dependencyDirectory,new Collection<IgnoreGlob>(), buildDirectory) }
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.No>();
    }

    [Fact]
    public void Given_file_changed_in_dependency_of_dependancy_of_build_directory_And_file_type_ignored_using_relative_glob_in_build_directory_When_Test_Then_no_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        var initialDependency = "dependency";
        RepositoryTarget dependencyDirectory = new RepositoryTarget($"src/{initialDependency}");
        var transitiveDependency = "transitive";
        RepositoryTarget transativeDirectory = new RepositoryTarget($"src/{transitiveDependency}");
        ISet<string> changes = new HashSet<string> { $"{transativeDirectory.Directory}/somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {
                buildDirectory, new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>
                {
                    IgnoreGlob.Construct(new Glob($"../{transitiveDependency}/**/*.md"), buildDirectory,
                        new Collection<RepositoryTarget> { dependencyDirectory ,transativeDirectory})
                }, new RepositoryTarget(Build.TARGET))
            },
            { dependencyDirectory, new BuildDirectory(dependencyDirectory,new Collection<IgnoreGlob>(), buildDirectory) },
            { transativeDirectory, new BuildDirectory(transativeDirectory,new Collection<IgnoreGlob>(), dependencyDirectory,buildDirectory) }

        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.No>();
    }

    [Fact]
    public void Given_file_changed_in_dependency_of_build_directory_And_all_files_ignored_using_relative_glob_in_build_directory_But_subdirectory_containing_changed_file_is_also_a_dependency_When_Test_Then_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        var dependencySubdirectory = "dependency";
        RepositoryTarget dependencyDirectory = new RepositoryTarget($"src/{dependencySubdirectory}");
        RepositoryTarget dependencyDirectoryAPI = new RepositoryTarget($"{dependencyDirectory}/API");
        ISet<string> changes = new HashSet<string> { $"{dependencyDirectoryAPI.Directory}/somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {
                buildDirectory, new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>
                {
                    IgnoreGlob.Construct(new Glob($"../{dependencySubdirectory}/**/*.md"), buildDirectory,
                        new Collection<RepositoryTarget> { dependencyDirectory })
                }, new RepositoryTarget(Build.TARGET))
            },
            { dependencyDirectory, new BuildDirectory(dependencyDirectory,new Collection<IgnoreGlob>(), buildDirectory) },
            {dependencyDirectoryAPI,new BuildDirectory(dependencyDirectoryAPI,new Collection<IgnoreGlob>(),buildDirectory)}
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.Yes>();
    }

    [Fact]
    public void
        Given_file_changed_in_dependency_with_multiple_parents_And_not_all_parents_ignore_file_When_Test_Then_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        RepositoryTarget immediateDependencyA = new RepositoryTarget("src/dependencyA");
        RepositoryTarget immediateDependencyB = new RepositoryTarget("src/dependencyB");
        string transitiveSubdirectory = "transitive";
        RepositoryTarget transitiveDependency =new RepositoryTarget( $"src/{transitiveSubdirectory}");
        ISet<string> changes = new HashSet<string> { $"{transitiveDependency.Directory}/notImportantToA/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            { buildDirectory, new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(), new RepositoryTarget(Build.TARGET))},
            {immediateDependencyA, new BuildDirectory(immediateDependencyA, new Collection<IgnoreGlob>
            {
                IgnoreGlob.Construct(new Glob($"../{transitiveSubdirectory}/notImportantToA/**/*.cs"), immediateDependencyA,
                    new Collection<RepositoryTarget> { transitiveDependency })
            }, buildDirectory)},
            {immediateDependencyB, new BuildDirectory(immediateDependencyB, new Collection<IgnoreGlob>(), buildDirectory)},
            {transitiveDependency,new BuildDirectory(transitiveDependency, new Collection<IgnoreGlob>(), immediateDependencyA,immediateDependencyB)},
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.Yes>();
    }

    [Fact]
    public void
        Given_file_changed_in_dependency_with_parents_AandB_And_A_ignores_directory_And_B_ignores_different_directory_When_Test_Then_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        RepositoryTarget immediateDependencyA = new RepositoryTarget("src/dependencyA");
        RepositoryTarget immediateDependencyB = new RepositoryTarget("src/dependencyB");
        var transitiveSubdirectory = "transitive";
        RepositoryTarget transitiveDependency = new RepositoryTarget($"src/{transitiveSubdirectory}");
        ISet<string> changes = new HashSet<string> { $"{transitiveDependency.Directory}/notImportantToA/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            { buildDirectory, new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(), new RepositoryTarget(Build.TARGET))},
            {immediateDependencyA, new BuildDirectory(immediateDependencyA, new Collection<IgnoreGlob>
            {
                IgnoreGlob.Construct(new Glob($"../{transitiveSubdirectory}/notImportantToA/**/*.cs"), immediateDependencyA,
                    new Collection<RepositoryTarget> { transitiveDependency })
            }, buildDirectory)},
            {immediateDependencyB, new BuildDirectory(immediateDependencyB, new Collection<IgnoreGlob>
            {
                IgnoreGlob.Construct(new Glob($"../{transitiveSubdirectory}/notImportantToB/**/*.cs"), immediateDependencyB,
                    new Collection<RepositoryTarget> { transitiveDependency })
            }, buildDirectory)},
            {transitiveDependency,new BuildDirectory(transitiveDependency, new Collection<IgnoreGlob>(), immediateDependencyA,immediateDependencyB)},
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.Yes>();
    }


    [Fact]
    public void
        Given_file_changed_in_dependency_with_multiple_parents_all_parents_ignore_file_When_Test_Then_no_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = new RepositoryTarget("src/builddir");
        RepositoryTarget immediateDependencyA = new RepositoryTarget("src/dependencyA");
        RepositoryTarget immediateDependencyB = new RepositoryTarget("src/dependencyB");
        var transitiveSubdirectory = "transitive";
        RepositoryTarget transitiveDependency = new RepositoryTarget( $"src/{transitiveSubdirectory}");
        ISet<string> changes = new HashSet<string> { $"{transitiveDependency.Directory}/notImportant/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            { buildDirectory, new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(), new RepositoryTarget(Build.TARGET))},
            {immediateDependencyA, new BuildDirectory(immediateDependencyA, new Collection<IgnoreGlob>
            {
                IgnoreGlob.Construct(new Glob($"../{transitiveSubdirectory}/notImportant/**/*.cs") , immediateDependencyA,
                    new Collection<RepositoryTarget> { transitiveDependency })
            }, buildDirectory)},
            {immediateDependencyB, new BuildDirectory(immediateDependencyB,new Collection<IgnoreGlob>
            {
                IgnoreGlob.Construct(new Glob( $"../{transitiveSubdirectory}/notImportant/**/*.cs"), immediateDependencyB,
                    new Collection<RepositoryTarget> { transitiveDependency })
            }, buildDirectory)},
            {transitiveDependency,new BuildDirectory(transitiveDependency, new Collection<IgnoreGlob>(), immediateDependencyA,immediateDependencyB)},
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().BeOfType<ShouldBuild.No>();
    }
    
}