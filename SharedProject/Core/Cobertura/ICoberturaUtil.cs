using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Cobertura
{
    interface ICoberturaUtil
    {
        FileLineCoverage ProcessCoberturaXml(string xmlFile);
		string[] GetSourceFiles(string assemblyName, string qualifiedClassName, int file);
	}
}