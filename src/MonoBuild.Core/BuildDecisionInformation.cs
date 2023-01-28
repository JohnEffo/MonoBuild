using FluentResults;

namespace MonoBuild.Core;

public class BuildDecisionInformation
{
    public ISet<string> Changes { get; }
    public Dictionary<RepositoryTarget, BuildDirectory> Builds { get; private set; }

    public BuildDecisionInformation(
        ISet<string> changes)
    {
        Changes = changes;
    }

    public async Task<Result<BuildDecisionInformation>> LoadBuilDetails(
        ILoadBuildDirectory loader,
        AbsoluteTarget target)
    {
        try
        {
            Builds = await Build.LoadAsync(loader, target);
            return this;
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error(ex.Message));
        }
    }
}