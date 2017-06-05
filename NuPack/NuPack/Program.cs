using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Threading;
using Mono.Cecil;
using NuGet;
using System.Runtime.Versioning;
using System.Collections.Generic;

namespace NuCreate
{
    static public class Program
    {
        static public void Main(string[] arguments)
        {
            if (arguments == null) { throw new ArgumentNullException(); }
            switch (arguments.Length)
            {
                case 3:
                    var _solution = arguments[0];
                    var _directory = string.Concat(Path.GetDirectoryName(arguments[1]), @"\");
                    var _document = XDocument.Load(arguments[1]);
                    var _namespace = _document.Root.Name.Namespace;
                    var _name = _document.Descendants(_namespace.GetName("AssemblyName")).Single().Value;
                    var _type = _document.Descendants(_namespace.GetName("OutputType")).SingleOrDefault();
                    var _nuspec = _document.Descendants(_namespace.GetName("None")).SingleOrDefault(_Element => _Element.Attribute("Include") != null && _Element.Attribute("Include").Value.EndsWith(".nuspec", StringComparison.CurrentCultureIgnoreCase));
                    foreach (var _element in _document.Descendants(_namespace.GetName("OutputPath")))
                    {
                        foreach (var _attribute in _element.Parent.Attributes())
                        {
                            if (_attribute.Value.Contains(arguments[2]))
                            {
                                var _identity = string.Concat(_directory, _element.Value, _name);
                                switch (_type == null ? "Library" : _type.Value)
                                {
                                    case "Library": Program.Create(_solution, arguments[1], arguments[2], _directory, string.Concat(_identity, ".dll"), _nuspec == null ? null : string.Concat(_directory, _nuspec.Attribute("Include").Value)); return;
                                    case "WinExe":
                                    case "Exe": Program.Create(_solution, arguments[1], arguments[2], _directory, string.Concat(_identity, ".exe"), _nuspec == null ? null : string.Concat(_directory, _nuspec.Attribute("Include").Value)); return;
                                    default: throw new NotSupportedException($"Unknown OutputType: {_type.Value}");
                                }
                            }
                        }
                    }
                    break;
                default: throw new ArgumentException();
            }
        }

        static private Manifest Nuspec(string filename)
        {
            using (var _stream = File.Open(filename, FileMode.Open))
            {
                return Manifest.ReadFrom(_stream, true);
            }
        }

        static private string Metadata<T>(this AssemblyDefinition assembly)
            where T : Attribute
        {
            var _attribute = assembly.CustomAttributes.FirstOrDefault(_Attribute => _Attribute.AttributeType.Resolve() == assembly.MainModule.Import(typeof(T)).Resolve());
            if (_attribute == null) { return null; }
            var _value = _attribute.ConstructorArguments.Single().Value.ToString();
            if (string.IsNullOrWhiteSpace(_value)) { return null; }
            return _value;
        }

        static private void Try(Action action)
        {
            var _index = 0;
            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch
                {
                    if (++_index > 10) { throw; }
                    Thread.Sleep(64);
                }
            }
        }

