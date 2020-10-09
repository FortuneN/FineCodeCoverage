using System.Collections.Generic;
using System.Xml.Serialization;

// Generated from cobertura XML schema
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "methods")]
    public class Methods
    {
        [XmlElement(ElementName = "method")]
        public List<Method> Method { get; set; }
    }
}