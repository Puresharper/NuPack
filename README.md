# NuPack

NuPack is an easy way to produce a nuget package for .NET 4.0+ based on AssemblyInfo or nuspec when building with Visual Studio. It is materialized as a nuget package.

https://www.nuget.org/packages/NuPack

When NuPack is reference by a project, no dependency is created, there is only a new build step to automatically pack the project output into a nuget package with project name as id.

## Features
- **Nuspec : the manual way**

When a .nuspec file is detected as part of project, NuPack respect the specification and dosen't apply any auto configuration to produce the expected nuget package.

- **Library : the most common scenario**

Produce a simple library (lib folder) with nuget package dependencies. This pattern is automatically apply when there is no .nuspec file detected for project of type library.

- **Console : the build action**

When package is based on console application and .nuspec is not declared, a build directory is defined with a .targets file to provide a simple way to add a build action step as post build with project file (csproj) as first argument and configuration (Debug/Release) as second.

## Roadmap
- **Automatically include project dependency**

Dependency like project reference must be include recursively in library nuget package as nuget dependency is it produce a nuget package (project with NuPack inclusion) and must be include recursively as simple library (dll) if it is not a nuget producer.

- **Propagation of xml documentation**

Nuget process does not propagate xml documentation of dependency in output and cannot be considered in NuPack packaging process. Allow NuPack to propagate automatically xml documentation can help to keep coding documentation available in each project node.

- **Automatically detect relation between project and github.com to complete metadata**

One of frustrating thing with nuget is to have a clean and full metadata. Unfortunately, AssemblyInfo does not provide a way to expose all nuget needs. In other hand, it is often necessary to declare in multiple place the same informations that cause synchronization issue and add  a maintenance overhead. Using github.com api to automatically complete nuget creation can be a good thing to stay reactive.


## More
[How to create a nuget package using NuPack](https://www.codeproject.com/Tips/1190135/How-to-create-a-nuget-package-on-each-Visual-Studi)