        static private IEnumerable<string> Resources(string project, string configuration)
        {
            var _directory = Path.GetDirectoryName(project);
            var _document = XDocument.Load(project);
            var _namespace = _document.Root.Name.Namespace;
            foreach (var _element in _document.Descendants(_namespace.GetName("OutputPath")))
            {
                foreach (var _attribute in _element.Parent.Attributes())
                {
                    if (_attribute.Value.Contains(configuration))
                    {
                        foreach (var _package in Directory.EnumerateFiles(string.Concat(_directory, @"\", _element.Value), "*.nupkg"))
                        {
                            foreach (var _resource in new ZipPackage(_package).GetLibFiles())
                            {
                                if (_resource.IsEmptyFolder()) { continue; }
                                yield return Path.GetFileName(_resource.Path);
                            }
                        }
                        foreach (var _project in _document.Descendants(_namespace.GetName("ProjectReference")).Select(_Reference => string.Concat(_directory, @"\", _Reference.Attribute("Include").Value)))
                        {
                            foreach (var _resource in Program.Resources(_project, configuration))
                            {
                                yield return _resource;
                            }
                        }
                        yield break;
                    }
                }
            }
        }

        static private void Create(string solution, string project, string configuration, string directory, string assembly, string nuspec)
        {
            var _assembly = AssemblyDefinition.ReadAssembly(assembly);
            var _name = _assembly.Name.Name; 
            var _version = _assembly.Metadata<AssemblyFileVersionAttribute>();
            var _package = string.Concat(assembly.Substring(0, assembly.Length - 4), ".", _version, ".nupkg");
            if (nuspec == null)
            {
                var _manifest = new PackageBuilder();
                var _metadata = new ManifestMetadata()
                {
                    Id = _name,
                    Title = _assembly.Metadata<AssemblyProductAttribute>() ?? "-",
                    Authors = _assembly.Metadata<AssemblyCompanyAttribute>() ?? "-",
                    Version = _version,
                    Summary = _assembly.Metadata<AssemblyDescriptionAttribute>() ?? (_assembly.Metadata<AssemblyProductAttribute>() ?? "-"),
                    Description = _assembly.Metadata<AssemblyDescriptionAttribute>() ?? "-",
                    Copyright = _assembly.Metadata<AssemblyCopyrightAttribute>() ?? "-"
                };
                _manifest.Populate(_metadata);
                Program.Try(() => { if (File.Exists(_package)) { File.Delete(_package); } });
                var _directory = Path.GetDirectoryName(assembly);
                if (assembly.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    var _targets = string.Concat(assembly.Substring(0, assembly.Length - 4), ".targets");
                    using (var _stream = typeof(Program).Assembly.GetManifestResourceStream("NuPack.Template")) { File.WriteAllText(_targets, new StreamReader(_stream).ReadToEnd().Replace("[name]", _name).Replace("[version]", _version), Encoding.UTF8); }
                    _manifest.PopulateFiles(null, Directory.EnumerateFiles(_directory).Where(_Filename => !_Filename.EndsWith(".vshost.exe", StringComparison.CurrentCultureIgnoreCase) && !_Filename.EndsWith(".vshost.exe.manifest", StringComparison.CurrentCultureIgnoreCase) && !_Filename.EndsWith(".pdb", StringComparison.CurrentCultureIgnoreCase) && !_Filename.EndsWith(".tmp", StringComparison.CurrentCultureIgnoreCase) && !_Filename.EndsWith(".bak", StringComparison.CurrentCultureIgnoreCase)).Select(_Filename => new ManifestFile() { Source = _Filename, Target = $"build" }));
                    using (var _stream = File.Open(_package, FileMode.Create)) { _manifest.Save(_stream); }
                    Program.Try(() => { if (File.Exists(_targets)) { File.Delete(_targets); } });
                }
                else
                {
                    var _dependencies = new PackageReferenceFile(string.Concat(directory, "packages.config")).GetPackageReferences().Where(_Package => _Package.Id != "NuPack").ToArray();
                    var _dictionary = _dependencies.Select(_Dependency => string.Concat(solution, @"packages\", _Dependency.Id, ".", _Dependency.Version, @"\lib")).Where(_Library => Directory.Exists(_Library)).SelectMany(_Library => Directory.EnumerateFiles(_Library, "*", SearchOption.AllDirectories)).ToArray();
                    foreach (var _dependency in _dependencies.GroupBy(_Package => _Package.TargetFramework)) { _manifest.DependencySets.Add(new PackageDependencySet(_dependency.Key, _dependency.Select(_Package => new PackageDependency(_Package.Id, _Package.VersionConstraint, null, null)))); }
                    var _targets = string.Concat(assembly.Substring(0, assembly.Length - 4), ".targets");
                    if (_dictionary.Any(_Resource => !_Resource.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        using (var _stream = typeof(Program).Assembly.GetManifestResourceStream("NuPack.Template"))
                        {
                            var _document = XDocument.Parse(new StreamReader(_stream).ReadToEnd().Replace("[name]", _name).Replace("[version]", _version), LoadOptions.PreserveWhitespace);
                            var _namespace = _document.Root.Name.Namespace;
                            var _sequence = _document.Descendants(_namespace.GetName("Target")).Single();
                            _sequence.RemoveNodes();
                            foreach (var _dependency in _dependencies)
                            {
                                var _library = string.Concat(solution, @"packages\", _dependency.Id, ".", _dependency.Version, @"\lib");
                                if (Directory.Exists(_library))
                                {
                                    foreach (var _resource in Directory.EnumerateFiles(_library).Select(_Filename => Path.GetFileName(_Filename)))
                                    {
                                        if (_resource.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase)) { continue; }
                                        _sequence.Add(new XElement(_namespace.GetName("Exec"), new XAttribute("Command", $"xcopy /f /q /y \"$(SolutionDir)packages\\{ _dependency.Id }.{ _dependency.Version }\\lib\\{ _resource }\" \"$(ProjectDir)$(OutDir)*\" > nul")));
                                    }
                                }
                            }
                            File.WriteAllText(_targets, _document.ToString(), Encoding.UTF8);
                            _manifest.PopulateFiles(null, new ManifestFile[] { new ManifestFile() { Source = _targets, Target = $"build" } });
                        }
                    }
                    var _framework = $"lib/net{ _assembly.MainModule.RuntimeVersion[1] }{ _assembly.MainModule.RuntimeVersion[3] }/";
                    var _resources = _dictionary.Select(_Resource => Path.GetFileName(_Resource)).Concat(Program.Resources(project, configuration)).Select(_Resource => _Resource.ToLower()).Distinct().ToArray();
                    _manifest.PopulateFiles(null, Directory.EnumerateFiles(_directory).Where(_Resource => !string.Equals(_Resource, _targets, StringComparison.CurrentCultureIgnoreCase) && !_Resource.EndsWith(".pdb", StringComparison.CurrentCultureIgnoreCase) && !_Resource.EndsWith(".tmp", StringComparison.CurrentCultureIgnoreCase) && !_Resource.EndsWith(".bak", StringComparison.CurrentCultureIgnoreCase) && !_resources.Contains(Path.GetFileName(_Resource).ToLower())).Select(_Resource => new ManifestFile() { Source = _Resource, Target = _framework }));
                    using (var _stream = File.Open(_package, FileMode.Create)) { _manifest.Save(_stream); }
                    Program.Try(() => { if (File.Exists(_targets)) { File.Delete(_targets); } });
                }
            }
            else
            {
                var _manifest = Program.Nuspec(nuspec);
                using (var _stream = File.Open(_package, FileMode.Create)) { _manifest.Save(_stream, true); }
            }
            Console.WriteLine($"{ _name } -> { _package }");
        }
    }
}
