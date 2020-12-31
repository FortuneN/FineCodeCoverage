using System.Collections.Generic;
using System.Xml.Serialization;

// Generated from cobertura XML schema


namespace FineCodeCoverage.Core.Cobertura
{
    [XmlRoot(ElementName = "classes")]
    public class Classes
    {
        [XmlElement(ElementName = "class")]
        public List<Class> Class { get; set; }
    }
}