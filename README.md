# NuPack

NuPack is an easy way to produce a nuget package for .NET 4.0+ based en AssemblyInfo or nuspec when building with Visual Studio.

## Library
Produce a simple library with nuget package dependencies for library project type

## Console
When package is based on console application, a build directory is defined with a .targets file to provide a simple way to add a build action step as post build with project file (csproj) as first argument and configuration (Debug/Release) as second.

## Nuspec
When a .nuspec file is detected as part of project, NuPack respect the specification and dosen't apply any auto configuration.
