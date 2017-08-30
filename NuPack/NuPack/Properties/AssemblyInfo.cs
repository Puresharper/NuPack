using System;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("NuPack .NET 4.0+")]
[assembly: AssemblyDescription("NuPack .NET 4.0+ create a nuget package on build")]
[assembly: AssemblyCompany("Tony THONG")]
[assembly: AssemblyProduct("NuPack")]
[assembly: AssemblyCopyright("Copyright ©  2017 Tony THONG")]
[assembly: AssemblyTrademark("NuPack")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("85c23e55-ec01-4202-8bf6-4c708374359b")]
[assembly: AssemblyVersion("3.4.4")]
[assembly: AssemblyFileVersion("3.4.4")]
#if DEBUG
 [assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif