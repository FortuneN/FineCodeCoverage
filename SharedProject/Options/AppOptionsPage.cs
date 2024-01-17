using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using Microsoft;
using System;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE80;

namespace FineCodeCoverage.Options
{
    internal class AppOptionsPage : DialogPage, IAppOptions
    {
        private const string oldRunCategory = "Run ( Coverlet / OpenCover )";
        private const string commonRunCategory = "Run ( Common )";
        private const string commonEnvironmentCategory = "Environment ( Common )";
        private const string coverletExcludeIncludeCategory = "Exclude / Include ( Coverlet )";
        private const string oldExcludeIncludeCategory = "Exclude / Include ( Coverlet / OpenCover )";
        private const string commonExcludeIncludeCategory = "Exclude / Include ( Common )";
        private const string msExcludeIncludeCategory = "Exclude / Include ( Microsoft )";
        private const string coverletToolCategory = "Tool ( Coverlet )";
        private const string openCoverToolCategory = "Tool ( OpenCover )";
        private const string oldOutputCategory = "Output ( Coverlet / OpenCover )";
        private const string commonOutputCategory = "Output ( Common )";
        private const string commonReportCategory = "Report ( Common )";
        private const string openCoverReportCategory = "Report ( OpenCover )";
        private const string commonUiCategory = "UI ( Common )";
        
        private static readonly Lazy<IAppOptionsStorageProvider> lazyAppOptionsStorageProvider = new Lazy<IAppOptionsStorageProvider>(GetAppOptionsStorageProvider);

