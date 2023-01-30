---
title: "Build From Source"
date: 2023-01-30T21:37:54Z
draft: false
weight: 6
---
### Prerequisites
You will need to have Dotnet 7 installed for instructions on how to do this: https://dotnet.microsoft.com/en-us/download


### Building 

* Clone the repository
```shell
git clone https://github.com/JohnEffo/MonoBuild
cd monobuild
```

* Build the exe
```shell
dotnet build
```

The build artifacts will located at ./monobuild/src/bin/debug/net7.0. You may want to copy them from here and place them into a directory in your path environment variable.