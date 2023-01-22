using System.Collections.ObjectModel;
using System.Text;

namespace MonoBuild.Core;

public abstract record IgnoreGlob(Glob Glob)
{
    public static IgnoreGlob Construct(
        string globPattern,RepositoryTarget currentBuild, Collection<RepositoryTarget> dependancies)
    {
        if (globPattern.StartsWith(".."))
        {
            return BuiltRelativeBlob(globPattern, currentBuild, dependancies);
        }

        return new Local(globPattern);
    }

    private static IgnoreGlob BuiltRelativeBlob(
        string globPattern,
        RepositoryTarget currentBuild,
        Collection<RepositoryTarget> dependancies)
    {
        //Ensure relative globs have / not \ because the globing matcher produces matches with / as the path separator
        var dir = Path.GetDirectoryName(Path.Combine(currentBuild.Directory, globPattern))?.Replace("\\","/");
        var dependencyPattern = new DirectoryInfo(dir).FullName.Replace("\\", "/").Replace(Environment.CurrentDirectory.Replace("\\", "/") + "/", "");
        var matchingDependency = dependancies.FirstOrDefault(dep => dependencyPattern.StartsWith(dep.Directory));
        if (matchingDependency == null)
        {
            throw new ArgumentException(
                $"Cannot create an ignore for {globPattern} as it is not a member of any provided dependency",
                nameof(globPattern));
        }

        return new Relative(ConstructDependancyGlobPattern(dependencyPattern, globPattern), matchingDependency);
    }

    private static Glob ConstructDependancyGlobPattern(
        string dependencyPattern,
        string globPattern)
    {
        var lastDir = dependencyPattern.Split("/").Last();
        return new StringBuilder(dependencyPattern)
            .Append(globPattern.Split(lastDir).LastOrDefault() ?? "").ToString();
    }

    public record Local(
        Glob Glob) : IgnoreGlob(Glob);

    public record Relative(Glob Glob,RepositoryTarget Target):IgnoreGlob(Glob);

}