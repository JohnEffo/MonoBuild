+++
title = "How It Works"
date = 2023-01-30T22:05:33Z
weight = 5
chapter = false
pre = "<b>1.1. </b>"
draft = true
+++

Monobuild decides On whether a build is required, by looking at the files changed in the last commit. It decides if a changed file should cause a build, buy building a list of dependent directories of the build directory.

## Information Retrieval

So at the start of the process two sets of information are gathered:

##### 1 Files changes: 
The list of files considered for a given commit can be seen by 
```
git diff --name-only head head~1
```

##### 2 Dependent directories 
  * The project file, ```.monobuild.deps``` and  ```.monobuild.ingore``` files read, for the build directory. 
  * For each dependant directory found in a project or deps file the the process of loading dependencies and exclusions is repeated until a complete tree of all the build dependencies is created. 

#### Custom Dependencies

The directory of any csproj/fsproj file is considered a dependant directory, but if you want a build to be triggered by a transitive dependency which is not described by the project files, you can add manual dependency files. 

#### Ignore Files

We don't necessarily want a build to be triggered for a change in every file type. We can ignore files using a file pattern. Ignores can be relative for files in another directory, or local ignores to the current directory.

## Dependency Resolution

Files are removed from the list of changed files if they do not match any dependency, if any files are left in the list then a build is required. Files are removed by the following process:
1. Any file not directly in a build directory.
1. Any file matched by a local exclusion. Every directory in the build tree may have its own exclusion file.
1. Any in the file excluded by the build directories relative exclusions.
1. Any file where in a directory where all of its parents have ignored it.


Step 1 is easy to understand if we are building Service A and all the changes happen in Service B which is not a dependency of of Service A then all the files can be removed from the list and no build is required.

