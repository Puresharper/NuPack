using NuGet;
using System;

namespace NuPack.Extension
{
    /// <summary>
    /// Setting used to call NuPack
    /// </summary>
    public class Setting
    {
        private string m_Solution;
        private string m_Project;
        private string m_Configuration;
        private string m_Plateform;
        private string m_Assembly;

        /// <summary>
        /// Setting
        /// </summary>
        /// <param name="solution">Solution</param>
        /// <param name="project">Project</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="plateform">Plateform</param>
        /// <param name="assembly">Assembly</param>
        public Setting(string solution, string project, string configuration, string plateform, string assembly)
        {
            this.m_Solution = solution;
            this.m_Project = project;
            this.m_Configuration = configuration;
            this.m_Plateform = plateform;
            this.m_Assembly = assembly;
        }

        /// <summary>
        /// Solution.
        /// </summary>
        public string Solution
        {
            get { return this.m_Solution; }
        }

        /// <summary>
        /// Project.
        /// </summary>
        public string Project
        {
            get { return this.m_Project; }
        }

        /// <summary>
        /// Configuration.
        /// </summary>
        public string Configuration
        {
            get { return this.m_Solution; }
        }

        /// <summary>
        /// Plateform.
        /// </summary>
        public string Plateform
        {
            get { return this.m_Plateform; }
        }

        /// <summary>
        /// Assembly.
        /// </summary>
        public string Assembly
        {
            get { return this.m_Assembly; }
        }
    }
}
