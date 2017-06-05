using System;
using NuGet;

namespace NuPack.Extension
{
    /// <summary>
    /// Package definition.
    /// </summary>
    public class Package
    {
        private string m_Directory;
        private PackageBuilder m_Builder;

        /// <summary>
        /// Create a new package definition.
        /// </summary>
        /// <param name="directory">Directory where package will be save</param>
        /// <param name="builder">Builder to use to produce package</param>
        public Package(string directory, PackageBuilder builder)
        {
            this.m_Directory = directory;
            this.m_Builder = builder;
        }

        /// <summary>
        /// Directory where package will be save.
        /// </summary>
        public string Directory
        {
            get { return this.m_Directory; }
        }

        /// <summary>
        /// Builder to use to produce package.
        /// </summary>
        public PackageBuilder Builder
        {
            get { return this.m_Builder; }
        }
    }
}
