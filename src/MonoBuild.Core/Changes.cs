using System.Collections.ObjectModel;
using FluentResults;
using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;

namespace MonoBuild.Core;

public class Changes
{
    public static Result<ISet<string>> GetChanges(string repository)
    {
        try
        {
            var repo = new Repository(repository);
            var tip = repo.Head.Tip.Tree;
            var result = new HashSet<string>();
            foreach (var parent in repo.Head.Tip.Parents.Select(p => p.Tree))
            {
                var diff = repo.Diff.Compare<TreeChanges>(parent, tip);
                var modifications = new Collection<IEnumerable<TreeEntryChanges>>
                    { diff.Modified, diff.Added, diff.Deleted };
                modifications
                    .SelectMany(m => m.Select(p => p.Path))
                    .Aggregate(result, AddPath);
            }
            Matcher m = new Matcher();
        
            return result;
            
        }
        catch (Exception )
        {
            return Result.Fail($"No repository exists at {repository}");
        }
        
    }

    private static HashSet<string> AddPath(HashSet<string> set, string? path)
    {
        set.Add(path);
        return set;
    }
}



