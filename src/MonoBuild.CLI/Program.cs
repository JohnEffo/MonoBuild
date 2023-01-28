using System.CommandLine;
using FluentResults;
using MonoBuild.Core;
using System.Text;

public class Program
{
    static async Task<int> Main(
        string[] args)
    {
        var repositoryOption = new Option<DirectoryInfo>(
            name: "--repository",
            () => new DirectoryInfo(Environment.CurrentDirectory),
            description:
            "The repository to process if no repository supplied the current directory is assumed to be the repository");
        repositoryOption.AddAlias("-r");

        var targetOption =
            new Option<string>("--target", "The target directory containing the project to be built");
        targetOption.IsRequired = true;
        targetOption.AddAlias("-t");

        var quietOption = new Option<YesNo>("--quiet",() => YesNo.No, 
            "Mono build will always write <YES> or <NO> to the terminal to indicate whether a build is required, if --quiet is set to Yes then debug information is also output if a build is required.");
        quietOption.IsRequired = false;
        quietOption.AddAlias("-q");

        var rootCommand =
            new RootCommand("Mono repo decide if a build needs to happen for a directory of a mono");
        rootCommand.AddOption(repositoryOption);
        rootCommand.AddOption(targetOption);
        rootCommand.AddOption(quietOption);

        rootCommand.SetHandler(async (
            repository,
            buildDirectory,
            quiet,
            fileLoader) =>
        {
            var buildsRequired = await GatherBuildInformationData(buildDirectory, repository, fileLoader);

            if (buildsRequired.IsFailed)
            {
                Console.WriteLine($"{buildsRequired.Errors.First()}{Environment.NewLine}" );
            }
            else
            {
                var changeInformation = buildsRequired.Value;
                var result = Build.IsRequired(changeInformation.Changes, changeInformation.Builds!) switch
                {
                     ShouldBuild.No _ => "<NO>",
                     ShouldBuild.Yes yes => BuildYes(yes.filesCausingBuild, quiet)
                };
                Console.WriteLine(result);
            }
        }, repositoryOption, targetOption,quietOption , new LoadDirectoryBinder());

        return await rootCommand.InvokeAsync(args);
    }

    private static string BuildYes(
        IEnumerable<string> filesCausingBuild,
        YesNo quiet)
    {
        var result = new StringBuilder();
        if (quiet == YesNo.No)
        {
            result.Append("The following files changed:").AppendLine();
            filesCausingBuild.Aggregate(result, (
                builder,
                change) => builder.Append(change).AppendLine());
        }
        return result.AppendLine("<YES>").ToString();
    }

    private static async Task<Result<BuildDecisionInformation>> GatherBuildInformationData(
        string buildDirectory,
        DirectoryInfo repository,
        ILoadBuildDirectory fileLoader)
    {
        var absoluteTarget = new AbsoluteTarget(buildDirectory, repository.FullName);
        var buildsRequired = await Changes
            .GetChanges(repository.FullName)
            .Bind(changes => new BuildDecisionInformation(changes).LoadBuilDetails(fileLoader, absoluteTarget));
        return buildsRequired;
    }
}