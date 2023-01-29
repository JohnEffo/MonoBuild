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
            
            .Select(line => new Dependancy(line) )
            .Select(dep => new DependencyLocation(dep.Path,dep.SelfParents));

    private record Dependancy
    {
        public bool SelfParents { get;  }

        public Dependancy(string line)
        {
            var lineParts = line.Split(":");
            if (lineParts.Length == 1)
            {
               Path = EnsureEndsWithForwardSlash(lineParts[0]);
               SelfParents = true;
            }
            else
            {
                Path = EnsureEndsWithForwardSlash(lineParts[1]);
                SelfParents = lineParts[0] switch
                {
                    "self" => true,
                    "parent" => false,
                    _ => throw new InvalidOperationException($"Valid  dependency line formats are: 'dependencyLocation'; 'self:dependencyLocation' for self parenting dependencies or 'parent:dependencyLocation' dependencies which use the current build directory as the parent '" ),
                };
            }
        }

        private string? EnsureEndsWithForwardSlash(
            string linePart)
        => linePart.EndsWith("/") ? linePart : linePart + "/";

        public string Path { get; }
}
     
    

    public string SearchPattern => ".monobuild.deps";
}