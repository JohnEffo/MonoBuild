using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace MonoBuild.Core;

internal class BuildLoader
{
    private  Stack<ParentChild[]?> DependancyStack { get; }
    public DependencyList Path { get; }
    public Dictionary<RepositoryTarget, BuildDirectoryConstruction> Result { get; }
    private readonly AbsoluteTarget _buildFor;
    public BuildLoader(AbsoluteTarget target
    )
    {
        DependancyStack = new Stack<ParentChild[]?>();
        Path = new DependencyList();
        Result = new Dictionary<RepositoryTarget, BuildDirectoryConstruction>();
        _buildFor = target;
        DependancyStack.Push(new ParentChild[] { new ParentChild(Build.TARGET, target.BuildDirectory) });
    }

    public bool TargetSeenBefore(
        RepositoryTarget targetDir)
        => Result.ContainsKey(targetDir);

    public void AddParentToChild(
        ParentChild parentChild)
    {
        var buildPage = Result[parentChild.Child];
        buildPage.AddParent(parentChild.Parent);
    }

    public bool BuildDirectoriesLeftToProcess([MaybeNullWhen(false)] out ParentChild[] items)
        => DependancyStack.TryPop(out items);

    public async Task LoadTargetDependancies(
        ILoadBuildDirectory buildDirectoryLoader,
        ParentChild item
    )
    {
        var currentBuild = await buildDirectoryLoader.Load(_buildFor with { BuildDirectory = item.Child });
        var children = GetRepositoryLocations(currentBuild.Targets, item.Child);
        var parentChildren = children.Select(c => item.MakeChild(c.Directory)).ToArray();
        if (parentChildren.Any())
        {
            DependancyStack.Push(parentChildren);
        }
        else
        {
            //No children to load so not going further down this path
            Path.DependancyFinished();
        }
        Result.Add(item.Child, new BuildDirectoryConstruction(currentBuild.IgnoreGlobs, children, item));
    }

    private static List<RepositoryTarget> GetRepositoryLocations(
        Collection<DependencyLocation> targets,
        RepositoryTarget parent)
        => targets
            .Select(target => new RepositoryTarget(parent.GetRepositoryBasedNameFor(target.RepositoryLocation)))
            .ToList();

    public ParentChild RetreiveFirstItemPushingAllOthersBackOnStack(ParentChild[] items)
    {
        var (item, tail) = (items[0], items[1..]);
        Path.Add(item);
        if (tail.Any())
        {
            DependancyStack.Push(tail);
        }
        return item;
    }
}