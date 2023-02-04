using System.Text;
using FluentResults;
using Microsoft.Extensions.FileSystemGlobbing;

namespace MonoBuild.Core;

public record ParentChild(
    RepositoryTarget Parent,
    RepositoryTarget Child)
{
    public ParentChild MakeChild(
        DependencyLocation child)
    {
        var childRepository = Child.GetRepositoryBasedNameFor(child.Path);
        return new(this.Child, new RepositoryTarget(childRepository));
    }
}

public class Build
{
    public const string TARGET = "</_target_/>";

    /// <summary>
    /// Load the build directory into the dictionary used to determine is the build is required
    /// </summary>
    /// <param name="buildDirectoryLoader">Used to access the file system</param>
    /// <param name="buildDirectory">The directory to load build information for</param>
    /// <returns></returns>
    public static async Task<Dictionary<RepositoryTarget, BuildDirectory>> LoadAsync(
        ILoadBuildDirectory buildDirectoryLoader,
        AbsoluteTarget buildDirectory)
    {
        var builderLoader = new BuildLoader(buildDirectory);
        while (builderLoader.BuildDirectoriesLeftToProcess(out var items))
        {
            var firstItem = builderLoader.RetreiveFirstItemPushingAllOthersBackOnStack(items);
            if (builderLoader.TargetSeenBefore(firstItem.Child))
            {
                if (items.Length == 1)
                {
                    builderLoader.DependancyFinished();
                }
                builderLoader.AddParentToChild(firstItem);
            }
            else
            {
                await builderLoader.LoadTargetDependancies(buildDirectoryLoader, firstItem);
            }
        }

        return builderLoader.Result.ToDictionary(r => r.Key, r => r.Value.ToBuildDirectory());
    }


    /// <summary>
    /// Is a build required for the current buildDirectories required.
    /// </summary>
    /// <param name="changes">A list of files changed as reported by Git</param>
    /// <param name="buildIDirectories">A dictionary where the key is the dependency directory and the object is the build directory information for that directory </param>
    /// <returns></returns>
    public static ShouldBuild IsRequired(
        ISet<string> changes,
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories)
    {
        var changesInADependantDirectory = RemoveAnyChangesNotInBuildDependancyList(changes,
            buildIDirectories.Select(kv => kv.Key.Directory).ToHashSet());
        if (!changesInADependantDirectory.Any())
        {
            return new ShouldBuild.No();
        }

        var changesNotRemovedByLocalExclusions =
            GetChangesNotRemovedByLocalExclusions(changesInADependantDirectory, buildIDirectories);
        if (!changesNotRemovedByLocalExclusions.Any())
        {
            return new ShouldBuild.No();
        }

        var targetBuildDirectory = buildIDirectories.First(bd => bd.Value.Parents.First().Directory == TARGET).Value;
        var notRemovedByTargetRelativeExclusions =
            GetChangesNotRemovedByBuildDirectoryRelativeExclusions(changesNotRemovedByLocalExclusions,
                buildIDirectories, targetBuildDirectory);
        if (!notRemovedByTargetRelativeExclusions.Any())
        {
            return new ShouldBuild.No();
        }

        var changesNotRemovedByTransitiveExclusions =
            RemoveChangesRemovedByEveryParentOfDependancy(notRemovedByTargetRelativeExclusions, buildIDirectories, targetBuildDirectory);
        if (!changesNotRemovedByTransitiveExclusions.Any())
        {
            return new ShouldBuild.No();
        }

        return new ShouldBuild.Yes(changesNotRemovedByTransitiveExclusions);
    }

    private static ISet<string> GetChangesNotRemovedByBuildDirectoryRelativeExclusions(
        ISet<string> remainingChanges,
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories,
        BuildDirectory targetBuildDirectory)
    {
 
        var immediatDependanciesOfBuild =
            buildIDirectories.Values.Where(bd => bd.Parents.Any(p => p == targetBuildDirectory.Directory));

        var filesExcluded = remainingChanges
            .Select(change => new {File = change, ExcludingPatterns = GetMatchingRelativeExlusions(change, targetBuildDirectory)})
            .Where(fileExludsionList => fileExludsionList.ExcludingPatterns.Any())
            .Where(fileExclusionList => ExcludedByClosestParent(immediatDependanciesOfBuild, fileExclusionList.ExcludingPatterns, fileExclusionList.File))
            .Select(fileExclusionList=>fileExclusionList.File)
            .ToHashSet();

        return remainingChanges.Where(file => !filesExcluded.Contains(file)).ToHashSet();
    }

    private static bool ExcludedByClosestParent(
        IEnumerable<BuildDirectory> dependencies,
        HashSet<IgnoreGlob.Relative> excludingPatterns,
        string fileName)
    {
        var longestExcludingPattern = excludingPatterns.OrderByDescending(p => p.Target.Directory.Length).First();
        var longestDependancyWhichStartFileName = dependencies
            .Where(d => fileName.StartsWith(d.Directory.Directory, StringComparison.OrdinalIgnoreCase))
            .MaxBy(d => d.Directory.Directory.Length)
            .Directory;
   
        return longestExcludingPattern.Target == longestDependancyWhichStartFileName;
    }


