using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface IVsRunSettingsWriter
    {
        Task RemoveRunSettingsFilePathAsync(Guid projectGuid);
        Task<bool> WriteRunSettingsFilePathAsync(Guid projectGuid, string projectRunSettingsFilePath);
    }
}
