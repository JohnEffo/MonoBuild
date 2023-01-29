using System.Collections.ObjectModel;

namespace MonoBuild.Core;

public interface IDependencyExtractor
{
    IEnumerable<DependencyLocation> GetDependencyFor(
        string dependencyBlob);

    string SearchPattern { get; }
}