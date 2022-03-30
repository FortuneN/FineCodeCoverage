using System;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.Model
{
    internal interface IVsBuildFCCSettingsProvider
    {
        Task<XElement> GetSettingsAsync(Guid projectId);
    }
}
