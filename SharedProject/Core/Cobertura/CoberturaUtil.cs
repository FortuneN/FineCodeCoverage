using System.Xml;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine.Cobertura
{
    [Export(typeof(ICoberturaUtil))]
	internal class CoberturaUtil:ICoberturaUtil
    {
		private readonly XmlSerializer SERIALIZER = new XmlSerializer(typeof(CoverageReport));
		private readonly XmlReaderSettings READER_SETTINGS = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
		private CoverageReport coverageReport;
        private FileLineCoverage fileLineCoverage;

		[ImportingConstructor]
		public CoberturaUtil(
            IFileRenameListener fileRenameListener
        )
		{
            fileRenameListener.ListenForFileRename((oldFile, newFile) =>
            {
                fileLineCoverage?.UpdateRenamed(oldFile, newFile);
            });

		}

        private CoverageReport LoadReport(string xmlFile)
		{
			using (var reader = XmlReader.Create(xmlFile, READER_SETTINGS))
			{
				var report = (CoverageReport)SERIALIZER.Deserialize(reader);
				return report;
			}
		}

		public IFileLineCoverage ProcessCoberturaXml(string xmlFile)
		{
			fileLineCoverage = new FileLineCoverage();

			coverageReport = LoadReport(xmlFile);

			foreach (var package in coverageReport.Packages.Package)
			{
				foreach (var classs in package.Classes.Class)
				{
					fileLineCoverage.Add(classs.Filename, classs.Lines.Line);
				}
			}

            fileLineCoverage.Completed();
            return fileLineCoverage;
		}



		public string[] GetSourceFiles(string assemblyName, string qualifiedClassName, int file)
		{
			// Note : There may be more than one file; e.g. in the case of partial classes
			// For riskhotspots the file parameter is available ( otherwise is -1 )

			var package = coverageReport
				.Packages.Package
				.SingleOrDefault(x => x.Name.Equals(assemblyName));

			if (package == null)
			{
				return new string[0];
			}

			var classes = package
				.Classes.Class
				.Where(x => x.Name.Equals(qualifiedClassName));

			if (file != -1)
			{
				classes = new List<Class> { classes.ElementAt(file) };
			}

			var classFiles = classes
				.Select(x => x.Filename)
				.ToArray();

			return classFiles;
		}

        
    }
}