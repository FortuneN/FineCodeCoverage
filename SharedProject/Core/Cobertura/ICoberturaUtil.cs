using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Cobertura
{
    interface ICoberturaUtil
    {
        IFileLineCoverage ProcessCoberturaXml(string xmlFile);
		string[] GetSourceFiles(string assemblyName, string qualifiedClassName, int file);
	}
}