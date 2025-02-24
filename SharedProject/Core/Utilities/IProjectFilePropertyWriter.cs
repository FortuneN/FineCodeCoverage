using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
    public interface IProjectFilePropertyWriter
    {
        Task<bool> RemovePropertyAsync(IVsHierarchy pHierProj, string propertyName);
        Task<bool> WritePropertyAsync(IVsHierarchy projectHierarchy, string propertyName, string value);
    }
}
