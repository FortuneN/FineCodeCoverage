using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "methods")]
    [ExcludeFromCodeCoverage]
    public class Methods
    {
        [XmlElement(ElementName = "method")]
        public List<Method> Method { get; set; }
    }
}