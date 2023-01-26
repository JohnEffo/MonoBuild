// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using FluentResults;
using MonoBuild.Core;
using FluentResults.Extensions;

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
            new Option<DirectoryInfo>("--target", "The target directory containing the project to be built");
        targetOption.IsRequired = true;
        targetOption.AddAlias("-t");


        var rootCommand =
            new RootCommand("Mono repo decide if a build needs to happen for a directory of a mono");
        rootCommand.AddOption(repositoryOption);
        rootCommand.AddOption(targetOption);

        rootCommand.SetHandler((
            repository,
            file) =>
        {
            Console.WriteLine($"Execution repository is {repository}");
            var buildsRequired = Changes
                .GetChanges(repository.FullName)
                .Bind(changes =>  Result.Ok() );
            if (buildsRequired.IsFailed)
            {
                Console.WriteLine($"Failed: {buildsRequired.Errors.First().Message}");

            }

        }, repositoryOption, targetOption);

        return await rootCommand.InvokeAsync(args);
    }

}

//Load Depenancfcies for build dir
//Load Ignoresfor build file
//ForEach Dependancy in dependancie
   //LoadDependancies and build



