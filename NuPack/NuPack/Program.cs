using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Threading;
using Mono.Cecil;
using NuGet;
using NuPack.Extension;

namespace NuCreate
{
    static public class Program
    {
        static private string m_Native = BitConverter.ToString(typeof(object).Assembly.GetName().GetPublicKeyToken()).Replace("-", string.Empty);

        static public void Main(string[] arguments)
        {
            Program.Create(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4]);
        }

        static private Manifest Open(string filename)
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

        static private IEnumerable<string> Resources(string project, string configuration, bool dependency)
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
                        if (dependency)
                        {
                            foreach (var _package in Directory.EnumerateFiles(string.Concat(_directory, @"\", _element.Value), "*.nupkg"))
                            {
                                foreach (var _resource in new ZipPackage(_package).GetLibFiles())
                                {
                                    if (_resource.IsEmptyFolder()) { continue; }
                                    yield return Path.GetFileName(_resource.Path);
                                }
                            }
                        }
                        foreach (var _project in _document.Descendants(_namespace.GetName("ProjectReference")).Select(_Reference => string.Concat(_directory, @"\", _Reference.Attribute("Include").Value)))
                        {
                            foreach (var _resource in Program.Resources(_project, configuration, true))
                            {
                                yield return _resource;
                            }
                        }
                        yield break;
                    }
                }
            }
        }

        static private IEnumerable<IPlugin> Extends(IEnumerable<PackageReference> dependencies, Setting setting, ref PackageBuilder package)
        {
            var _plugins = new List<IPlugin>();
            var _solution = Path.GetDirectoryName(setting.Solution);
            foreach (var _dependency in dependencies)
            {
                var _directory = string.Concat(_solution, @"\packages\", _dependency.Id, ".", _dependency.Version, @"\NuPack");
                if (Directory.Exists(_directory))
                {
                    foreach (var _library in Directory.EnumerateFiles(_directory, "*.dll"))
                    {
                        try
                        {
                            var _assembly = Assembly.LoadFrom(_library);
                            var _identity = BitConverter.ToString(_assembly.GetName().GetPublicKeyToken()).Replace("-", string.Empty);
                            if (string.Equals(_identity, Program.m_Native, StringComparison.InvariantCultureIgnoreCase)) { continue; }
                            foreach (var _type in _assembly.GetTypes())
                            {
                                if (!_type.IsAbstract && typeof(IPlugin).IsAssignableFrom(_type))
                                {
                                    var _constructor = _type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
                                    if (_constructor == null) { continue; }
                                    _plugins.Add(Activator.CreateInstance(_type, true) as IPlugin);
                                }
                            }
                        }
                        catch { continue; }
                    }
                }
            }
            foreach (var _plugin in _plugins) { package = _plugin.Update(setting, package); }
            _plugins.Reverse();
            return _plugins;
        }

        static private string Specification(string project)
        {
            var _document = XDocument.Load(project);
            var _namespace = _document.Root.Name.Namespace;
            var _element = _document.Descendants(_namespace.GetName("None")).SingleOrDefault(_Element => _Element.Attribute("Include") != null && _Element.Attribute("Include").Value.EndsWith(".nuspec", StringComparison.CurrentCultureIgnoreCase));
            return _element == null ? null : string.Concat(Path.GetDirectoryName(project), _element.Attribute("Include").Value);
        }

        static private string Save(string directory, PackageBuilder package)
        {
            var _filename = $@"{ directory }\{ package.Id }.{ package.Version }.nupkg";
            using (var _memory = new MemoryStream())
            {
                package.Save(_memory);
                _memory.Seek(0L, SeekOrigin.Begin);
                Program.Try(() => { if (File.Exists(_filename)) { File.Delete(_filename); } });
                using (var _stream = File.Open(_filename, FileMode.Create)) { _memory.CopyTo(_stream); }
            }
            return _filename;
        }

        static private IEnumerable<string> Bin(string directory, params string[] exclusion)
        {
            foreach (var _filename in Directory.EnumerateFiles(directory))
            {
                var _name = Path.GetFileName(_filename);
                if (exclusion != null && exclusion.Any(_Name => string.Equals(_Name, _name, StringComparison.CurrentCultureIgnoreCase))) { continue; }
                if (_name.EndsWith(".vshost.exe", StringComparison.CurrentCultureIgnoreCase) || _name.EndsWith(".vshost.exe.manifest", StringComparison.CurrentCultureIgnoreCase)) { continue; }
                if (_name.EndsWith(".nupkg", StringComparison.CurrentCultureIgnoreCase) || _name.EndsWith(".pdb", StringComparison.CurrentCultureIgnoreCase) || _name.EndsWith(".tmp", StringComparison.CurrentCultureIgnoreCase) || _name.EndsWith(".bak", StringComparison.CurrentCultureIgnoreCase)) { continue; }
                yield return _filename;
            }
        }

        static private ZipPackage Template()
        {
            using (var _stream = typeof(Program).Assembly.GetManifestResourceStream("NuPack.Template.1.0.0.nupkg"))
            {
                return new ZipPackage(_stream);
            }
        }

        static private void Create(string solution, string project, string configuration, string plateform, string assembly)
        {
            var _assembly = AssemblyDefinition.ReadAssembly(assembly);
            var _name = _assembly.Name.Name; 
            var _version = _assembly.Metadata<AssemblyFileVersionAttribute>();
            var _directory = Path.GetDirectoryName(assembly);
            var _dependencies = new PackageReferenceFile(string.Concat(Path.GetDirectoryName(project), @"\packages.config")).GetPackageReferences().Where(_Package => _Package.Id != "NuPack").ToArray();
            var _extension = _dependencies.Any(_Dependency => _Dependency.Id == "NuPack.Extension");
            var _setting = new Setting(solution, project, configuration, plateform, assembly);
            var _specification = Program.Specification(project);
            if (_specification == null)
            {
                var _builder = typeof(PackageBuilder).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(bool) }, null).Invoke(new object[] { true }) as PackageBuilder;
                var _metadata = new ManifestMetadata()
                {
                    Id = _name,
                    Title = _assembly.Metadata<AssemblyProductAttribute>() ?? _name,
                    Authors = _assembly.Metadata<AssemblyCompanyAttribute>() ?? "-",
                    Version = _version,
                    Summary = _assembly.Metadata<AssemblyDescriptionAttribute>() ?? (_assembly.Metadata<AssemblyProductAttribute>() ?? "-"),
                    Description = _assembly.Metadata<AssemblyDescriptionAttribute>() ?? "-",
                    Copyright = _assembly.Metadata<AssemblyCopyrightAttribute>() ?? "-"
                };
                _builder.Populate(_metadata);
                if (assembly.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (_extension) { throw new NotSupportedException(); }
                    var _targets = string.Concat(assembly.Substring(0, assembly.Length - 4), ".targets");
                    _builder.PopulateFiles(null, Program.Bin(_directory).Select(_Filename => new ManifestFile() { Source = _Filename, Target = "build" }));
                    var _plugins = Program.Extends(_dependencies, _setting, ref _builder);
                    Program.Try(() => { if (File.Exists(_targets)) { File.Delete(_targets); } });
                    using (var _stream = typeof(Program).Assembly.GetManifestResourceStream("NuPack.Template")) { File.WriteAllText(_targets, new StreamReader(_stream).ReadToEnd().Replace("[name]", _name).Replace("[version]", _builder.Version.ToOriginalString()), Encoding.UTF8); }
                    var _filename = Program.Save(_directory, _builder);
                    Program.Try(() => { if (File.Exists(_targets)) { File.Delete(_targets); } });
                    Console.WriteLine($"{ _name } -> { _filename }");
                    foreach (var _plugin in _plugins) { _plugin.Dispose(); }
                }
                else
                {
                    if (_extension)
                    {
                        _builder.Files.AddRange(Program.Template().GetContentFiles());
                        _builder.PopulateFiles(null, Program.Bin(_directory, "Microsoft.Web.XmlTransform.dll", "NuGet.Core.dll", "NuPack.Extension.dll", "NuPack.Extension.xml").Select(_Filename => new ManifestFile() { Source = _Filename, Target = $"NuPack" }));
                        var _plugins = Program.Extends(_dependencies, _setting, ref _builder);
                        Console.WriteLine($"{ _name } -> { Program.Save(_directory, _builder) }");
                        foreach (var _plugin in _plugins) { _plugin.Dispose(); }
                    }
                    else
                    {
                        var _dictionary = _dependencies.Select(_Dependency => string.Concat(solution, @"packages\", _Dependency.Id, ".", _Dependency.Version, @"\lib")).Where(_Library => Directory.Exists(_Library)).SelectMany(_Library => Directory.EnumerateFiles(_Library, "*", SearchOption.AllDirectories)).ToArray();
                        foreach (var _dependency in _dependencies.GroupBy(_Package => _Package.TargetFramework)) { _builder.DependencySets.Add(new PackageDependencySet(_dependency.Key, _dependency.Select(_Package => new PackageDependency(_Package.Id, _Package.VersionConstraint, null, null)))); }
                        var _targets = string.Concat(assembly.Substring(0, assembly.Length - 4), ".targets");
                        if (_dictionary.Any(_Resource => !_Resource.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase)))
                        {
                            using (var _stream = typeof(Program).Assembly.GetManifestResourceStream("NuPack.Template"))
                            {
                                var _document = XDocument.Parse(new StreamReader(_stream).ReadToEnd().Replace("[name]", _name), LoadOptions.PreserveWhitespace);
                                var _namespace = _document.Root.Name.Namespace;
                                var _sequence = _document.Descendants(_namespace.GetName("Target")).Single();
                                _sequence.RemoveNodes();
                                foreach (var _dependency in _dependencies)
                                {
                                    var _library = string.Concat(solution, @"packages\", _dependency.Id, ".", _dependency.Version.ToOriginalString(), @"\lib");
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
                                _builder.PopulateFiles(null, new ManifestFile[] { new ManifestFile() { Source = _targets, Target = $"build" } });
                            }
                        }
                        var _framework = $"lib/net{ _assembly.MainModule.RuntimeVersion[1] }{ _assembly.MainModule.RuntimeVersion[3] }/";
                        var _resources = _dictionary.Select(_Resource => Path.GetFileName(_Resource)).Concat(Program.Resources(project, configuration, false)).Select(_Resource => _Resource.ToLower()).Distinct().ToArray();
                        _builder.PopulateFiles(null, Program.Bin(_directory, Path.GetFileName(_targets)).Where(_Resource => !_resources.Contains(Path.GetFileName(_Resource).ToLower())).Select(_Resource => new ManifestFile() { Source = _Resource, Target = _framework }));
                        var _plugins = Program.Extends(_dependencies, _setting, ref _builder);
                        var _filename = Program.Save(_directory, _builder);
                        Program.Try(() => { if (File.Exists(_targets)) { File.Delete(_targets); } });
                        Console.WriteLine($"{ _name } -> { _filename }");
                        foreach (var _plugin in _plugins) { _plugin.Dispose(); }
                    }
                }
            }
            else
            {
                var _builder = new PackageBuilder();
                var _manifest = Program.Open(_specification);
                _builder.Populate(_manifest.Metadata);
                _builder.PopulateFiles(null, _manifest.Files);
                var _plugins = Program.Extends(_dependencies, _setting, ref _builder);
                Console.WriteLine($"{ _name } -> { Program.Save(_directory, _builder) }");
                foreach (var _plugin in _plugins) { _plugin.Dispose(); }
            }
        }
    }
}
