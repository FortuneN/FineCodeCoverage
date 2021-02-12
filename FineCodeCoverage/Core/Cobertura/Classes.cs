using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "classes")]
    [ExcludeFromCodeCoverage]
    public class Classes
    {
        [XmlElement(ElementName = "class")]
        public List<Class> Class { get; set; }
    }
}