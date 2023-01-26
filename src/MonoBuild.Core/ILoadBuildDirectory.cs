namespace MonoBuild.Core;

public interface ILoadBuildDirectory
{
    DirectoryLoadResult Load(
        AbsoluteTarget buildDirectory);

    
}