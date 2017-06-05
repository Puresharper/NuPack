using System;
using NuGet;

namespace NuPack.Extension.Sample
{
    public class Plugin : IPlugin
    {
        public Package Update(Setting setting, Package package)
        {
            package.Builder.LicenseUrl = new Uri("https://raw.githubusercontent.com/Virtuoze/NuPack/master/LICENSE.md");
            return package;
        }
    }
}
