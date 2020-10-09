using System.Xml.Serialization;

// Generated from cobertura XML schema
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "line")]
    public class Line
    {
        [XmlAttribute(AttributeName = "number")]
        public int Number { get; set; }

        [XmlAttribute(AttributeName = "hits")]
        public int Hits { get; set; }

        [XmlAttribute(AttributeName = "branch")]
        public string Branch { get; set; }

        [XmlElement(ElementName = "conditions")]
        public Conditions Conditions { get; set; }

        [XmlAttribute(AttributeName = "condition-coverage")]
        public string ConditionCoverage { get; set; }
    }
}