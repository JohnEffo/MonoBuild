﻿using System.Collections.ObjectModel;
using FluentAssertions.Execution;

namespace MonoBuild.Test.Unit.BuildTests;

public class IsRequired
{
    [Fact]
    public void Given_no_file_in_build_directory_changed_When_Test_Then_no_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = "src/builddir";
        ISet<string> changes = new HashSet<string> { "src/nonBuildDire/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory,new Collection<IgnoreGlob>(),Build.TARGET)}
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().Be(ShouldBuild.No);
    }

    [Fact]
    public void Given_file_changed_in_build_directory_And_file_type_not_ignored_When_Test_Then_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = "src/builddir";
        ISet<string> changes = new HashSet<string> { $"{buildDirectory.Directory}/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(),Build.TARGET)}
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().Be(ShouldBuild.Yes);
    }

    [Fact]
    public void Given_files_changed_in_build_directory_And_not_all_files_ignored_When_Test_Then_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = "src/builddir";
        ISet<string> changes = new HashSet<string> { $"{buildDirectory.Directory}/somefile.cs", $"{buildDirectory.Directory}/somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory,buildDirectory.IgnoreLocalFilesOfType("*.md"),Build.TARGET)}
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().Be(ShouldBuild.Yes);
    }

    [Fact]
    public void Given_file_changed_in_dependency_of_build_directory_And_file_type_not_ignored_When_Test_Then_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = "src/builddir";
        RepositoryTarget dependencyDirectory = "src/dependency";
        ISet<string> changes = new HashSet<string> { $"{dependencyDirectory.Directory}/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(),Build.TARGET)},
            {dependencyDirectory,new BuildDirectory(dependencyDirectory,new Collection<IgnoreGlob>(), buildDirectory) }
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().Be(ShouldBuild.Yes);
    }

    [Fact]
    public void Given_file_changed_in_build_directory_And_file_type_is_ignored_When_Test_Then_no_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = "src/builddir";
        ISet<string> changes = new HashSet<string> { $"{buildDirectory.Directory}somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory, buildDirectory.IgnoreLocalFilesOfType("*.md"), Build.TARGET) }
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().Be(ShouldBuild.No);
    }

    [Fact]
    public void Given_file_changed_in_dependency_of_build_directory_And_file_type_ignored_locally_by_dependent_directory_When_Test_Then_no_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = "src/builddir";
        RepositoryTarget dependencyDirectory = "src/dependency";
        ISet<string> changes = new HashSet<string> { $"{dependencyDirectory.Directory}/somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {buildDirectory,new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(),Build.TARGET)},
            {dependencyDirectory,new BuildDirectory(dependencyDirectory,dependencyDirectory.IgnoreLocalFilesOfType("*.md"), buildDirectory) }
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().Be(ShouldBuild.No);
    }

