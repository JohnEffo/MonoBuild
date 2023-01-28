using System.Text.RegularExpressions;

namespace MonoBuild.Core;

public class ProjDependencyExtractor : IDependencyExtractor
{
    private static readonly Regex Regex = new Regex(@"<ProjectReference Include=\""(?<Path>.*)\""");

    public ProjDependencyExtractor(
        string projectType)
    {
        SearchPattern = projectType;
    }

    public IEnumerable<string> GetDependencyFor(
        string dependencyBlob)
    {
        var matches = Regex.Matches(dependencyBlob);
        return matches
            .Select(m => m.Groups["Path"].Value)
            .Select(path=>path.Replace("\\","/"))
            .ToList();
    }

    public string SearchPattern { get; }
}