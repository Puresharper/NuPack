using System;

namespace NuPack.Extension
{
    /// <summary>
    /// Plugin to reorganize package.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Produce a new package based on original.
        /// </summary>
        /// <param name="setting">Setting used to produce package</param>
        /// <param name="package">Package definition</param>
        /// <returns></returns>
        Package Update(Setting setting, Package package);
    }
}
