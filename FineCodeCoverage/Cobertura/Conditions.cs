using System.Collections.Generic;
using System.Xml.Serialization;

// Generated from cobertura XML schema
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace FineCodeCoverage.Cobertura
{
    [XmlRoot(ElementName = "conditions")]
    public class Conditions
    {
        [XmlElement(ElementName = "condition")]
        public List<Condition> Condition { get; set; }
    }
}