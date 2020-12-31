using System.Collections.Generic;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Core.Cobertura
{
    [XmlRoot(ElementName = "conditions")]
    public class Conditions
    {
        [XmlElement(ElementName = "condition")]
        public List<Condition> Condition { get; set; }
    }
}