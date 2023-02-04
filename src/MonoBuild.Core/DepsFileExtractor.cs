using System.Linq.Expressions;
using System.Net.Sockets;

namespace MonoBuild.Core;

public static class MonoBuildFileLines
{
    public static IEnumerable<string> Process(
        string fileBlob)
        => fileBlob.Split("\r")
            .Select(line => line.Trim())
            .Where(line => !line.StartsWith("#"))
            .Where(line => line.Length > 0)
            .Select(line => line.Replace("\\", "/"));
}


public class DepsFileExtractor:IDependencyExtractor
{
    public IEnumerable<DependencyLocation> GetDependencyFor(
        string dependencyBlob)
        => MonoBuildFileLines.Process(dependencyBlob)
            .Select(EnsureEndsWithForwardSlash)
            .Select(line => new DependencyLocation(line));
    public string SearchPattern => ".monobuild.deps";

    private string? EnsureEndsWithForwardSlash(
        string linePart)
        => linePart.EndsWith("/") ? linePart : linePart + "/";

    public string Path { get; }
}