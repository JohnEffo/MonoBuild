using System.IO.Enumeration;
using Microsoft.Extensions.FileSystemGlobbing;

namespace MonoBuild.Core;

public class Build
{
    public const string TARGET = "TARGET";

    /// <summary>
    /// Is a build required for the current buildDirectories required.
    /// </summary>
    /// <param name="changes">I list for files changed as reported by Git</param>
    /// <param name="buildIDirectories">A dictionary where the Key is the parent and the item is one of its dependencies.
    /// The actual build directory will have a RepositoryTarget with a directory of TARGET</param>
    /// <returns></returns>
    public static ShouldBuild IsRequired(
        ISet<string> changes,
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories)
    {
        var changesInADependantDirectory =RemoveAnyChangesNotInBuildDependancyList(changes, buildIDirectories.Select(kv => kv.Key.Directory).ToHashSet());
        if (!changesInADependantDirectory.Any())
        {
            return ShouldBuild.No;
        }

        var changesNotRemovedByLocalExclusions =
            GetChangesNotRemovedByLocalExclusions(changesInADependantDirectory, buildIDirectories);
        if (!changesNotRemovedByLocalExclusions.Any())
        {
            return ShouldBuild.No;
        }

        var changesNotRemovedByTransitiveExclusions =
            RemoveChangesRemovedByEveryParentOfDependancy(changesNotRemovedByLocalExclusions, buildIDirectories);
        if (!changesNotRemovedByTransitiveExclusions.Any())
        {
            return ShouldBuild.No;
        }
        return ShouldBuild.Yes;
    }

    /// <summary>
    /// If a file in a dependency has changed but it is ignored by every one of its immediate parents,
    /// then it should not trigger a build.
    /// </summary>
    /// <param name="changesNotRemovedByLocalExclusions">The remaining files which could trigger a build</param>
    /// <param name="buildIDirectories"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static ISet<string> RemoveChangesRemovedByEveryParentOfDependancy(
        ISet<string> changesNotRemovedByLocalExclusions,
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories)
    {
        return changesNotRemovedByLocalExclusions
            .Select(file => new
            {
                FileName = file,
                BuildDirectories = GetBuildDirectoriesWhichContainFile(buildIDirectories, file)
            })
            .Where(fileBuildInfo =>
                !FileExcludedByAllImmediateParents(fileBuildInfo.FileName, fileBuildInfo.BuildDirectories, buildIDirectories))
            .Select(fileBuildInfo => fileBuildInfo.FileName).ToHashSet();
        
    }

    private static bool FileExcludedByAllImmediateParents(
        string fileName,
        IEnumerable<RepositoryTarget> buildDirectories,
        Dictionary<RepositoryTarget, BuildDirectory> buildDirectoryMap)
    {
        var parentWhichCouldHaveExlusions = buildDirectories
            .SelectMany(buidDir => buildDirectoryMap[buidDir].Parents)
            .Where(parent => parent != TARGET)
            .ToList();
        if (!parentWhichCouldHaveExlusions.Any())
        {
            return false;
        }
        return  parentWhichCouldHaveExlusions.All(parent => FileExcludedByParent(fileName, buildDirectoryMap[parent]));
    }
    private static bool FileExcludedByParent(string fileName,
        BuildDirectory buildDirectory)
    {
        Matcher matcher = new();
        var excludePatternsGroups= buildDirectory.IgnoredGlobs.OfType<IgnoreGlob.Relative>().Select(glob => glob.Glob.Pattern);
       
        matcher.AddIncludePatterns(excludePatternsGroups);
        return matcher.Match(fileName).Files.Any();
    }

    private static IEnumerable<RepositoryTarget> GetBuildDirectoriesWhichContainFile(
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories,
        string file)
    {
        return buildIDirectories.Keys.Where(buidlDir => file.StartsWith(buidlDir.Directory));
    }

    /// <summary>
    /// If a file type is removed by its build directory it is removed for all parents.
    /// </summary>
    /// <param name="changesInABuildDirectory">The changes which occur in build directories</param>
    /// <param name="buildIDirectories">The build directories</param>
    /// <returns></returns>
    private static ISet<string>  GetChangesNotRemovedByLocalExclusions(
        ISet<string> changesInABuildDirectory,
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories)
        => buildIDirectories
            .Values
            .Select(filterDirectory => new KeyValuePair<BuildDirectory, IEnumerable<string>> (filterDirectory, FilterChangesToThoseInDirectory(changesInABuildDirectory, filterDirectory)))
            .SelectMany(kv => GetFilesNotExcluded(kv.Key, kv.Value))
            .ToHashSet();
    

    private static IEnumerable<string> GetFilesNotExcluded(BuildDirectory buildDirectory, IEnumerable<string> files)
    {
        Matcher matcher = new();
        var excludePatternsGroups = buildDirectory.IgnoredGlobs.OfType<IgnoreGlob.Local>().Select(t => "**\\" + t.Glob.Pattern);
        matcher.AddIncludePatterns(excludePatternsGroups);
        var filesToRemove = matcher.Match(files).Files.Select(f => f.Path);
        return files.Where(file => !filesToRemove.Contains(file));
    }

    private static IEnumerable<string> FilterChangesToThoseInDirectory(ISet<string> changesInABuildDirectory, BuildDirectory dir)
    {
        return changesInABuildDirectory.Where(change => change.StartsWith(dir.Directory.Directory));
    }

    private static ISet<string> RemoveAnyChangesNotInBuildDependancyList(
        ISet<string> changes,
        HashSet<string> buildDirectories)
        => changes.Where(change => buildDirectories.Any(directory => change.StartsWith(directory))).ToHashSet();
}