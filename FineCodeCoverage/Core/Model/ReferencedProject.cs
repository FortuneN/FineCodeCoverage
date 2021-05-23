using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Core.Model
{
    internal class ReferencedProject
    {
        internal const string excludeFromCodeCoveragePropertyName = "FCCExcludeFromCodeCoverage";
        private readonly string projectPath;

        public ReferencedProject(string projectPath, string assemblyName)
        {
            AssemblyName = assemblyName;
            this.projectPath = projectPath;
        }
        public ReferencedProject(string projectPath)
        {
            this.projectPath = projectPath;
            AssemblyName = GetAssemblyName(XElementUtil.Load(projectPath, true), Path.GetFileNameWithoutExtension(projectPath));
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
