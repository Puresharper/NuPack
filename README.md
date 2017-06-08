[![NuGet](https://img.shields.io/nuget/v/nupack.svg)](https://www.nuget.org/packages/NuPack)

# NuPack

NuPack is an easy way to produce a nuget package for .NET 4.0+ based on AssemblyInfo or nuspec when building with Visual Studio. It is materialized as a nuget package.

When NuPack is reference by a project, no dependency is created, there is only a new build step to automatically pack the project output into a nuget package with project name as id.

## Features
- **Nuspec : the manual way**

When a .nuspec file is detected as part of project, NuPack respect the specification and dosen't apply any auto configuration to produce the expected nuget package.

- **Library : the most common scenario**

Produce a simple library (lib folder) with nuget package dependencies. This pattern is automatically apply when there is no .nuspec file detected for project of type library.

- **Console : the build action**

When package is based on console application and .nuspec is not declared, a build directory is defined with a .targets file to provide a simple way to add a build action step as post build with 5 arguments : $(SolutionPath), $(ProjectPath), $(Configuration), $(PlatformName) and $(TargetPath)

- **Automatically include dependencies**

Dependencies can be a nuget packages, project references, etc... They must be include recursively in the generated nuget package with an adequat form.

- **Propagation of resources**

Nuget process does not propagate xml documentation and resources of dependency in output and cannot be considered in NuPack packaging process. Allow NuPack to propagate them automatically help to keep a clean structure and documentation in each node.

- **Extensibility pattern with custom nuget package** [![NuGet](https://img.shields.io/nuget/v/nupack.extension.svg)](https://www.nuget.org/packages/NuPack.Extension)

NuPack can provide a library to develop a plugin as nuget package. It will detect plugin from package.config and load it from NuPack folder to add additional behavior to NuPack. Plugin will be called with arguments passed to NuPack and produce a PackageBuilder from original PackageBuilder before save result. Plugin is disposed after nuget creation.

- **Detection of NuPack Extension project to handle it**

NuPack have to create a specific package to store plugin (library) in NuPack folder when project reference NuPack.Extension.

## Roadmap
- **Optimizer pattern for console application**

When NuPack is referenced by a console application named [Library].Optimizer and reference a library named [Library], an optimizer pattern is done. Generated nuget package contains the [Library].dll into lib folder and [Library].Optimizer.exe is placed into build folder with a .targets file to execute optimizer on postbuild with same arguments than standard build action for console application pattern. The nuget package will take [Library] name as id. It means that pattern is not done if [Library] is a nuget producer. Optimizer pattern will be an entry point to rewrite IL for example or prepare something based on [Library].dll usage.

- **Support for Visual Studio 2017**

Visual Studio 2017 changed nuget integration template. NuPack have to be updated to support Visual Studio 2017.

- **Automatically detect relation between project and github.com to complete metadata**

One of frustrating thing with nuget is to have a clean and full metadata. Unfortunately, AssemblyInfo does not provide a way to expose all nuget needs. In other hand, it is often necessary to declare in multiple place the same informations that cause synchronization issue and add  a maintenance overhead. Using github.com api to automatically complete nuget creation can be a good thing to stay reactive.


## More
- [How to create a nuget package using NuPack](https://www.codeproject.com/Tips/1190135/How-to-create-a-nuget-package-on-each-Visual-Studi)
- [How to setup a managed postbuild with NuPack](https://www.codeproject.com/Tips/1190360/How-to-setup-a-managed-postbuild-without-scripting)
- [How to extend NuPack to complete its behavior thanks to NuPack Extension 4.0+](https://www.codeproject.com/Tips/1190921/How-to-extend-NuPack-to-complement-its-behavior)
- [Swagger4WCF use NuPack postbuild pattern](https://www.codeproject.com/Tips/1190441/How-to-generate-basic-swagger-yaml-description-for)
