using System.Xml;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Engine.Cobertura
{

    [Export(typeof(ICoberturaUtil))]
	internal class CoberturaUtil:ICoberturaUtil
	{
		private readonly XmlSerializer SERIALIZER = new XmlSerializer(typeof(CoverageReport));
		private readonly XmlReaderSettings READER_SETTINGS = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
		private CoverageReport coverageReport;
		public List<CoverageLine> CoverageLines { get; private set; }

		private CoverageReport LoadReportFile(string inputFilePath)
		{
			using (var reader = XmlReader.Create(inputFilePath, READER_SETTINGS))
			{
				var report = (CoverageReport)SERIALIZER.Deserialize(reader);
				return report;
			}
		}

		//private void CoverageXmlFileToJsonFile(string xmlFile, string jsonFile, bool formattedJson = false)
		//{
		//	var xmlText = File.ReadAllText(xmlFile);
		//	var jsonText = CoverageXmlTextToJsonText(xmlText, formattedJson);
		//	File.WriteAllText(jsonFile, jsonText);
		//}

		//private string CoverageXmlTextToJsonText(string xmlText, bool formattedJson = false)
		//{
		//	long count = 0;

		//	var xmlLines = xmlText
		//		.Split('\r', '\n')
		//		.Select(x => x.Trim())
		//		.Where(x => !x.StartsWith("<?xml"))
		//		.Where(x => !x.StartsWith("<!DOCTYPE"))
		//		.Where(x => !x.StartsWith("<sources>") && !x.StartsWith("</sources>") && !x.StartsWith("<source>"))
		//		.Where(x => !x.StartsWith("<packages>") && !x.StartsWith("</packages>"))
		//		.Where(x => !x.StartsWith("<classes>") && !x.StartsWith("</classes>"))
		//		.Where(x => !x.StartsWith("<methods>") && !x.StartsWith("</methods>"))
		//		.Where(x => !x.StartsWith("<lines>") && !x.StartsWith("</lines>"))
		//		.Select(x => x
		//			.Replace("<package ", $"<packages id=\"p{count++}\" json:Array='true' ").Replace("</package>", "</packages>")
		//			.Replace("<class ", $"<classes id=\"c{count++}\" json:Array='true' ").Replace("</class>", "</classes>")
		//			.Replace("<method ", $"<methods id=\"m{count++}\" json:Array='true' ").Replace("</method>", "</methods>")
		//			.Replace("<line ", $"<lines id=\"l{count++}\" json:Array='true' ").Replace("</line>", "</lines>")
		//		);

		//	var processedXmlText = string
		//		.Join(Environment.NewLine, xmlLines)
		//		.Replace("<coverage ", "<coverage xmlns:json='http://james.newtonking.com/projects/json' ");

		//	var xmlDocument = new XmlDocument();
		//	xmlDocument.LoadXml(processedXmlText);

		//	string jsonText = JsonConvert
		//		.SerializeXmlNode(xmlDocument, formattedJson ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None, true)
		//		.Replace("\"@", "\"");

		//	return jsonText;
		//}

		public void ProcessCoberturaXmlFile(string xmlFilePath)
		{
			CoverageLines = new List<CoverageLine>();

			coverageReport = LoadReportFile(xmlFilePath);

			foreach (var package in coverageReport.Packages.Package)
			{
				foreach (var classs in package.Classes.Class)
				{
					foreach (var line in classs.Lines.Line)
					{
						CoverageLines.Add(new CoverageLine
						{
							Package = package,
							Class = classs,
							Line = line
						});
					}
				}
			}
		}

		public string[] GetSourceFiles(string assemblyName, string qualifiedClassName)
		{
			// Note : There may be more than one file; e.g. in the case of partial classes

			var package = coverageReport
				.Packages.Package
				.SingleOrDefault(x => x.Name.Equals(assemblyName));

			if (package == null)
			{
				return new string[0];
			}

			var classFiles = package
				.Classes.Class
				.Where(x => x.Name.Equals(qualifiedClassName))
				.Select(x => x.Filename)
				.ToArray();

			return classFiles;
		}
	}
}