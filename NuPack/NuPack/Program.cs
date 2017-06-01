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

namespace NuCreate
{
    static public class Program
    {
        static public void Main(string[] arguments)
        {
            if (arguments == null) { throw new ArgumentNullException(); }
            switch (arguments.Length)
            {
                case 2:
                    var _directory = string.Concat(Path.GetDirectoryName(arguments[0]), @"\");
                    var _document = XDocument.Load(arguments[0]);
                    var _namespace = _document.Root.Name.Namespace;
                    var _name = _document.Descendants(_namespace.GetName("AssemblyName")).Single().Value;
                    var _type = _document.Descendants(_namespace.GetName("OutputType")).SingleOrDefault();
                    var _nuspec = _document.Descendants(_namespace.GetName("None")).SingleOrDefault(_Element => _Element.Attribute("Include") != null && _Element.Attribute("Include").Value.EndsWith(".nuspec", StringComparison.CurrentCultureIgnoreCase));
                    foreach (var _element in _document.Descendants(_namespace.GetName("OutputPath")))
                    {
                        foreach (var _attribute in _element.Parent.Attributes())
                        {
                            if (_attribute.Value.Contains(arguments[1]))
                            {
                                var _identity = string.Concat(_directory, _element.Value, _name);
                                switch (_type == null ? "Library" : _type.Value)
                                {
                                    case "Library": Program.Create(_directory, string.Concat(_identity, ".dll"), _nuspec == null ? null : string.Concat(_directory, _nuspec.Attribute("Include").Value)); return;
                                    case "WinExe":
                                    case "Exe": Program.Create(_directory, string.Concat(_identity, ".exe"), _nuspec == null ? null : string.Concat(_directory, _nuspec.Attribute("Include").Value)); return;
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

        static private void Create(string directory, string assembly, string nuspec)
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
                    Authors = _assembly.Metadata<AssemblyCompanyAttribute>() ?? "-",
                    Version = _version,
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
                    _manifest.PopulateFiles(null, Directory.EnumerateFiles(_directory).Where(_Filename => !_Filename.EndsWith(".vshost..exe", StringComparison.CurrentCultureIgnoreCase) && !_Filename.EndsWith(".pdb", StringComparison.CurrentCultureIgnoreCase) && !_Filename.EndsWith(".pdb", StringComparison.CurrentCultureIgnoreCase)).Select(_Filename => new ManifestFile() { Source = _Filename, Target = $"build" }));
                    using (var _stream = File.Open(_package, FileMode.Create)) { _manifest.Save(_stream); }
                    Program.Try(() => { if (File.Exists(_targets)) { File.Delete(_targets); } });
                }
                else
                {
                    foreach (var _packages in new PackageReferenceFile(string.Concat(directory, "packages.config")).GetPackageReferences().Where(_Package => _Package.Id != "NuPack").GroupBy(_Package => _Package.TargetFramework)) { _manifest.DependencySets.Add(new PackageDependencySet(_packages.Key, _packages.Select(_Package => new PackageDependency(_Package.Id, _Package.VersionConstraint, null, null)))); }
                    _manifest.PopulateFiles(null, new[] { new ManifestFile() { Source = assembly, Target = $"lib/net{ _assembly.MainModule.RuntimeVersion[1] }{ _assembly.MainModule.RuntimeVersion[3] }/" } });
                    using (var _stream = File.Open(_package, FileMode.Create)) { _manifest.Save(_stream); }
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
