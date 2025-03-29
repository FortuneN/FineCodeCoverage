using FineCodeCoverage.Core.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel.Composition;
using System.IO;
using FineCodeCoverage.Options;
using System.Collections.Generic;
using System.Text;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    [Export(typeof(ITUnitSettingsProvider))]
    internal class TUnitSettingsProvider : ITUnitSettingsProvider
    {
        private readonly IFileUtil fileUtil;
        private readonly IXmlUtils xmlUtils;
        private readonly IRunSettingsToConfiguration runSettingsToConfiguration;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly IEnvironment environment;
        private int fccRunWhenTestsExceed;
        private bool fccRunWhenTestsFail;

        [ImportingConstructor]
        public TUnitSettingsProvider(
            IFileUtil fileUtil,
            IXmlUtils xmlUtils,
            IRunSettingsToConfiguration runSettingsToConfiguration,
            IAppOptionsProvider appOptionsProvider,
            IEnvironment environment
        )
        {
            this.fileUtil = fileUtil;
            this.xmlUtils = xmlUtils;
            this.runSettingsToConfiguration = runSettingsToConfiguration;
            this.appOptionsProvider = appOptionsProvider;
            this.environment = environment;
            TakeFCCOptions(appOptionsProvider.Get());
            this.appOptionsProvider.OptionsChanged += TakeFCCOptions;
        }

        private void TakeFCCOptions(IAppOptions appOptions)
        {
            this.fccRunWhenTestsExceed = appOptions.RunWhenTestsExceed;
            this.fccRunWhenTestsFail = appOptions.RunWhenTestsFail;
        }

        public async Task<TUnitSettings> ProvideAsync(ITUnitCoverageProject tUnitCoverageProject, CancellationToken cancellationToken)
        {
            await tUnitCoverageProject.CoverageProject.PrepareForCoverageAsync(cancellationToken, false);
            var coberturaPath = GetCoberturaPath(tUnitCoverageProject);
            var commandLineParseResult = tUnitCoverageProject.CommandLineParseResult;
            // todo commandLineParseResult.HasError
            string configurationPathArgument = null;
            var additionalArgsStringBuilder = new StringBuilder();
            string ignoreExitCodeArg = null;
            int? minimumExpectedTests = null;
            foreach (var option in commandLineParseResult.Options)
            {
                switch (option.Name)
                {
                    case "coverage":
                    case "coverage-output-format":
                    case "coverage-output"://for now will use own
                        break;
                    case "coverage-settings":
                    case "settings":
                        var arg = option.Arguments.FirstOrDefault();
                        if (arg != null)
                        {
                            if (ConfigurationPathArgExists(arg))
                            {
                                configurationPathArgument = arg;
                            }
                        }
                        break;
                    case "ignore-exit-code":
                        ignoreExitCodeArg = option.Arguments.FirstOrDefault();
                        break;
                    case "minimum-expected-tests":
                        var minExpectedTestsArg = option.Arguments.FirstOrDefault();
                        if (minExpectedTestsArg != null)
                        {
                            if(int.TryParse(minExpectedTestsArg, out var result))
                            {
                                minimumExpectedTests = result;
                            }
                        }
                        break;
                    default:
                        AddToAdditionalArgs($"--{option.Name} {string.Join(" ", option.Arguments)}");
                        break;
                }
            }

            AddToAdditionalArgs(GetMinimumExpectedTestsPart(minimumExpectedTests));
            AddToAdditionalArgs(GetIgnoreExitCodePart(ignoreExitCodeArg));

            var configurationPath = await GetConfigurationPathAsync(tUnitCoverageProject, configurationPathArgument, cancellationToken);
            return new TUnitSettings(tUnitCoverageProject.ExePath, configurationPath, coberturaPath, additionalArgsStringBuilder.ToString());

            bool ConfigurationPathArgExists(string pathArg)
            {
                pathArg = pathArg.Replace("\"", "").Replace("'", "");
                return fileUtil.Exists(pathArg);
            }

            void AddToAdditionalArgs(string part)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    additionalArgsStringBuilder.Append($" {part}");
                }
            }
        }

        private string GetMinimumExpectedTestsPart(int? minimumExpectedTestsArg)
        {
            // non zero positive integer
            if (!minimumExpectedTestsArg.HasValue && fccRunWhenTestsExceed > 1)
            {
                minimumExpectedTestsArg = fccRunWhenTestsExceed - 1;
            }
            return minimumExpectedTestsArg.HasValue ? $"--minimum-expected-tests {minimumExpectedTestsArg}" : null;
        }

        private string GetIgnoreExitCodePart(string ignoreExitCodeArg)
        {
            var ignoreExitCodeString = GetIgnoreExitCodeString(ignoreExitCodeArg);
            var ignoredExitCodes = GetIgnoredExitCodes(ignoreExitCodeString);
            if(!ignoredExitCodes.Contains(2) && fccRunWhenTestsFail)
            {
                ignoredExitCodes.Add(2);
            }
            return ignoredExitCodes.Any() ? $"--ignore-exit-code {string.Join(";", ignoredExitCodes)}" : null;
        }

        private string GetIgnoreExitCodeString(string ignoreExitCodesArg)
        {
            var environmentVariableValue = environment.GetEnvironmentVariable("TESTINGPLATFORM_EXITCODE_IGNORE");
            return environmentVariableValue ?? ignoreExitCodesArg ?? "";
        }

        private List<int> GetIgnoredExitCodes(string exitCodes)
        {
            try
            {
                var codes = exitCodes.Split(';');
                return codes.Select(code => int.Parse(code)).ToList();
            }
            catch
            {
                return Enumerable.Empty<int>().ToList();
            }
        }

        private async Task<string> GetConfigurationPathAsync(
            ITUnitCoverageProject tUnitCoverageProject,
            string configurationPathArgument,
            CancellationToken cancellationToken
        )
        {
            if (configurationPathArgument != null)
            {
                if (tUnitCoverageProject.HasCoverageExtension)
                {
                    return configurationPathArgument;
                }

                var configurationOrRunSettingsElement = xmlUtils.Load(configurationPathArgument);
                var name = configurationOrRunSettingsElement.Name.LocalName;
                if (name == "Configuration") return configurationPathArgument;
                if (name == "RunSettings")
                {
                    var configurationElement = runSettingsToConfiguration.ConvertToConfiguration(configurationOrRunSettingsElement);
                    if (configurationElement != null)
                    {
                        return WriteConfiguration(tUnitCoverageProject, xmlUtils.Serialize(configurationElement));
                    }
                }
            }

            return await WriteFCCConfigurationAsync(tUnitCoverageProject, cancellationToken);
        }

        private async Task<string> WriteFCCConfigurationAsync(ITUnitCoverageProject tUnitCoverageProject, CancellationToken cancellationToken)
        {
            var configuration = await tUnitCoverageProject.GetConfigurationAsync(cancellationToken);
            return WriteConfiguration(tUnitCoverageProject, configuration);
        }

        private string WriteConfiguration(ITUnitCoverageProject tUnitCoverageProject, string configuration)
        {
            var coverageProject = tUnitCoverageProject.CoverageProject;
            var configurationPath = Path.Combine(coverageProject.CoverageOutputFolder, coverageProject.Id.ToString() + "config.xml");
            fileUtil.WriteAllText(configurationPath, configuration);
            return configurationPath;
        }

        private static string GetCoberturaPath(ITUnitCoverageProject tUnitCoverageProject)
        {
            var coverageProject = tUnitCoverageProject.CoverageProject;
            return Path.Combine(coverageProject.CoverageOutputFolder, coverageProject.Id.ToString() + "coverage.xml");
        }

    }


}
