using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Cobertura
{
    interface ICoberturaUtil
    {
		List<CoverageLine> CoverageLines { get; }
		
		void ProcessCoberturaXmlFile(string xmlFilePath);
		string[] GetSourceFiles(string assemblyName, string qualifiedClassName, int file);
	}
}