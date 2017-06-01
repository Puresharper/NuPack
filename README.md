# NuPack

NuPack is an easy way to produce a nuget package for .NET 4.0+ based en AssemblyInfo or nuspec when building with Visual Studio. It is materialized as a nuget package.

https://www.nuget.org/packages/NuPack

When NuPack is reference by a project, no dependency is created, there is only a new build step to automatically pack the project output into a nuget package with project name as id.

## Nuspec : the manual way
When a .nuspec file is detected as part of project, NuPack respect the specification and dosen't apply any auto configuration.

## Library : the most common scenario 
Produce a simple library (lib folder) with nuget package dependencies. This pattern is automatically apply when there is no .nuspec file detected for project of type library.

## Console : the build action
When package is based on console application and .nuspect is not declared, a build directory is defined with a .targets file to provide a simple way to add a build action step as post build with project file (csproj) as first argument and configuration (Debug/Release) as second.
