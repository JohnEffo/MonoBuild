using System.Collections.ObjectModel;

namespace MonoBuild.Test.BuildTests;

public static class IgnoreGlobFactory
{
    public  static Collection<IgnoreGlob> IgnoreLocalFilesOfType(this RepositoryTarget buildDirectory,
        params string[] globPatterns)
    {
        var result = new Collection<IgnoreGlob>();
        return globPatterns.Aggregate(result, (
            globs,
            globPattern) =>
        {
            globs.Add(IgnoreGlob.Construct(globPattern, buildDirectory, new Collection<RepositoryTarget>()));
            return globs;
        });
    }
}