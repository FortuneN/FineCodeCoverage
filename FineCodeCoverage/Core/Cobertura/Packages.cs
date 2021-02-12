using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

// Generated from cobertura XML schema

namespace FineCodeCoverage.Engine.Cobertura
{
    [XmlRoot(ElementName = "packages")]
    [ExcludeFromCodeCoverage]
    public class Packages
    {
        [XmlElement(ElementName = "package")]
        public List<Package> Package { get; set; }
    }
}