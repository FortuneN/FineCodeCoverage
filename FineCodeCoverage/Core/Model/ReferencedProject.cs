using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using FineCodeCoverage.Core.Utilities;
using VSLangProj;

namespace FineCodeCoverage.Core.Model
{
    internal class ReferencedProject
	{
        private readonly XElement projectFileXElement;

        public ReferencedProject(Reference reference)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var referenceProjectPath = reference.SourceProject.FullName;
			projectFileXElement = XElementUtil.Load(referenceProjectPath, true);
			AssemblyName = Path.GetFileNameWithoutExtension(reference.Path);
		}

		public string AssemblyName { get; private set; }
		public bool HasExcludeFromCodeCoverageAssemblyAttribute
		{
			get
			{
					/*
				 ...
				<ItemGroup>
					<AssemblyAttribute Include="System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute" />
				</ItemGroup>
				...
				 */

				var xassemblyAttribute = projectFileXElement.XPathSelectElement($"/ItemGroup/AssemblyAttribute[@Include='System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute']");

				return xassemblyAttribute != null;
			}
		}
	}
}
