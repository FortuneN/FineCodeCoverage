using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "method")]
    [ExcludeFromCodeCoverage]
    public class Method
    {
        [XmlArray(ElementName = "lines")]
        [XmlArrayItem(ElementName = "line")]
        public List<Line> Lines { get; set; }

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