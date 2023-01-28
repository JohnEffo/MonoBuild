using System.Collections.ObjectModel;
using System.CommandLine.Binding;
using System.IO.Abstractions;
using MonoBuild.Core;

public class LoadDirectoryBinder : BinderBase<ILoadBuildDirectory>
{
    protected override ILoadBuildDirectory GetBoundValue(
        BindingContext bindingContext)
    {
        Collection<IDependencyExtractor> extractores = new Collection<IDependencyExtractor>
        {
            new ProjDependencyExtractor("*.csproj"), new ProjDependencyExtractor("*.fsproj"), new DepsFileExtractor()
        };
        return new LoadBuildDirectory(extractores, new ProjDependencyExtractor(string.Empty), new FileSystem());
    }
}