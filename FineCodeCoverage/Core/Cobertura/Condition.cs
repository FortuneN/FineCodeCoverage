using System.Xml.Serialization;

// Generated from cobertura XML schema
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "condition")]
    public class Condition
    {
        [XmlAttribute(AttributeName = "number")]
        public string Number { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "coverage")]
        public string Coverage { get; set; }
    }
}