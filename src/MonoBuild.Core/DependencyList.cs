using System.Text;

namespace MonoBuild.Core;

public class DependencyList
{
  
    private Stack<ParentChild> path = new Stack<ParentChild>();
    

    public void Add(ParentChild parentChild)
    {
        if (path.TryPeek(out var head) && head.Parent == parentChild.Parent)
        {
            path.Pop();
        }

        path.Push(parentChild);
        if (path.GroupBy(p => p.Child).Any(grp => grp.Count() > 1))
        {
            throw new InvalidOperationException($"Circular dependency detected: {UnwindPath()}");
        }
     
    }

    private string UnwindPath()
    {
        var pathstring = new StringBuilder().Append("Path:").AppendLine();
        while (path.TryPop(out var item))
        {
            pathstring.Append(item.Child.Directory).Append("<=").AppendLine();
        }
        return pathstring.ToString();
    }

    public void DependancyFinished()
    {
        path.Pop();
    }
}