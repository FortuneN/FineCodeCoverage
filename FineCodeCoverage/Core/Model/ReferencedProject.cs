using System.IO;
using System.Xml.XPath;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Core.Model
{
    internal class ReferencedProject
	{
		internal const string excludeFromCodeCoveragePropertyName = "FCCExcludeFromCodeCoverage";
        private readonly string projectPath;

        public ReferencedProject(string projectPath,string assemblyPath)
        {
			AssemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            this.projectPath = projectPath;
        }

		public string AssemblyName { get; private set; }
		public bool ExcludeFromCodeCoverage
		{
			get
			{
				/*
					 ...
					<PropertyGroup>
						<FCCExcludeFromCodeCoverage />
					</PropertyGroup>
					...
				 */
				var projectFileXElement = XElementUtil.Load(projectPath, true);
				var excludeFromCodeCoverageProperty = projectFileXElement.XPathSelectElement($"/PropertyGroup/{excludeFromCodeCoveragePropertyName}");

				return excludeFromCodeCoverageProperty != null;
			}
		}
	}
}
