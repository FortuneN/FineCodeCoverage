using FineCodeCoverage.Options;
using System.Collections.Generic;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.Model
{
    internal interface ISettingsMerger
    {
        IAppOptions Merge(
            IAppOptions globalOptions, 
            List<XElement> settingsFileElements, 
            XElement projectSettingsElement
        );
    }
}
