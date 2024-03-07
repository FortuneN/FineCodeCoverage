using System.Xml.Serialization;
using System.Xml;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Engine.Cobertura
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ICoberturaDeserializer))]
    internal class CoberturaDerializer : ICoberturaDeserializer
    {
        private readonly XmlSerializer xmlSerializer = new XmlSerializer(typeof(CoverageReport));
        private readonly XmlReaderSettings xmlReaderSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        public CoverageReport Deserialize(string xmlFile)
        {
            using (var reader = XmlReader.Create(xmlFile, xmlReaderSettings))
            {
                var report = (CoverageReport)xmlSerializer.Deserialize(reader);
                return report;
            }
        }
    }
}
