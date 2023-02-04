using System.Collections.ObjectModel;
using System.Text;

namespace MonoBuild.Core;

public abstract record IgnoreGlob(Glob Glob)
{
    public static IgnoreGlob Construct(
        Glob glob,RepositoryTarget currentBuild, Collection<RepositoryTarget> dependancies)
    {
        if (glob.Pattern.StartsWith(".."))
        {
            return BuiltRelativeBlob(glob, currentBuild, dependancies);
        }

        return new Local(glob);
    }

    private static IgnoreGlob BuiltRelativeBlob(
        Glob glob,
        RepositoryTarget currentBuild,
        Collection<RepositoryTarget> dependencies)
    {
        //Ensure relative globs have / not \; because the globing matcher produces matches with / as the path separator
        var dir = Path.GetDirectoryName(Path.Combine(currentBuild.Directory, glob.Pattern))?.Replace("\\", "/");
        var dependencyPattern = new DirectoryInfo(dir).FullName.Replace("\\", "/").Replace(Environment.CurrentDirectory.Replace("\\", "/") + "/", "");
        var matchingDependency = dependencies
            .Where(dep => dependencyPattern.StartsWith(dep.Directory))
            .MaxBy(dep => dep.Directory.Length);
        if (matchingDependency == null)
        {
            throw new ArgumentException(
                $"Cannot create an ignore for {glob.Pattern} as it is not a member of any provided dependency",
                nameof(glob));
        }

        return new Relative(ConstructDependancyGlobPattern(dependencyPattern, glob), matchingDependency);
    }

    private static Glob ConstructDependancyGlobPattern(
        string dependencyPattern,
        Glob glob)
    {
        var lastDir = dependencyPattern.Split("/").Last();
        return new Glob( new StringBuilder(dependencyPattern)
            .Append(glob.Pattern.Split(lastDir).LastOrDefault() ?? "").ToString());
    }

    public record Local(
        Glob Glob) : IgnoreGlob(Glob);

    public record Relative(Glob Glob,RepositoryTarget Target):IgnoreGlob(Glob);

}