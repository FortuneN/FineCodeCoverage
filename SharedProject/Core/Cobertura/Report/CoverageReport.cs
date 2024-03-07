using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "coverage")]
    [ExcludeFromCodeCoverage]
    public class CoverageReport
    {
        [XmlElement(ElementName = "sources")]
        public Sources Sources { get; set; }

        [XmlArray(ElementName = "packages")]
        [XmlArrayItem(ElementName = "package")]
        public List<Package> Packages { get; set; }

        [XmlAttribute(AttributeName = "line-rate")]
        public float LineRate { get; set; }

        [XmlAttribute(AttributeName = "branch-rate")]
        public float BranchRate { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }

        [XmlAttribute(AttributeName = "timestamp")]
        public string Timestamp { get; set; }

        [XmlAttribute(AttributeName = "lines-covered")]
        public int LinesCovered { get; set; }

        [XmlAttribute(AttributeName = "lines-valid")]
        public int LinesValid { get; set; }

        [XmlAttribute(AttributeName = "branches-covered")]
        public int BranchesCovered { get; set; }

        [XmlAttribute(AttributeName = "branches-valid")]
        public int BranchesValid { get; set; }
    }
}