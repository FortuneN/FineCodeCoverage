using FineCodeCoverage.Impl;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "line")]
    [ExcludeFromCodeCoverage]
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

        private CoverageType? coverageType;
        public CoverageType CoverageType
        {
            get
            {
                if(coverageType == null)
                {
                    
                    var lineConditionCoverage = ConditionCoverage?.Trim();

                    coverageType = CoverageType.NotCovered;

                    if (Hits > 0)
                    {
                        coverageType = CoverageType.Covered;

                        if (!string.IsNullOrWhiteSpace(lineConditionCoverage) && !lineConditionCoverage.StartsWith("100"))
                        {
                            coverageType = CoverageType.Partial;
                        }
                    }
                }
                return coverageType.Value;
            }
        }
        
    }
}