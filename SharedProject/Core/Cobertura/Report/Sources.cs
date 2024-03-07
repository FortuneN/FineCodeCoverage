using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "sources")]
    [ExcludeFromCodeCoverage]
    public class Sources
    {
        [XmlElement(ElementName = "source")]
        public string Source { get; set; }
    }
}