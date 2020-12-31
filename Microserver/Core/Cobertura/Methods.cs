using System.Collections.Generic;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Core.Cobertura
{
    [XmlRoot(ElementName = "methods")]
    public class Methods
    {
        [XmlElement(ElementName = "method")]
        public List<Method> Method { get; set; }
    }
}