namespace MonoBuild.Core;

public interface ILoadBuildDirectory
{
    Task<DirectoryLoadResult> Load(
        AbsoluteTarget buildDirectory);

    
}