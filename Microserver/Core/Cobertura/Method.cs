using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Core.Cobertura
{
    [XmlRoot(ElementName = "method")]
    public class Method
    {
        [XmlElement(ElementName = "lines")]
        public Lines Lines { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "signature")]
        public string Signature { get; set; }

        [XmlAttribute(AttributeName = "line-rate")]
        public float LineRate { get; set; }

        [XmlAttribute(AttributeName = "branch-rate")]
        public float BranchRate { get; set; }
    }
}