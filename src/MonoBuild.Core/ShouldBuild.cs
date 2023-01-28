namespace MonoBuild.Core;

public abstract record ShouldBuild
{
    public record No() : ShouldBuild;

    public record Yes(
        IEnumerable<string> filesCausingBuild):ShouldBuild;
    
}
