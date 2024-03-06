using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "class")]
    [ExcludeFromCodeCoverage]
    public class Class
    {
        [XmlArray(ElementName = "methods")]
        [XmlArrayItem(ElementName = "method")]
        public List<Method> Methods { get; set; }

        [XmlArray(ElementName = "lines")]
        [XmlArrayItem(ElementName = "line")]
        public List<Line> Lines { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "filename")]
        public string Filename { get; set; }

        [XmlAttribute(AttributeName = "line-rate")]
        public float LineRate { get; set; }

        [XmlAttribute(AttributeName = "branch-rate")]
        public float BranchRate { get; set; }

        [XmlAttribute(AttributeName = "complexity")]
        public float Complexity { get; set; }
    }
}