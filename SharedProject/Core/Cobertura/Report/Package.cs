using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "package")]
    [ExcludeFromCodeCoverage]
    public class Package
    {
        [XmlArray(ElementName = "classes")]
        [XmlArrayItem(ElementName = "class")]
        public List<Class> Classes { get; set; }

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