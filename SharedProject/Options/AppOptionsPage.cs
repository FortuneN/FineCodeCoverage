using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using Microsoft;
using System;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE80;

namespace FineCodeCoverage.Options
{
    /*
        
        The DialogPage uses a PropertyGrid to display the options.
        The PropertyGrid use TypeDescriptor to get the properties which will use the attributes
        CategoryAttribute, DescriptionAttribute and DisplayNameAttribute to display the options.
        The PropertyGrid by default has PropertySort.CategorizedAlphabetical.
        
    `   todo
         When there is no DisplayNameAttribute applied the property name will be used.
         Property names cannot be changed otherwise the settings will be lost.
         Would like the sub categories ( which are not supported ) to appear together.  
         The simplest method is to apply the DisplayNameAttribute to the property but the property name is used when using xml for settings.
         Could add the setting name in brackets to the DisplayNameAttribute.
         This should be done when making it clear in the readme which options are only allowed in visual studio options.
    */
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
        //[DisplayName("Enabled")]
        public bool Enabled { get; set; }

        [Category(commonRunCategory)]
        [Description("Set to false for VS Option Enabled=false to not disable coverage")]
        //[DisplayName("Disabled No Coverage")]
        public bool DisabledNoCoverage { get; set; }

        [Category(commonRunCategory)]
        [Description("Specifies whether or not the ms code coverage is used (BETA).  No, IfInRunSettings, Yes")]
        //[DisplayName("Run Ms Code Coverage)")]
        public RunMsCodeCoverage RunMsCodeCoverage { get; set; }

        [Description("Specify false to prevent coverage when tests fail.  Cannot be used in conjunction with RunInParallel")]
        [Category(commonRunCategory)]
        //[DisplayName("Run When Tests Fail")]
        public bool RunWhenTestsFail { get; set; }

        [Description("Specify a value to only run coverage based upon the number of executing tests.  Cannot be used in conjunction with RunInParallel")]
        [Category(commonRunCategory)]
        //[DisplayName("Run When Tests Exceed")]
        public int RunWhenTestsExceed { get; set; }
        #endregion

        #region old run
        [Description("Specify true to not wait for tests to finish before running OpenCover / Coverlet coverage")]
        [Category(oldRunCategory)]
        //[DisplayName("Run In Parallel")]
        public bool RunInParallel { get; set; }
        #endregion
        #endregion

        #region exclude / include
        #region common exclude include
        [Category(commonExcludeIncludeCategory)]
        [Description("Set to true to add all referenced projects to Include.")]
        //[DisplayName("Include Referenced Projects")]
        public bool IncludeReferencedProjects { get; set; }