        private static IAppOptionsStorageProvider GetAppOptionsStorageProvider()
        {
            IAppOptionsStorageProvider appOptionsStorageProvider = null;
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(SDTE));
                var sp = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
                var componentModel = sp.GetService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel)) as Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
                Assumes.Present(componentModel);
                appOptionsStorageProvider = componentModel.GetService<IAppOptionsStorageProvider>();
            });
            return appOptionsStorageProvider;
        }

        #region run
        #region common run category
        [Category(commonRunCategory)]
        [Description("Specifies whether or not coverage output is enabled")]
        public bool Enabled { get; set; }

        [Category(commonRunCategory)]
        [Description("Set to false for VS Option Enabled=false to not disable coverage")]
        public bool DisabledNoCoverage { get; set; }

        [Category(commonRunCategory)]
        [Description("Specifies whether or not the ms code coverage is used (BETA).  No, IfInRunSettings, Yes")]
        public RunMsCodeCoverage RunMsCodeCoverage { get; set; }

        [Description("Specify false to prevent coverage when tests fail.  Cannot be used in conjunction with RunInParallel")]
        [Category(commonRunCategory)]
        public bool RunWhenTestsFail { get; set; }

        [Description("Specify a value to only run coverage based upon the number of executing tests.  Cannot be used in conjunction with RunInParallel")]
        [Category(commonRunCategory)]
        public int RunWhenTestsExceed { get; set; }
        #endregion

        #region old run
        [Description("Specify true to not wait for tests to finish before running OpenCover / Coverlet coverage")]
        [Category(oldRunCategory)]
        public bool RunInParallel { get; set; }
        #endregion
        #endregion

        #region exclude / include
        #region common exclude include
        [Category(commonExcludeIncludeCategory)]
        [Description("Set to true to add all referenced projects to Include.")]
        public bool IncludeReferencedProjects { get; set; }

        [Category(commonExcludeIncludeCategory)]
        [Description(
        @"Specifies whether to report code coverage of the test assembly
		")]
        public bool IncludeTestAssembly { get; set; }

        [Category(commonExcludeIncludeCategory)]
        [Description(
        @"Provide a list of assemblies to exclude from coverage.  The dll name without extension is used for matching.
		")]
        public string[] ExcludeAssemblies { get; set; }

        [Category(commonExcludeIncludeCategory)]
        [Description(
        @"Provide a list of assemblies to include in coverage. The dll name without extension is used for matching.
		")]
        public string[] IncludeAssemblies { get; set; }
        #endregion

        #region old exclude include
        [Category(oldExcludeIncludeCategory)]
        [Description(
        @"Filter expressions to exclude specific modules and types (multiple)
		
		Wildcards
		* => matches zero or more characters
		
		Examples
		[*]* => Excludes all types in all assemblies (nothing is instrumented)
		[coverlet.*]Coverlet.Core.Coverage => Excludes the Coverage class in the Coverlet.Core namespace belonging to any assembly that matches coverlet.* (e.g coverlet.core)
		[*]Coverlet.Core.Instrumentation.* => Excludes all types belonging to Coverlet.Core.Instrumentation namespace in any assembly
		[coverlet.*.tests]* => Excludes all types in any assembly starting with coverlet. and ending with .tests
		
		Both 'Exclude' and 'Include' options can be used together but 'Exclude' takes precedence.
		")]
        public string[] Exclude { get; set; }

        [Category(oldExcludeIncludeCategory)]
        [Description(
        @"Filter expressions to include specific modules and types (multiple)
		
		Wildcards
		* => matches zero or more characters
		
		Examples
		[*]*"" => Includes all types in all assemblies (nothing is instrumented)
		[coverlet.*]Coverlet.Core.Coverage => Includes the Coverage class in the Coverlet.Core namespace belonging to any assembly that matches coverlet.* (e.g coverlet.core)
		[*]Coverlet.Core.Instrumentation.* => Includes all types belonging to Coverlet.Core.Instrumentation namespace in any assembly
		[coverlet.*.tests]* => Includes all types in any assembly starting with coverlet. and ending with .tests
		
		Both 'Exclude' and 'Include' options can be used together but 'Exclude' takes precedence.
		")]
        public string[] Include { get; set; }

        [Category(oldExcludeIncludeCategory)]
        [Description(
        @"Glob patterns specifying source files to exclude (multiple)
		Use file path or directory path with globbing (e.g. **/Migrations/*)
		")]
        public string[] ExcludeByFile { get; set; }

        [Category(oldExcludeIncludeCategory)]
        [Description(
        @"Attributes to exclude from code coverage (multiple)

		You can ignore a method or an entire class from code coverage by creating and applying the [ExcludeFromCodeCoverage] attribute present in the System.Diagnostics.CodeAnalysis namespace.
		You can also ignore additional attributes by adding to this list (short name or full name supported) e.g. :
		[GeneratedCode] => Present in the System.CodeDom.Compiler namespace
		[MyCustomExcludeFromCodeCoverage] => Any custom attribute that you may define
		")]
        public string[] ExcludeByAttribute { get; set; }
        #endregion

        #region ms exclude include
        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies specified by assembly name or file path - for exclusion")]
        public string[] ModulePathsExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies specified by assembly name or file path - for inclusion")]
        public string[] ModulePathsInclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies by the Company attribute - for exclusion")]
        public string[] CompanyNamesExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies by the Company attribute - for inclusion")]
        public string[] CompanyNamesInclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies by the public key token - for exclusion")]
        public string[] PublicKeyTokensExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies by the public key token - for inclusion")]
        public string[] PublicKeyTokensInclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match elements by the path name of the source file in which they're defined - for exclusion")]
        public string[] SourcesExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match elements by the path name of the source file in which they're defined - for inclusion")]
        public string[] SourcesInclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match elements that have the specified attribute by full name - for exclusion")]
        public string[] AttributesExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match elements that have the specified attribute by full name - for inclusion")]
        public string[] AttributesInclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match procedures, functions, or methods by fully qualified name, including the parameter list. - for exclusion")]
        public string[] FunctionsExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match procedures, functions, or methods by fully qualified name, including the parameter list. - for inclusion")]
        public string[] FunctionsInclude { get; set; }
        #endregion

        #region coverlet only 
        [Description("Specify false for global and project options to be used for coverlet data collector configuration elements when not specified in runsettings")]
        [Category(coverletExcludeIncludeCategory)]
        public bool RunSettingsOnly { get; set; }
        #endregion
        #endregion

        #region output
        #region common output
        [Description("To have fcc output visible in a sub folder of your solution provide this name")]
        [Category(commonOutputCategory)]
        public string FCCSolutionOutputDirectoryName { get; set; }
        #endregion

        #region old output
        [Description("If your tests are dependent upon their path set this to true. OpenCover / Coverlet")]
        [Category(oldOutputCategory)]
        public bool AdjacentBuildOutput { get; set; }
        #endregion
        #endregion

        #region common environment
        [Description("Folder to which copy tools subfolder. Must alredy exist. Requires restart of VS.")]
        [Category(commonEnvironmentCategory)]
        public string ToolsDirectory { get; set; }
        #endregion

        #region common ui
        [Category(commonUiCategory)]
        [Description("Use Environment / Fonts and Colors for editor Coverage colouring")]
        public bool CoverageColoursFromFontsAndColours { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent coverage marks in the overview margin")]
        public bool ShowCoverageInOverviewMargin { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent covered marks in the overview margin")]
        public bool ShowCoveredInOverviewMargin { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent uncovered marks in the overview margin")]
        public bool ShowUncoveredInOverviewMargin { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent partially covered marks in the overview margin")]
        public bool ShowPartiallyCoveredInOverviewMargin { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to hide the toolbar on the report tool window")]
        public bool ShowToolWindowToolbar { get; set; }
        #endregion

        #region common report category
        [Category(commonReportCategory)]
        [Description("When cyclomatic complexity exceeds this value for a method then the method will be present in the risk hotspots tab.")]
        public int ThresholdForCyclomaticComplexity { get; set; }

        [Category(commonReportCategory)]
        [Description("Set to true for coverage table to have a sticky thead.")]
        public bool StickyCoverageTable { get; set; }

        [Category(commonReportCategory)]
        [Description("Set to false to show types in report in short form.")]
        public bool NamespacedClasses { get; set; }

        [Category(commonReportCategory)]
        [Description("Control qualification of types when NamespacedClasses is true.")]
        public NamespaceQualification NamespaceQualification { get; set; }

        [Category(commonReportCategory)]
        [Description("Set to true to hide classes, namespaces and assemblies that are fully covered.")]
        public bool HideFullyCovered { get; set; }

        [Category(commonReportCategory)]
        [Description("Set to false to show classes, namespaces and assemblies that are not coverable.")]
        public bool Hide0Coverable { get; set; }

        [Category(commonReportCategory)]
        [Description("Set to true to hide classes, namespaces and assemblies that have 0% coverage.")]
        public bool Hide0Coverage { get; set; }
        #endregion

        #region OpenCover report category
        [Category(openCoverReportCategory)]
        [Description("When npath complexity exceeds this value for a method then the method will be present in the risk hotspots tab. OpenCover only")]
        public int ThresholdForNPathComplexity { get; set; }

        [Category(openCoverReportCategory)]
        [Description("When crap score exceeds this value for a method then the method will be present in the risk hotspots tab. OpenCover only")]
        public int ThresholdForCrapScore { get; set; }
        #endregion

        #region coverlet tool only
        [Description("Specify true to use your own dotnet tools global install of coverlet console.")]
        [Category(coverletToolCategory)]
        public bool CoverletConsoleGlobal { get; set; }

        [Description("Specify true to use your own dotnet tools local install of coverlet console.")]
        [Category(coverletToolCategory)]
        public bool CoverletConsoleLocal { get; set; }

        [Description("Specify path to coverlet console exe if you need functionality that the FCC version does not provide.")]
        [Category(coverletToolCategory)]
        public string CoverletConsoleCustomPath { get; set; }

        [Description("Specify path to directory containing coverlet collector files if you need functionality that the FCC version does not provide.")]
        [Category(coverletToolCategory)]
        public string CoverletCollectorDirectoryPath { get; set; }
        #endregion

        #region open cover tool only
        [Description("Specify path to open cover exe if you need functionality that the FCC version does not provide.")]
        [Category(openCoverToolCategory)]
        public string OpenCoverCustomPath { get; set; }

        [Description("Change from Default if FCC determination of path32 or path64 is incorrect.")]
        [Category(openCoverToolCategory)]
        public OpenCoverRegister OpenCoverRegister { get; set; }

        [Category(openCoverToolCategory)]
        [Description("Supply your own target if required.")]
        public string OpenCoverTarget { get; set; }

        [Category(openCoverToolCategory)]
        [Description("If supplying your own target you can also supply additional arguments.  FCC supplies the test dll path.")]
        public string OpenCoverTargetArgs { get; set; }
        #endregion

        public override void SaveSettingsToStorage()
        {
            lazyAppOptionsStorageProvider.Value.SaveSettingsToStorage(this);
        }

        public override void LoadSettingsFromStorage()
        {
            lazyAppOptionsStorageProvider.Value.LoadSettingsFromStorage(this);
        }

    }

}
