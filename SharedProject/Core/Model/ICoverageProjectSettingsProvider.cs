using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.Model
{
    internal interface ICoverageProjectSettingsProvider
    {
        Task<XElement> ProvideAsync(ICoverageProject coverageProject);
    }

}
