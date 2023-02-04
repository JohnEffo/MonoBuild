using System.Collections.ObjectModel;
using System.Text;

namespace MonoBuild.Core;

public class BuildDirectoryConstruction
{
    private readonly Collection<Glob> _ignoreGlobs;
    private readonly HashSet<RepositoryTarget> _parents;
    private readonly RepositoryTarget _target;
    private readonly Collection<RepositoryTarget> _children;

    public BuildDirectoryConstruction(
        Collection<Glob> ignoreGlobs,
        List<RepositoryTarget> children,
        ParentChild parentChild)
    {
        _target = parentChild.Child;
        _parents = new HashSet<RepositoryTarget> { parentChild.Parent };
        _children = new Collection<RepositoryTarget>(children);
        _ignoreGlobs = ignoreGlobs;
    }

    

    public void AddParent(
        RepositoryTarget parent)
    {
        var lengthAtStart = _parents.Count;
        _parents.Add(parent);
        if (_parents.Count ==lengthAtStart)
        {
            throw new InvalidOperationException(
                $"Circular route detected when adding dependency {parent} it has already been seen: {Environment.NewLine} {OutputRoute()}");
        }
    }

    public BuildDirectory ToBuildDirectory()
    {
        var ignoreBlobs = new Collection<IgnoreGlob>(_ignoreGlobs.Select(blob => IgnoreGlob.Construct(blob, _target  , _children)).ToArray());
        return new BuildDirectory(_target, ignoreBlobs, _parents.ToArray());
    }

    private string OutputRoute()
    {
        var builder = new StringBuilder();
        foreach (RepositoryTarget target in _parents)
        {
            builder.Append(target.Directory).Append(": =>").Append("\n");
        }
        return builder.ToString();
    }
}