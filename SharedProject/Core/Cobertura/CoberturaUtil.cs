using System.Linq;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine.Cobertura
{
    [Export(typeof(ICoberturaUtil))]
	internal class CoberturaUtil:ICoberturaUtil
    {
        private readonly ICoberturaDeserializer coberturaDeserializer;
        private readonly IFileLineCoverageFactory fileLineCoverageFactory;
        private CoverageReport coverageReport;
        private IFileLineCoverage fileLineCoverage;

        private class FileLine : ILine
        {
			public FileLine(Line line)
			{
                CoverageType = GetCoverageType(line);
				Number = line.Number;
            }

            private static CoverageType GetCoverageType(Line line)
            {
                var lineConditionCoverage = line.ConditionCoverage?.Trim();

                var coverageType = CoverageType.NotCovered;

                if (line.Hits > 0)
                {
                    coverageType = CoverageType.Covered;

                    if (!string.IsNullOrWhiteSpace(lineConditionCoverage) && !lineConditionCoverage.StartsWith("100"))
                    {
                        coverageType = CoverageType.Partial;
                    }
                }
                return coverageType;
            }
            public int Number { get; }
            public CoverageType CoverageType { get; }
        }

        [ImportingConstructor]
		public CoberturaUtil(
			ICoberturaDeserializer coberturaDeserializer,
            IFileRenameListener fileRenameListener,
			IFileLineCoverageFactory fileLineCoverageFactory
        )
		{
            fileRenameListener.ListenForFileRename((oldFile, newFile) =>
            {
                fileLineCoverage?.UpdateRenamed(oldFile, newFile);
            });
            this.coberturaDeserializer = coberturaDeserializer;
            this.fileLineCoverageFactory = fileLineCoverageFactory;
        }


		public IFileLineCoverage ProcessCoberturaXml(string xmlFile)
		{
			fileLineCoverage = fileLineCoverageFactory.Create();

			coverageReport = coberturaDeserializer.Deserialize(xmlFile);

            AddThenSort();
            return fileLineCoverage;
		}

        private void AddThenSort()
        {
            foreach (var package in coverageReport.Packages)
            {
                foreach (var classs in package.Classes)
                {
                    fileLineCoverage.Add(classs.Filename, classs.Lines.Select(l => new FileLine(l)).Cast<ILine>());
                }
            }

            fileLineCoverage.Sort();
        }

		private Package GetPackage(string assemblyName)
		{
            return coverageReport.Packages.SingleOrDefault(package => package.Name.Equals(assemblyName));
        }

		public string[] GetSourceFiles(string assemblyName, string qualifiedClassName, int file)
		{
			// Note : There may be more than one file; e.g. in the case of partial classes
			// For riskhotspots the file parameter is available ( otherwise is -1 )

			var package = GetPackage(assemblyName);
            return package == null ? new string[0] : GetSourceFilesFromPackage(package, qualifiedClassName, file);
		}

		private static string[] GetSourceFilesFromPackage(Package package, string qualifiedClassName, int file)
		{
            var classes = GetClasses(package, qualifiedClassName);
            return GetSourceFiles(classes, file);
        }

        private static IEnumerable<Class> GetClasses(Package package, string qualifiedClassName)
        {
            return package.Classes.Where(x => x.Name.Equals(qualifiedClassName));
        }

        private static string[] GetSourceFiles(IEnumerable<Class> classes, int file)
        {
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