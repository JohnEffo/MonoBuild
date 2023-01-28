using System.Collections.ObjectModel;
using System.IO.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;

namespace MonoBuild.Core;

public class LoadBuildDirectory:ILoadBuildDirectory
{
    private readonly Collection<IDependencyExtractor> _extractors;
    private readonly IDependencyExtractor _ignoreExtractor;
    private readonly IFileSystem _fileSystem;
    private const string Ignore = ".monobuild.ignore";

    public LoadBuildDirectory(Collection<IDependencyExtractor> extractors, IDependencyExtractor ignoreExtractor,IFileSystem fileSystem)
    {
        _extractors = extractors;
        _ignoreExtractor = ignoreExtractor;
        _fileSystem = fileSystem;
    }
    public async Task<DirectoryLoadResult> Load(
        AbsoluteTarget buildDirectory)
    { 
        AssertPathExists(buildDirectory);
        Collection<DependencyLocation> targets = await RetrieveTargets(buildDirectory);
        Collection<Glob> ignores = await RetrieveIgnores(buildDirectory);
        return new DirectoryLoadResult(ignores, targets);
    }

    private void AssertPathExists(
        AbsoluteTarget path)
    {
        
        if (!_fileSystem.Path.Exists(path.AbsolutePath))
        {
            throw new ArgumentException($"The path {path.BuildDirectory} does not exist");
        }
    }

    private async Task<Collection<DependencyLocation>> RetrieveTargets(
        AbsoluteTarget path)
    {
        var directory = path.AbsolutePath;
        var result = new Collection<DependencyLocation>();
        foreach (var dependencyExtractor in _extractors)
        {
            var dependencySourceFiles=_fileSystem.Directory.GetFiles(directory, dependencyExtractor.SearchPattern);
            foreach (var dependencySourceFile in dependencySourceFiles)
            {
                var fileContent = await _fileSystem.File.ReadAllTextAsync(dependencySourceFile);
                var dependencies = dependencyExtractor.GetDependencyFor(fileContent)
                    .Select(file => new DependencyLocation(file));
                dependencies.Aggregate(result, AddItem);
            }
        }
        return result;
    }

    private Collection<T> AddItem<T>(
        Collection<T> locations,
        T dependencyLocation)
    {
        locations.Add(dependencyLocation);
        return locations;
    }

    private async Task<Collection<Glob>> RetrieveIgnores(
        AbsoluteTarget path)
    {
        var filePath  = $"{path.AbsolutePath}/{Ignore}";
        if (_fileSystem.File.Exists(filePath))
        {
            var fileContents = await _fileSystem.File.ReadAllTextAsync(filePath);
            var ignores = _ignoreExtractor.GetDependencyFor(fileContents).Select(ignore => new Glob(ignore));
            return ignores.Aggregate(new Collection<Glob>(), AddItem);
        }
        return new Collection<Glob>();

    }
}