    [Fact]
    public void Given_file_changed_in_dependency_of_build_directory_And_file_type_ignored_using_relative_glob_in_build_directory_When_Test_Then_no_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = "src/builddir";
        var dependencySubdirectory = "dependency";
        RepositoryTarget dependencyDirectory = $"src/{dependencySubdirectory}";
        ISet<string> changes = new HashSet<string> { $"{dependencyDirectory.Directory}/somefile.md" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            {
                buildDirectory, new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>
                {
                    IgnoreGlob.Construct($"../{dependencySubdirectory}/**/*.md", buildDirectory,
                        new Collection<RepositoryTarget> { dependencyDirectory })
                }, Build.TARGET)
            },
            { dependencyDirectory, new BuildDirectory(dependencyDirectory,new Collection<IgnoreGlob>(), buildDirectory) }
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().Be(ShouldBuild.No);
    }

    [Fact]
    public void
        Given_file_changed_in_dependency_with_multiple_parents_And_not_all_parents_ignore_file_When_Test_Then_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = "src/builddir";
        RepositoryTarget immediateDependencyA = "src/dependencyA";
        RepositoryTarget immediateDependencyB = "src/dependencyB";
        string transitiveSubdirectory = "transitive";
        RepositoryTarget transitiveDependency = $"src/{transitiveSubdirectory}";
        ISet<string> changes = new HashSet<string> { $"{transitiveDependency.Directory}/notImportantToA/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            { buildDirectory, new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(), Build.TARGET)},
            {immediateDependencyA, new BuildDirectory(immediateDependencyA, new Collection<IgnoreGlob>
            {
                IgnoreGlob.Construct($"../{transitiveSubdirectory}/notImportantToA/**/*.cs", immediateDependencyA,
                    new Collection<RepositoryTarget> { transitiveDependency })
            }, buildDirectory)},
            {immediateDependencyB, new BuildDirectory(immediateDependencyB, new Collection<IgnoreGlob>(), buildDirectory)},
            {transitiveDependency,new BuildDirectory(transitiveDependency, new Collection<IgnoreGlob>(), immediateDependencyA,immediateDependencyB)},
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().Be(ShouldBuild.Yes);
    }

    [Fact]
    public void
        Given_file_changed_in_dependency_with_parents_AandB_And_A_ignores_directory_And_B_ignores_different_directory_When_Test_Then_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = "src/builddir";
        RepositoryTarget immediateDependencyA = "src/dependencyA";
        RepositoryTarget immediateDependencyB = "src/dependencyB";
        var transitiveSubdirectory = "transitive";
        RepositoryTarget transitiveDependency = $"src/{transitiveSubdirectory}";
        ISet<string> changes = new HashSet<string> { $"{transitiveDependency.Directory}/notImportantToA/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            { buildDirectory, new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(), Build.TARGET)},
            {immediateDependencyA, new BuildDirectory(immediateDependencyA, new Collection<IgnoreGlob>
            {
                IgnoreGlob.Construct($"../{transitiveSubdirectory}/notImportantToA/**/*.cs", immediateDependencyA,
                    new Collection<RepositoryTarget> { transitiveDependency })
            }, buildDirectory)},
            {immediateDependencyB, new BuildDirectory(immediateDependencyB, new Collection<IgnoreGlob>
            {
                IgnoreGlob.Construct($"../{transitiveSubdirectory}/notImportantToB/**/*.cs", immediateDependencyB,
                    new Collection<RepositoryTarget> { transitiveDependency })
            }, buildDirectory)},
            {transitiveDependency,new BuildDirectory(transitiveDependency, new Collection<IgnoreGlob>(), immediateDependencyA,immediateDependencyB)},
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().Be(ShouldBuild.Yes);
    }


    [Fact]
    public void
        Given_file_changed_in_dependency_with_multiple_parents_all_parents_ignore_file_When_Test_Then_no_build_required()
    {
        //Given
        RepositoryTarget buildDirectory = "src/builddir";
        RepositoryTarget immediateDependencyA = "src/dependencyA";
        RepositoryTarget immediateDependencyB = "src/dependencyB";
        var transitiveSubdirectory = "transitive";
        RepositoryTarget transitiveDependency = $"src/{transitiveSubdirectory}";
        ISet<string> changes = new HashSet<string> { $"{transitiveDependency.Directory}/notImportant/somefile.cs" };
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories = new Dictionary<RepositoryTarget, BuildDirectory>
        {
            { buildDirectory, new BuildDirectory(buildDirectory, new Collection<IgnoreGlob>(), Build.TARGET)},
            {immediateDependencyA, new BuildDirectory(immediateDependencyA, new Collection<IgnoreGlob>
            {
                IgnoreGlob.Construct($"../{transitiveSubdirectory}/notImportant/**/*.cs", immediateDependencyA,
                    new Collection<RepositoryTarget> { transitiveDependency })
            }, buildDirectory)},
            {immediateDependencyB, new BuildDirectory(immediateDependencyB,new Collection<IgnoreGlob>
            {
                IgnoreGlob.Construct($"../{transitiveSubdirectory}/notImportant/**/*.cs", immediateDependencyB,
                    new Collection<RepositoryTarget> { transitiveDependency })
            }, buildDirectory)},
            {transitiveDependency,new BuildDirectory(transitiveDependency, new Collection<IgnoreGlob>(), immediateDependencyA,immediateDependencyB)},
        };

        //When
        var build = Build.IsRequired(changes, buildIDirectories);

        //When
        build.Should().Be(ShouldBuild.No);
    }
    
}

public static class IgnoreGlobFactory
{
    public  static Collection<IgnoreGlob> IgnoreLocalFilesOfType(this RepositoryTarget buildDirectory,
        params string[] globPatterns)
    {
        var result = new Collection<IgnoreGlob>();
        return globPatterns.Aggregate(result, (
            globs,
            globPattern) =>
        {
            globs.Add(IgnoreGlob.Construct(globPattern, buildDirectory, new Collection<RepositoryTarget>()));
            return globs;
        });
    }
}