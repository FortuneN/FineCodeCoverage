using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using System.IO;
using VSLangProj;

namespace FineCodeCoverage.Core.Model
{
	internal class ReferencedProject
	{
        public ReferencedProject(Reference reference)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var referenceProjectPath = reference.SourceProject.FullName;
			var projectFileXElement = XElementUtil.Load(referenceProjectPath, true);

			HasExcludeFromCodeCoverageAssemblyAttribute = FCCEngine.HasExcludeFromCodeCoverageAssemblyAttribute(projectFileXElement);
			AssemblyName = Path.GetFileNameWithoutExtension(reference.Path);
		}

		public string AssemblyName { get; private set; }
		public bool HasExcludeFromCodeCoverageAssemblyAttribute { get; private set; }
	}
}
