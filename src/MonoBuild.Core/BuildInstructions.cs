using System.Collections.ObjectModel;

namespace MonoBuild.Core;

public record BuildDirectory(RepositoryTarget Directory,
    Collection<IgnoreGlob> IgnoredGlobs,
params RepositoryTarget[] Parents );

public record DirectoryLoadResult(
    Collection<Glob> IgnoredGlobs,
    Collection<RepositoryTarget> Targets);


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
}

/// <summary>
/// The absolute target directory
/// </summary>
/// <param name="RepositoryTarget">The repository relative target directory</param>
/// <param name="Repository">The repository the build is located in</param>
public record AbsoluteTarget(
    RepositoryTarget RepositoryTarget,
    GitRepository Repository)
{
    /// <summary>
    /// The absolute path we would need to give to the file system to access the directory
    /// </summary>
    public string AbsolutePath => Path.Combine(Repository.Directory, RepositoryTarget.Directory);
}

/// <summary>
/// The repository the build is located in.
/// </summary>
/// <param name="Directory">The absolute directory of the repository</param>
public record GitRepository(string Directory);