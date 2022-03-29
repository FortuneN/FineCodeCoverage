using System;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface IVsRunSettingsWriter
    {
        Task<bool> RemoveRunSettingsFilePathAsync(Guid projectGuid);
        Task<bool> WriteRunSettingsFilePathAsync(Guid projectGuid, string projectRunSettingsFilePath);
    }
}
