using System.Collections.Generic;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Core.Cobertura
{
    [XmlRoot(ElementName = "lines")]
    public class Lines
    {
        [XmlElement(ElementName = "line")]
        public List<Line> Line { get; set; }
    }
}