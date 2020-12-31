using System.Xml.Serialization;

// Generated from cobertura XML schema


namespace FineCodeCoverage.Core.Cobertura
{
    [XmlRoot(ElementName = "sources")]
    public class Sources
    {
        [XmlElement(ElementName = "source")]
        public string Source { get; set; }
    }
}