    /// <summary>
    /// If a file in a dependency has changed but it is ignored by every one of its immediate parents,
    /// then it should not trigger a build.
    /// </summary>
    /// <param name="changesNotRemovedByLocalExclusions">The remaining files which could trigger a build</param>
    /// <param name="buildIDirectories"></param>
    /// <param name="targetBuildDirectory"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static ISet<string> RemoveChangesRemovedByEveryParentOfDependancy(
        ISet<string> changesNotRemovedByLocalExclusions,
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories,
        BuildDirectory targetBuildDirectory)
    {
        return changesNotRemovedByLocalExclusions
            .Select(file => new
            {
                FileName = file,
                ContianingDirectories = GetBuildDirectoriesWhichContainFile(buildIDirectories, file)
            })
            .Where(fileBuildInfo =>
                !FileExcludedByAllImmediateParents(fileBuildInfo.FileName, fileBuildInfo.ContianingDirectories,
                    buildIDirectories, targetBuildDirectory.Directory))
            .Select(fileBuildInfo => fileBuildInfo.FileName).ToHashSet();
    }

    private static bool FileExcludedByAllImmediateParents(
        string fileName,
        IEnumerable<RepositoryTarget> containingDirectories,
        Dictionary<RepositoryTarget, BuildDirectory> buildDirectoryMap,
        RepositoryTarget targetBuildDirectory)
    {
        var parentWhichCouldHaveExlusions = containingDirectories
            .SelectMany(buidDir => buildDirectoryMap[buidDir].Parents.Where(p => p != targetBuildDirectory ))
            .Where(parent => parent.Directory != TARGET)
            .ToList();
        if (!parentWhichCouldHaveExlusions.Any())
        {
            return false;
        }

        return parentWhichCouldHaveExlusions.All(parent =>
            GetMatchingRelativeExlusions(fileName, buildDirectoryMap[parent]).Any());
    }

    private static HashSet<IgnoreGlob.Relative> GetMatchingRelativeExlusions(
        string fileName,
        BuildDirectory buildDirectory)
        => buildDirectory.IgnoredGlobs
                .OfType<IgnoreGlob.Relative>()
                .Where(glob => MatchesFile(glob.Glob, fileName))
                .ToHashSet();
                
  
    private static bool MatchesFile(
        Glob globGlob,
        string fileName)
    => new Matcher(StringComparison.OrdinalIgnoreCase)
        .AddInclude(globGlob.Pattern)
        .Match(fileName)
        .HasMatches;
    


    private static IEnumerable<RepositoryTarget> GetBuildDirectoriesWhichContainFile(
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories,
        string file)
    {
        return buildIDirectories.Keys
            .Where(buildDir => file.StartsWith(buildDir.Directory));
    }

    /// <summary>
    /// If a file type is removed by its build directory it is removed for all parents.
    /// </summary>
    /// <param name="changesInABuildDirectory">The changes which occur in build directories</param>
    /// <param name="buildIDirectories">The build directories</param>
    /// <returns></returns>
    private static ISet<string> GetChangesNotRemovedByLocalExclusions(
        ISet<string> changesInABuildDirectory,
        Dictionary<RepositoryTarget, BuildDirectory> buildIDirectories)
        => buildIDirectories
            .Values
            .Select(filterDirectory => new KeyValuePair<BuildDirectory, IEnumerable<string>>(filterDirectory,
                FilterChangesToThoseInDirectory(changesInABuildDirectory, filterDirectory)))
            .SelectMany(kv => GetFilesNotExcluded(kv.Key, kv.Value))
            .ToHashSet();


    private static IEnumerable<string> GetFilesNotExcluded(
        BuildDirectory buildDirectory,
        IEnumerable<string> files)
    {
        Matcher matcher = new();
        var excludePatternsGroups =
            buildDirectory.IgnoredGlobs.OfType<IgnoreGlob.Local>().Select(t =>buildDirectory.RoutedName(t.Glob.Pattern));
        matcher.AddIncludePatterns(excludePatternsGroups);
        var filesToRemove = matcher.Match(files).Files.Select(f => f.Path);
        return files.Where(file => !filesToRemove.Contains(file));
    }

    private static IEnumerable<string> FilterChangesToThoseInDirectory(
        ISet<string> changesInABuildDirectory,
        BuildDirectory dir)
    {
        return changesInABuildDirectory.Where(change => change.StartsWith(dir.Directory.Directory, StringComparison.InvariantCultureIgnoreCase));
    }

    private static ISet<string> RemoveAnyChangesNotInBuildDependancyList(
        ISet<string> changes,
        HashSet<string> buildDirectories)
        => changes
            .Where(change => !IsMonobuildfile(change))
            .Where(change => buildDirectories.Any(directory => change.StartsWith(directory, StringComparison.OrdinalIgnoreCase)))
            .ToHashSet();

    private static bool IsMonobuildfile(
        string change)
        => change.EndsWith(".monobuild.ignore", StringComparison.OrdinalIgnoreCase) || change.EndsWith(".monobuild.deps", StringComparison.OrdinalIgnoreCase);
}