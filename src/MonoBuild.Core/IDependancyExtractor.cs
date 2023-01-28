using System.Collections.ObjectModel;

namespace MonoBuild.Core;

public interface IDependencyExtractor
{
    IEnumerable<string> GetDependencyFor(
        string dependencyBlob);

    string SearchPattern { get; }
}