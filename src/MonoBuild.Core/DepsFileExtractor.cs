namespace MonoBuild.Core;

public class DepsFileExtractor:IDependencyExtractor
{
    public IEnumerable<string> GetDependencyFor(
        string dependencyBlob)
        => dependencyBlob.Split("\r")
            .Select(line => line.Trim())
            .Where(line => !line.StartsWith("#"))
            .Where(line => line.Length > 0)
            .Select(line => line.Replace("\\", "/"));

      

    public string SearchPattern => ".monobuild.deps";
}