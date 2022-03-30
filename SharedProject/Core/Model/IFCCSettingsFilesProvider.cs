using System.Collections.Generic;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.Model
{
    internal interface IFCCSettingsFilesProvider
    {
        List<XElement> Provide(string projectPath);
    }

}
