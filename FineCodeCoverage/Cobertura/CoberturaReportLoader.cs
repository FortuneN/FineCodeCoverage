using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace FineCodeCoverage.Cobertura
{
	/// <summary>
	/// Loads XMLs of type <see cref="CoverageReport"/> from the file system.
	/// </summary>
	public sealed class CoberturaReportLoader
	{
		private static readonly XmlSerializer SERIALIZER = new XmlSerializer(typeof(CoverageReport));
		private static readonly XmlReaderSettings READER_SETTINGS = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };

		public static List<CoverageReport> LoadReportFiles(IEnumerable<string> inputFilePaths)
		{
			if (inputFilePaths == null)
			{
				throw new ArgumentNullException(nameof(inputFilePaths));
			}

			var reports = new List<CoverageReport>();

			foreach (var inputFilePath in inputFilePaths)
			{
				reports.Add(LoadReportFile(inputFilePath));
			}

			return reports;
		}

		public static CoverageReport LoadReportFile(string inputFilePath)
		{
			using (var reader = XmlReader.Create(inputFilePath, READER_SETTINGS))
			{
				var report = (CoverageReport)SERIALIZER.Deserialize(reader);
				return report;
			}
		}
	}
}