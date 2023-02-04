---
title: "As a Dotnet tool"
date: 2023-01-30T19:56:24Z
draft: false
weight: 5
---

The easiest way of getting set up is to install as a Dotnet tool. 

## Globally

To install the tool globally:

```shell
dotnet tool install -g monobuild
```

This allows you to use the tool on the command line by simply typing `monobuild`. More information on the installation and removal of Dotnet tools is available [here](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-use)


## Locally

A local installation limits the tool to a particular directory. 

* The advantage of this is that if the directory is a Git repository that means that the tool configuration will travel with the repository. Other members of the team (or the CI/CD process) can get access to all the tools installed in a repository by running `dotnet tool restore` more information on this is available [here](https://learn.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use).

* The disadvantage of this, is that you need to type `dotnet monobuild` to execute the tool.

If you have not installed any Dotnet tools into your current directory, you will need create a manifest with:

```shell
dotnet new tool-manifest
```

Once the manifest is in place, local installation is as simple as:

```shell
dotnet tool install monobuild
```