        [Category(commonExcludeIncludeCategory)]
        [Description(
        @"Specifies whether to report code coverage of the test assembly
		")]
        //[DisplayName("Include Test Assembly")]
        public bool IncludeTestAssembly { get; set; }

        [Category(commonExcludeIncludeCategory)]
        [Description(
        @"Provide a list of assemblies to exclude from coverage.  The dll name without extension is used for matching.
		")]
        //[DisplayName("Exclude Assemblies")]
        public string[] ExcludeAssemblies { get; set; }

        [Category(commonExcludeIncludeCategory)]
        [Description(
        @"Provide a list of assemblies to include in coverage. The dll name without extension is used for matching.
		")]
        //[DisplayName("Include Assemblies")]
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
        //[DisplayName("Exclude")]
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
        //[DisplayName("Include")]
        public string[] Include { get; set; }

        [Category(oldExcludeIncludeCategory)]
        [Description(
        @"Glob patterns specifying source files to exclude (multiple)
		Use file path or directory path with globbing (e.g. **/Migrations/*)
		")]
        //[DisplayName("Exclude By File")]
        public string[] ExcludeByFile { get; set; }

        [Category(oldExcludeIncludeCategory)]
        [Description(
        @"Attributes to exclude from code coverage (multiple)

		You can ignore a method or an entire class from code coverage by creating and applying the [ExcludeFromCodeCoverage] attribute present in the System.Diagnostics.CodeAnalysis namespace.
		You can also ignore additional attributes by adding to this list (short name or full name supported) e.g. :
		[GeneratedCode] => Present in the System.CodeDom.Compiler namespace
		[MyCustomExcludeFromCodeCoverage] => Any custom attribute that you may define
		")]
        //[DisplayName("Exclude By Attribute")]
        public string[] ExcludeByAttribute { get; set; }
        #endregion

        #region ms exclude include
        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies specified by assembly name or file path - for exclusion")]
        //[DisplayName("Module Paths Exclude")]
        public string[] ModulePathsExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies specified by assembly name or file path - for inclusion")]
        //[DisplayName("Module Paths Include")]
        public string[] ModulePathsInclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies by the Company attribute - for exclusion")]
        //[DisplayName("Company Names Exclude")]
        public string[] CompanyNamesExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies by the Company attribute - for inclusion")]
        //[DisplayName("Company Names Include")]
        public string[] CompanyNamesInclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies by the public key token - for exclusion")]
        //[DisplayName("Public Key Tokens Exclude")]
        public string[] PublicKeyTokensExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match assemblies by the public key token - for inclusion")]
        //[DisplayName("Public Key Tokens Include")]
        public string[] PublicKeyTokensInclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match elements by the path name of the source file in which they're defined - for exclusion")]
        //[DisplayName("Sources Exclude")]
        public string[] SourcesExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match elements by the path name of the source file in which they're defined - for inclusion")]
        //[DisplayName("Sources Include")]
        public string[] SourcesInclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match elements that have the specified attribute by full name - for exclusion")]
        //[DisplayName("Attributes Exclude")]
        public string[] AttributesExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match elements that have the specified attribute by full name - for inclusion")]
        //[DisplayName("Attributes Include")]
        public string[] AttributesInclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match procedures, functions, or methods by fully qualified name, including the parameter list. - for exclusion")]
        //[DisplayName("Functions Exclude")]
        public string[] FunctionsExclude { get; set; }

        [Category(msExcludeIncludeCategory)]
        [Description("Multiple regexes that match procedures, functions, or methods by fully qualified name, including the parameter list. - for inclusion")]
        //[DisplayName("Functions Include")]
        public string[] FunctionsInclude { get; set; }
        #endregion

        #region coverlet only 
        [Description("Specify false for global and project options to be used for coverlet data collector configuration elements when not specified in runsettings")]
        [Category(coverletExcludeIncludeCategory)]
        //[DisplayName("Run Settings Only")]
        public bool RunSettingsOnly { get; set; }
        #endregion
        #endregion

        #region output
        #region common output
        [Description("To have fcc output visible in a sub folder of your solution provide this name")]
        [Category(commonOutputCategory)]
        //[DisplayName("FCC Solution Output Directory Name")]
        public string FCCSolutionOutputDirectoryName { get; set; }
        #endregion

        #region old output
        [Description("If your tests are dependent upon their path set this to true. OpenCover / Coverlet")]
        [Category(oldOutputCategory)]
        //[DisplayName("Adjacent Build Output")]
        public bool AdjacentBuildOutput { get; set; }
        #endregion
        #endregion

        #region common environment
        [Description("Folder to which copy tools subfolder. Must alredy exist. Requires restart of VS.")]
        [Category(commonEnvironmentCategory)]
        //[DisplayName("Tools Directory")]
        public string ToolsDirectory { get; set; }
        #endregion

        #region common ui

        [Category(commonUiCategory)]
        [Description("Set to false to disable all editor coverage indicators")]
        //[DisplayName("Show Editor Coverage")]
        public bool ShowEditorCoverage { get; set; }
        #region overview margin
        [Category(commonUiCategory)]
        [Description("Set to false to prevent coverage marks in the overview margin")]
        //[DisplayName("Show Overview Margin Coverage")]
        public bool ShowCoverageInOverviewMargin { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent covered marks in the overview margin")]
        //[DisplayName("Show Overview Margin Covered")]
        public bool ShowCoveredInOverviewMargin { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent uncovered marks in the overview margin")]
        //[DisplayName("Show Overview Margin Uncovered")]
        public bool ShowUncoveredInOverviewMargin { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent partially covered marks in the overview margin")]
        //[DisplayName("Show Overview Margin Partially Covered")]
        public bool ShowPartiallyCoveredInOverviewMargin { get; set; }
        #endregion
        #region glyph margin
        [Category(commonUiCategory)]
        [Description("Set to false to prevent coverage marks in the glyph margin")]
        //[DisplayName("Show Glyph Margin Coverage")]
        public bool ShowCoverageInGlyphMargin { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent covered marks in the glyph margin")]
        //[DisplayName("Show Glyph Margin Covered")]
        public bool ShowCoveredInGlyphMargin { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent uncovered marks in the glyph margin")]
        //[DisplayName("Show Glyph Margin Uncovered")]
        public bool ShowUncoveredInGlyphMargin { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent partially covered marks in the glyph margin")]
        //[DisplayName("Show Glyph Margin Partially Covered")]
        public bool ShowPartiallyCoveredInGlyphMargin { get; set; }
        #endregion
        #region line highlighting
        [Category(commonUiCategory)]
        [Description("Set to true to allow coverage line highlighting")]
        //[DisplayName("Show Line Highlighting Coverage")]
        public bool ShowLineCoverageHighlighting { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent covered line highlighting")]
        //[DisplayName("Show Line Highlighting Covered")]
        public bool ShowLineCoveredHighlighting { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent uncovered line highlighting")]
        //[DisplayName("Show Line Highlighting Uncovered")]
        public bool ShowLineUncoveredHighlighting { get; set; }

        [Category(commonUiCategory)]
        [Description("Set to false to prevent partially covered line highlighting")]
        //[DisplayName("Show Line Highlighting Partially Covered")]
        public bool ShowLinePartiallyCoveredHighlighting { get; set; }
        #endregion
        [Category(commonUiCategory)]
        [Description("Set to false to hide the toolbar on the report tool window")]
        //[DisplayName("Show Tool Window Toolbar")]
        public bool ShowToolWindowToolbar { get; set; }
        #endregion

        #region common report category
        [Category(commonReportCategory)]
        [Description("When cyclomatic complexity exceeds this value for a method then the method will be present in the risk hotspots tab.")]
        //[DisplayName("Threshold For Cyclomatic Complexity")]
        public int ThresholdForCyclomaticComplexity { get; set; }

        [Category(commonReportCategory)]
        [Description("Set to true for coverage table to have a sticky thead.")]
        //[DisplayName("Sticky Coverage Table")]
        public bool StickyCoverageTable { get; set; }

        [Category(commonReportCategory)]
        [Description("Set to false to show types in report in short form.")]
        //[DisplayName("Namespaced Classes")]
        public bool NamespacedClasses { get; set; }

        [Category(commonReportCategory)]
        [Description("Control qualification of types when NamespacedClasses is true.")]
        //[DisplayName("Namespace Qualification")]
        public NamespaceQualification NamespaceQualification { get; set; }

        [Category(commonReportCategory)]
        [Description("Set to true to hide classes, namespaces and assemblies that are fully covered.")]
        //[DisplayName("Hide Fully Covered")]
        public bool HideFullyCovered { get; set; }

        [Category(commonReportCategory)]
        [Description("Set to false to show classes, namespaces and assemblies that are not coverable.")]
        //[DisplayName("Hide Not Coverable")]
        public bool Hide0Coverable { get; set; }

        [Category(commonReportCategory)]
        [Description("Set to true to hide classes, namespaces and assemblies that have 0% coverage.")]
        //[DisplayName("Hide 0% Coverage")]
        public bool Hide0Coverage { get; set; }
        #endregion

        #region OpenCover report category
        [Category(openCoverReportCategory)]
        [Description("When npath complexity exceeds this value for a method then the method will be present in the risk hotspots tab. OpenCover only")]
        //[DisplayName("Threshold For NPath Complexity")]
        public int ThresholdForNPathComplexity { get; set; }

        [Category(openCoverReportCategory)]
        [Description("When crap score exceeds this value for a method then the method will be present in the risk hotspots tab. OpenCover only")]
        //[DisplayName("Threshold For Crap Score")]
        public int ThresholdForCrapScore { get; set; }
        #endregion

        #region coverlet tool only
        [Description("Specify true to use your own dotnet tools global install of coverlet console.")]
        [Category(coverletToolCategory)]
        //[DisplayName("Coverlet Console Global")]
        public bool CoverletConsoleGlobal { get; set; }

        [Description("Specify true to use your own dotnet tools local install of coverlet console.")]
        [Category(coverletToolCategory)]
        //[DisplayName("Coverlet Console Local")]
        public bool CoverletConsoleLocal { get; set; }

        [Description("Specify path to coverlet console exe if you need functionality that the FCC version does not provide.")]
        [Category(coverletToolCategory)]
        //[DisplayName("Coverlet Console Custom Path")]
        public string CoverletConsoleCustomPath { get; set; }

        [Description("Specify path to directory containing coverlet collector files if you need functionality that the FCC version does not provide.")]
        [Category(coverletToolCategory)]
        //[DisplayName("Coverlet Collector Directory Path")]
        public string CoverletCollectorDirectoryPath { get; set; }
        #endregion

        #region open cover tool only
        [Description("Specify path to open cover exe if you need functionality that the FCC version does not provide.")]
        [Category(openCoverToolCategory)]
        //[DisplayName("OpenCover Custom Path")]
        public string OpenCoverCustomPath { get; set; }

        [Description("Change from Default if FCC determination of path32 or path64 is incorrect.")]
        [Category(openCoverToolCategory)]
        //[DisplayName("OpenCover Register")]
        public OpenCoverRegister OpenCoverRegister { get; set; }

        [Category(openCoverToolCategory)]
        [Description("Supply your own target if required.")]
        //[DisplayName("OpenCover Target")]
        public string OpenCoverTarget { get; set; }

        [Category(openCoverToolCategory)]
        [Description("If supplying your own target you can also supply additional arguments.  FCC supplies the test dll path.")]
        //[DisplayName("OpenCover Target Args")]
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
