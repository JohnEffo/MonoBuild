using System.Collections.ObjectModel;
using System.IO.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;

namespace MonoBuild.Core;

public class LoadBuildDirectory:ILoadBuildDirectory
{
    private readonly Dictionary<Glob, IDependencyExtractor> _extractors;
    private readonly IFileSystem _fileSystem;
    private const string Ingore = ".monobuild.ignore";

    public LoadBuildDirectory(Dictionary<Glob,IDependencyExtractor> extractors,IFileSystem fileSystem)
    {
        _extractors = extractors;
        _fileSystem = fileSystem;
    }
    public DirectoryLoadResult Load(
        AbsoluteTarget buildDirectory)
    { 
        AssertPathExists(buildDirectory);
        Collection<DependancyLocation> targets = RetrieveTargets(buildDirectory);
        Collection<Glob> ignores = RetrieveIgnores(buildDirectory,targets);
        return new DirectoryLoadResult(ignores, targets);
    }

    private void AssertPathExists(
        AbsoluteTarget path)
    {
        
        if (!Path.Exists(path.AbsolutePath))
        {
            throw new ArgumentException($"The path {path.BuildDirectory} does not exist");
        }
    }

    private Collection<DependancyLocation> RetrieveTargets(
        AbsoluteTarget path)
    {
        //_fileSystem.Directory.GetFiles();
        Matcher matcher = new Matcher();
      throw new NotImplementedException();
    }

    private Collection<Glob> RetrieveIgnores(
        AbsoluteTarget path,
        Collection<DependancyLocation> repositoryTargets)
    {
        throw new NotImplementedException();
    }
}