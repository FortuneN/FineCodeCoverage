using System.Collections.Generic;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Core.Cobertura
{
    [XmlRoot(ElementName = "packages")]
    public class Packages
    {
        [XmlElement(ElementName = "package")]
        public List<Package> Package { get; set; }
    }
}