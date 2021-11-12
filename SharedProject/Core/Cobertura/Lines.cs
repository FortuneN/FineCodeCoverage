using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "lines")]
    [ExcludeFromCodeCoverage]
    public class Lines
    {
        [XmlElement(ElementName = "line")]
        public List<Line> Line { get; set; }
    }
}