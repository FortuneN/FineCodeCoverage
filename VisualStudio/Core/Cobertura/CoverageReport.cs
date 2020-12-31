using System.Xml.Serialization;

// Generated from cobertura XML schema


namespace FineCodeCoverage.Core.Cobertura
{
    [XmlRoot(ElementName = "coverage")]
    public class CoverageReport
    {
        [XmlElement(ElementName = "sources")]
        public Sources Sources { get; set; }

        [XmlElement(ElementName = "packages")]
        public Packages Packages { get; set; }

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