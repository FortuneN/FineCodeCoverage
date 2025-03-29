using System;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    internal class TUnitSettings
    {
        public TUnitSettings(
            string exePath,
            string settingsPath,
            string outputPath,
            string additionalArgs
            )
        {
            ExePath = exePath;
            SettingsPath = settingsPath;
            OutputPath = outputPath;
            AdditionalArgs = additionalArgs;
        }

        public string ExePath { get; }
        public string SettingsPath { get; }
        public string OutputPath { get; }
        public string AdditionalArgs { get; }
    }

    internal interface ITUnitCoverageRunner
    {
        event EventHandler ReadyEvent;
        void Initialize(string appDataFolderPath, CancellationToken cancellationToken);
        Task<bool> RunAsync(
            TUnitSettings tUnitSettings,
            bool hasCoverageExtension,
            bool showWindow = false,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
