using FineCodeCoverage.Core.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace FineCodeCoverage.Core.Cobertura
{
	public class CoberturaService : ICoberturaService
	{
		private static readonly XmlSerializer SERIALIZER = new XmlSerializer(typeof(CoverageReport));
		private static readonly XmlReaderSettings READER_SETTINGS = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };

		public List<CoverageReport> LoadReportFiles(IEnumerable<string> inputFilePaths)
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

		public CoverageReport LoadReportFile(string inputFilePath)
		{
			using var reader = XmlReader.Create(inputFilePath, READER_SETTINGS);
			var report = (CoverageReport)SERIALIZER.Deserialize(reader);
			return report;
		}

		public async Task CoverageXmlFileToJsonFileAsync(string xmlFile, string jsonFile, bool formattedJson = false)
		{
			var xmlText = await File.ReadAllTextAsync(xmlFile);
			var jsonText = CoverageXmlTextToJsonText(xmlText, formattedJson);
			File.WriteAllText(jsonFile, jsonText);
		}

		public string CoverageXmlTextToJsonText(string xmlText, bool formattedJson = false)
		{
			long count = 0;

			var xmlLines = xmlText
				.Split('\r', '\n')
				.Select(x => x.Trim())
				.Where(x => !x.StartsWith("<?xml"))
				.Where(x => !x.StartsWith("<!DOCTYPE"))
				.Where(x => !x.StartsWith("<sources>") && !x.StartsWith("</sources>") && !x.StartsWith("<source>"))
				.Where(x => !x.StartsWith("<packages>") && !x.StartsWith("</packages>"))
				.Where(x => !x.StartsWith("<classes>") && !x.StartsWith("</classes>"))
				.Where(x => !x.StartsWith("<methods>") && !x.StartsWith("</methods>"))
				.Where(x => !x.StartsWith("<lines>") && !x.StartsWith("</lines>"))
				.Select(x => x
					.Replace("<package ", $"<packages id=\"p{count++}\" json:Array='true' ").Replace("</package>", "</packages>")
					.Replace("<class ", $"<classes id=\"c{count++}\" json:Array='true' ").Replace("</class>", "</classes>")
					.Replace("<method ", $"<methods id=\"m{count++}\" json:Array='true' ").Replace("</method>", "</methods>")
					.Replace("<line ", $"<lines id=\"l{count++}\" json:Array='true' ").Replace("</line>", "</lines>")
				);

			var processedXmlText = string
				.Join(Environment.NewLine, xmlLines)
				.Replace("<coverage ", "<coverage xmlns:json='http://james.newtonking.com/projects/json' ");

			var xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(processedXmlText);

			string jsonText = JsonConvert
				.SerializeXmlNode(xmlDocument, formattedJson ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None, true)
				.Replace("\"@", "\"");

			return jsonText;
		}

		public CoverageReport ProcessCoberturaXmlFile(string xmlFilePath, out List<CoverageLine> coverageLines)
		{
			coverageLines = new List<CoverageLine>();

			var report = LoadReportFile(xmlFilePath);

			foreach (var package in report.Packages.Package)
			{
				foreach (var classs in package.Classes.Class)
				{
					foreach (var line in classs.Lines.Line)
					{
						coverageLines.Add(new CoverageLine
						{
							Package = package,
							Class = classs,
							Line = line
						});
					}
				}
			}

			return report;
		}
	}
}