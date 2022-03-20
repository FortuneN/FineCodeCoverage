using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics.CodeAnalysis;
using EnvDTE;
using Microsoft;

namespace FineCodeCoverage.Options
{
    internal class AppOptions : DialogPage, IAppOptions
    {
        private const string runCategory = "Run";
        private const string environmentCategory = "Environment";
        private const string excludeIncludeCategory = "Exclude / Include";
        private const string coverletCategory = "Coverlet";
        private const string openCoverCategory = "OpenCover";
        private const string outputCategory = "Output";
        private const string reportCategory = "Report";
        private const string uiCategory = "UI";

        public AppOptions():this(false)
        {
            
        }
        internal AppOptions(bool isReadOnly)
        {
            if (!isReadOnly && AppOptionsStorageProvider == null)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var dte = (DTE)ServiceProvider.GlobalProvider.GetService(typeof(DTE));
                    var sp = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
                    var componentModel = sp.GetService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel)) as Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
                    Assumes.Present(componentModel);
                    AppOptionsStorageProvider = componentModel.GetService<IAppOptionsStorageProvider>();
                });
            }
        }
        internal IAppOptionsStorageProvider AppOptionsStorageProvider { get; set; }

        [Category(runCategory)]
        [Description("Specifies whether or not coverage output is enabled")]
        public bool Enabled { get; set; } = true;

        [Category(excludeIncludeCategory)]
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

        [Category(excludeIncludeCategory)]
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

        [Category(excludeIncludeCategory)]
        [Description("Set to true to add all referenced projects to Include.")]
        public bool IncludeReferencedProjects { get; set; }

        [Category(excludeIncludeCategory)]
        [Description(
        @"Glob patterns specifying source files to exclude (multiple)
		Use file path or directory path with globbing (e.g. **/Migrations/*)
		")]
        public string[] ExcludeByFile { get; set; } = new[] { "**/Migrations/*" };

        [Category(excludeIncludeCategory)]
        [Description(
        @"Specifies whether to report code coverage of the test assembly
		")]
        public bool IncludeTestAssembly { get; set; } = true;

        [Category(excludeIncludeCategory)]
        [Description(
        @"Attributes to exclude from code coverage (multiple)

		You can ignore a method or an entire class from code coverage by creating and applying the [ExcludeFromCodeCoverage] attribute present in the System.Diagnostics.CodeAnalysis namespace.
		You can also ignore additional attributes by adding to this list (short name or full name supported) e.g. :
		[GeneratedCode] => Present in the System.CodeDom.Compiler namespace
		[MyCustomExcludeFromCodeCoverage] => Any custom attribute that you may define
		")]
        public string[] ExcludeByAttribute { get; set; } = new[] { "GeneratedCode" };

        [Description("Specify true to not wait for tests to finish before running coverage")]
        [Category(runCategory)]
        public bool RunInParallel { get; set; }

        [Description("Specify false to prevent coverage when tests fail.  Cannot be used in conjunction with RunInParallel")]
        [Category(runCategory)]
        public bool RunWhenTestsFail { get; set; } = true;

        [Description("Specify a value to only run coverage based upon the number of executing tests.  Cannot be used in conjunction with RunInParallel")]
        [Category(runCategory)]
        public int RunWhenTestsExceed { get; set; }

        [Description("Folder to which copy tools subfolder. Must alredy exist. Requires restart of VS.")]
        [Category(environmentCategory)]
        public string ToolsDirectory { get; set; }

        [Description("Specify false for global and project options to be used for coverlet data collector configuration elements when not specified in runsettings")]
        [Category(coverletCategory)]
        public bool RunSettingsOnly { get; set; } = true;

        [Description("Specify true to use your own dotnet tools global install of coverlet console.")]
        [Category(coverletCategory)]
        public bool CoverletConsoleGlobal { get; set; }

        [Description("Specify true to use your own dotnet tools local install of coverlet console.")]
        [Category(coverletCategory)]
        public bool CoverletConsoleLocal { get; set; }

        [Description("Specify path to coverlet console exe if you need functionality that the FCC version does not provide.")]
        [Category(coverletCategory)]
        public string CoverletConsoleCustomPath { get; set; }

        [Description("Specify path to directory containing coverlet collector files if you need functionality that the FCC version does not provide.")]
        [Category(coverletCategory)]
        public string CoverletCollectorDirectoryPath { get; set; }

        [Description("Specify path to open cover exe if you need functionality that the FCC version does not provide.")]
        [Category(openCoverCategory)]
        public string OpenCoverCustomPath { get; set; }

        [Description("To have fcc output visible in a sub folder of your solution provide this name")]
        [Category(outputCategory)]
        public string FCCSolutionOutputDirectoryName { get; set; }

        [Description("If your tests are dependent upon their path set this to true.")]
        [Category(outputCategory)]
        public bool AdjacentBuildOutput { get; set; }

        [Category(reportCategory)]
        [Description("When cyclomatic complexity exceeds this value for a method then the method will be present in the risk hotspots tab.")]
        public int ThresholdForCyclomaticComplexity { get; set; } = 30;

        [Category(reportCategory)]
        [Description("When npath complexity exceeds this value for a method then the method will be present in the risk hotspots tab. OpenCover only")]
        public int ThresholdForNPathComplexity { get; set; } = 200;
        
        [Category(reportCategory)]
        [Description("When crap score exceeds this value for a method then the method will be present in the risk hotspots tab. OpenCover only")]
        public int ThresholdForCrapScore { get; set; } = 15;

        [Category(uiCategory)]
        [Description("Use Environment / Fonts and Colors for editor Coverage colouring")]
        public bool CoverageColoursFromFontsAndColours { get; set; }

        [Category(reportCategory)]
        [Description("Set to true for coverage table to have a sticky thead.")]
        public bool StickyCoverageTable { get; set; }

        [Category(reportCategory)]
        [Description("Set to false to show classes in report in short form.")]
        public bool NamespacedClasses { get; set; } = true;

        [Category(reportCategory)]
        [Description("Set to true to hide classes, namespaces and assemblies that are fully covered.")]
        public bool HideFullyCovered { get; set; }

        public override void SaveSettingsToStorage()
        {
            AppOptionsStorageProvider.SaveSettingsToStorage(this);
        }

        public override void LoadSettingsFromStorage()
        {
            AppOptionsStorageProvider.LoadSettingsFromStorage(this);
        }

    }

}
