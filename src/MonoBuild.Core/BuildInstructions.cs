using System.Collections.ObjectModel;

namespace MonoBuild.Core;

public record BuildDirectory(RepositoryTarget Directory,
    IEnumerable<IgnoreGlob> IgnoredGlobs,
params RepositoryTarget[] Parents );

public record DirectoryLoadResult(
    Collection<Glob> IgnoreGlobs,
    Collection<DependencyLocation> Targets);

public record DependencyLocation(
    string RepositoryLocation)
{
    public static implicit operator DependencyLocation(string location )=> new(location);
}


public record Glob(
    string Pattern)
{
    public static implicit operator Glob(string globPattern)=> new Glob(globPattern);
};

/// <summary>
/// The repository relative directory of the build
/// </summary>
/// <param name="Directory">The repository relative directory of the build</param>
public record RepositoryTarget(
    string Directory)
{
    public static implicit operator RepositoryTarget(
        string Directory) => new RepositoryTarget(Directory);

    public string GetRepositoryBasedNameFor(
        string path)
    {
        var readOnlySpan = Path.Combine(Directory, path).Replace("\\", "/");
        var dir = Path.GetDirectoryName(readOnlySpan).Replace("\\", "/");
        return new DirectoryInfo(dir).FullName.Replace("\\", "/").Replace(Environment.CurrentDirectory.Replace("\\", "/") + "/", "");
    }
}

/// <summary>
/// The absolute target directory
/// </summary>
/// <param name="BuildDirectory">The repository relative target directory</param>
/// <param name="Repository">The repository the build is located in</param>
public record AbsoluteTarget(
    RepositoryTarget BuildDirectory,
    GitRepository Repository)
{
    /// <summary>
    /// The absolute path we would need to give to the file system to access the directory
    /// </summary>
    public string AbsolutePath => Path.Combine(Repository.Directory, BuildDirectory.Directory);

    
}

/// <summary>
/// The repository the build is located in.
/// </summary>
/// <param name="Directory">The absolute directory of the repository</param>
public record GitRepository(
    string Directory)
{
    public static implicit operator GitRepository(
        string Directory) => new GitRepository(Directory);
}