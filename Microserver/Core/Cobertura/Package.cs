using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Core.Cobertura
{
    [XmlRoot(ElementName = "package")]
    public class Package
    {
        [XmlElement(ElementName = "classes")]
        public Classes Classes { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "line-rate")]
        public float LineRate { get; set; }

        [XmlAttribute(AttributeName = "branch-rate")]
        public float BranchRate { get; set; }

        [XmlAttribute(AttributeName = "complexity")]
        public float Complexity { get; set; }
    }
}