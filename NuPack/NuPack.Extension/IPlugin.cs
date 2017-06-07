using System;
using NuGet;

namespace NuPack.Extension
{
    /// <summary>
    /// Plugin to reorganize package builder
    /// </summary>
    public interface IPlugin : IDisposable
    {
        /// <summary>
        /// Produce a new package builder based on original.
        /// </summary>
        /// <param name="setting">Setting used to produce package</param>
        /// <param name="package">Package builder</param>
        /// <returns></returns>
        PackageBuilder Update(Setting setting, PackageBuilder package);
    }
}
