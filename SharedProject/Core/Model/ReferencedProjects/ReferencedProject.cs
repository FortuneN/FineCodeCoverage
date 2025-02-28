using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine.Model
{
    internal class ReferencedProject : IExcludableReferencedProject
    {
		internal const string excludeFromCodeCoveragePropertyName = "FCCExcludeFromCodeCoverage";
        private readonly string projectPath;
		

        public ReferencedProject(string projectPath,string assemblyName,bool isDll)
        {
            this.projectPath = projectPath;
            AssemblyName = assemblyName;
            IsDll = isDll;
        }

		public ReferencedProject(string projectPath)
        {
			this.projectPath = projectPath;
			AssemblyName = GetAssemblyName(LinqToXmlUtil.Load(projectPath, true), Path.GetFileNameWithoutExtension(projectPath));
		}

		private string GetAssemblyName(XElement projectFileXElement, string fallbackName = null)
		{
			/*
			<PropertyGroup>
				...
				<AssemblyName>Branch_Coverage.Tests</AssemblyName>
				...
			</PropertyGroup>
			 */

			var xassemblyName = projectFileXElement.XPathSelectElement("/PropertyGroup/AssemblyName");

			var result = xassemblyName?.Value.Trim();

			if (string.IsNullOrWhiteSpace(result))
			{
				result = fallbackName;
			}

			return result;
		}

		public string AssemblyName { get; }

		public bool IsDll { get; } = true;

        /*
			Annoyingly by allowing <FCCExcludeFromCodeCoverage /> and not <FCCExcludeFromCodeCoverage>true</FCCExcludeFromCodeCoverage>
			it is not possible to use IVsBuildPropertyStorage.
			Todo - consider breaking change to <FCCExcludeFromCodeCoverage>true</FCCExcludeFromCodeCoverage>
			Given that purpose is for dotnet framework.....
		*/
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
				var projectFileXElement = LinqToXmlUtil.Load(projectPath, true);
				var excludeFromCodeCoverageProperty = projectFileXElement.XPathSelectElement($"/PropertyGroup/{excludeFromCodeCoveragePropertyName}");

				return excludeFromCodeCoverageProperty != null;
			}
		}
	}
}
