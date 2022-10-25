using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using SharedProject.Core.Model;

namespace FineCodeCoverage.Engine.Cobertura
{
    interface ICoberturaUtil
    {
        FileLineCoverage ProcessCoberturaXml(string xmlFile);
		string[] GetSourceFiles(string assemblyName, string qualifiedClassName, int file);
	}
}