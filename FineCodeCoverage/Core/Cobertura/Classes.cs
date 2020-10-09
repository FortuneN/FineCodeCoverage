using System.Collections.Generic;
using System.Xml.Serialization;

// Generated from cobertura XML schema
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "classes")]
    public class Classes
    {
        [XmlElement(ElementName = "class")]
        public List<Class> Class { get; set; }
    }
}