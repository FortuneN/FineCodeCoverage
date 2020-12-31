using System.Xml.Serialization;

// Generated from cobertura XML schema


namespace FineCodeCoverage.Core.Cobertura
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