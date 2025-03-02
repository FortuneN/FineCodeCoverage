using System;
using System.Collections.Generic;
using System.Linq;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Options;
using FineCodeCoverage.Output;
using Microsoft.VisualStudio.Settings;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests
{
    public class AppOptionsProvider_Tests
    {
        private AutoMoqer autoMocker;
        private AppOptionsProvider appOptionsProvider;
        private Mock<WritableSettingsStore> mockWritableSettingsStore;

        [SetUp]
        public void Setup()
        {
            autoMocker = new AutoMoqer();
            appOptionsProvider = autoMocker.Create<AppOptionsProvider>();
            mockWritableSettingsStore = new Mock<WritableSettingsStore>();
            var mockWritableUserSettingsStoreProvider = autoMocker.GetMock<IWritableUserSettingsStoreProvider>();
            mockWritableUserSettingsStoreProvider.Setup(
                writableSettingsStoreProvider => writableSettingsStoreProvider.Provide()
            ).Returns(mockWritableSettingsStore.Object);
        }


        [Test]
        public void Should_Ensure_Store_When_LoadSettingsFromStorage()
        {
            appOptionsProvider.LoadSettingsFromStorage(new Mock<IAppOptions>().Object);
            mockWritableSettingsStore.Verify(writableSettingsStore => writableSettingsStore.CreateCollection("FineCodeCoverage"));
        }

        [Test]
        public void Should_Not_Create_Settings_Collection_If_Already_Exists()
        {
            mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.CollectionExists("FineCodeCoverage")).Returns(true);
            appOptionsProvider.LoadSettingsFromStorage(new Mock<IAppOptions>().Object);
            mockWritableSettingsStore.Verify(writableSettingsStore => writableSettingsStore.CreateCollection("FineCodeCoverage"), Times.Never());
        }

        [Test]
        public void Should_Ensure_Store_When_SaveSettingsToStorage()
        {
            appOptionsProvider.SaveSettingsToStorage(new Mock<IAppOptions>().Object);
            mockWritableSettingsStore.Verify(writableSettingsStore => writableSettingsStore.CreateCollection("FineCodeCoverage"));
        }

        [Test]
        public void Should_Not_Create_Settings_Collection_If_Already_Exists_When_SaveSettingsToStorage()
        {
            mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.CollectionExists("FineCodeCoverage")).Returns(true);
            appOptionsProvider.SaveSettingsToStorage(new Mock<IAppOptions>().Object);
            mockWritableSettingsStore.Verify(writableSettingsStore => writableSettingsStore.CreateCollection("FineCodeCoverage"), Times.Never());
        }

        [Test]
        public void Should_Have_Default_AppOptions_Property_When_Load_And_Does_Not_Exist_In_Storage()
        {
            var mockJsonConvertService = autoMocker.GetMock<IJsonConvertService>();
            mockJsonConvertService.Setup(
                jsonConvertService =>
                jsonConvertService.DeserializeObject(It.IsAny<string>(), typeof(string[]))
            ).Returns(new string[] { });

            mockWritableSettingsStore.Setup(
                writableSettingsStore => writableSettingsStore.PropertyExists("FineCodeCoverage", nameof(IAppOptions.AttributesExclude))
            ).Returns(false);
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.DefaultValueProvider = new NullStringArrayDefaultValueProvider();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;

            appOptionsProvider.LoadSettingsFromStorage(appOptions);
            
            Assert.Null(appOptions.AttributesExclude);
            mockWritableSettingsStore.VerifyAll();
        }

        [Test]
        public void Should_Default_NamespacedClasses_True()
        {
            DefaultTest(appOptions => appOptions.NamespacedClasses = true);
        }

        private void DefaultTest(Action<IAppOptions> verifyOptions)
        {
            mockWritableSettingsStore.Setup(
                writableSettingsStore => writableSettingsStore.PropertyExists("FineCodeCoverage", It.IsAny<string>())
            ).Returns(false);

            var mockAppOptions = new Mock<IAppOptions>();

            appOptionsProvider.LoadSettingsFromStorage(mockAppOptions.Object);

            mockAppOptions.VerifySet(verifyOptions);
        }

        [Test]
        public void Should_Default_Thresholds()
        {
            DefaultTest(appOptions => appOptions.ThresholdForCrapScore = 15);
            DefaultTest(appOptions => appOptions.ThresholdForNPathComplexity = 200);
            DefaultTest(appOptions => appOptions.ThresholdForCyclomaticComplexity = 30);
        }

        [Test]
        public void Should_Default_RunSettingsOnly_True()
        {
            DefaultTest(appOptions => appOptions.RunSettingsOnly = true);
        }

        [Test]
        public void Should_Default_RunWhenTestsFail_True()
        {
            DefaultTest(appOptions => appOptions.RunWhenTestsFail = true);
        }

        [Test]
        public void Should_Default_ExcludeByAttribute_GeneratedCode()
        {
            DefaultTest(appOptions => appOptions.ExcludeByAttribute = new[] { "GeneratedCode" });
        }

        [Test]
        public void Should_Default_IncludeTestAssembly_True()
        {
            DefaultTest(appOptions => appOptions.IncludeTestAssembly = true);
        }

        [Test]
        public void Should_Default_ExcludeByFile_Migrations()
        {
            DefaultTest(appOptions => appOptions.ExcludeByFile = new[] { "**/Migrations/*" });
        }

        [Test]
        public void Should_Default_Enabled_True()
        {
            DefaultTest(appOptions => appOptions.Enabled = true);
        }

        [Test]
        public void Should_Default_DisabledNoCoverage_True()
        {
            DefaultTest(appOptions => appOptions.DisabledNoCoverage = true);
        }

        [Test]
        public void Should_Default_True_ShowCoverageInOverviewMargin()
        {
            DefaultTest(appOptions => appOptions.ShowCoverageInOverviewMargin = true);
        }

        [Test]
        public void Should_Default_True_ShowCoveredInOverviewMargin()
        {
            DefaultTest(appOptions => appOptions.ShowCoveredInOverviewMargin = true);
        }

        [Test]
        public void Should_Default_True_ShowUncoveredInOverviewMargin()
        {
            DefaultTest(appOptions => appOptions.ShowUncoveredInOverviewMargin = true);
        }

        [Test]
        public void Should_Default_True_ShowPartiallyCoveredInOverviewMargin()
        {
            DefaultTest(appOptions => appOptions.ShowPartiallyCoveredInOverviewMargin = true);
        }

        [Test]
        public void Should_Not_Default_Any_Other_AppOptions_Properties()
        {
            mockWritableSettingsStore.Setup(
                writableSettingsStore => writableSettingsStore.PropertyExists("FineCodeCoverage", It.IsAny<string>())
            ).Returns(false);

            var mockAppOptions = new Mock<IAppOptions>();

            appOptionsProvider.LoadSettingsFromStorage(mockAppOptions.Object);

            var invocationNames = mockAppOptions.Invocations.Select(invocation => invocation.Method.Name).ToList();
            
            var expectedSetters = new List<string>
            {
                nameof(IAppOptions.Enabled),
                nameof(IAppOptions.ExcludeByFile),
                nameof(IAppOptions.IncludeTestAssembly),
                nameof(IAppOptions.ExcludeByAttribute),
                nameof(IAppOptions.RunWhenTestsFail),
                nameof(IAppOptions.RunSettingsOnly),
                nameof(IAppOptions.ThresholdForCrapScore),
                nameof(IAppOptions.ThresholdForNPathComplexity),
                nameof(IAppOptions.ThresholdForCyclomaticComplexity),
                nameof(IAppOptions.RunMsCodeCoverage),
                nameof(IAppOptions.NamespacedClasses),
                nameof(IAppOptions.ShowCoverageInOverviewMargin),
                nameof(IAppOptions.ShowCoveredInOverviewMargin),
                nameof(IAppOptions.ShowUncoveredInOverviewMargin),
                nameof(IAppOptions.ShowPartiallyCoveredInOverviewMargin),
                nameof(IAppOptions.ShowToolWindowToolbar),
                nameof(IAppOptions.Hide0Coverable),
                nameof(IAppOptions.DisabledNoCoverage),
                nameof(IAppOptions.ShowEditorCoverage),
                nameof(IAppOptions.ShowCoverageInGlyphMargin),
                nameof(IAppOptions.ShowCoveredInGlyphMargin),
                nameof(IAppOptions.ShowUncoveredInGlyphMargin),
                nameof(IAppOptions.ShowPartiallyCoveredInGlyphMargin),
                nameof(IAppOptions.ShowLineCoveredHighlighting),
                nameof(IAppOptions.ShowLinePartiallyCoveredHighlighting),
                nameof(IAppOptions.ShowLineUncoveredHighlighting),
                nameof(IAppOptions.UseEnterpriseFontsAndColors)
            };
            CollectionAssert.AreEquivalent(expectedSetters.Select(s => $"set_{s}"), invocationNames);
        }

        [TestCase(null)]
        [TestCase("  ")]
        public void Should_Have_Default_AppOptions_Property_When_Load_And_Is_Null_Or_Whitespace_In_Storage(string nullOrWhitespace)
        {
            var mockJsonConvertService = autoMocker.GetMock<IJsonConvertService>();
            mockJsonConvertService.Setup(
                jsonConvertService =>
                jsonConvertService.DeserializeObject(It.IsAny<string>(), typeof(string[]))
            ).Returns(new string[] { });

            mockWritableSettingsStore.Setup(
                writableSettingsStore => writableSettingsStore.PropertyExists("FineCodeCoverage", nameof(IAppOptions.AttributesExclude))
            ).Returns(true);
            mockWritableSettingsStore.Setup(
                writableSettingsStore => writableSettingsStore.GetString("FineCodeCoverage", nameof(IAppOptions.AttributesExclude))
            ).Returns(nullOrWhitespace);

            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.DefaultValueProvider = new NullStringArrayDefaultValueProvider();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;

            appOptionsProvider.LoadSettingsFromStorage(appOptions);
            
            Assert.Null(appOptions.AttributesExclude);
            mockWritableSettingsStore.VerifyAll();
        }

        [Test]
        public void Should_Use_Deseralized_String_From_Store_For_AppOption_Property_LoadSettingsFromStorage()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.DefaultValueProvider = new NullStringArrayDefaultValueProvider();
            mockAppOptions.SetupAllProperties();
            var appOptions = mockAppOptions.Object;

            Should_Use_Deseralized_String_From_Store_For_AppOption_Property(() =>
            {
                appOptionsProvider.LoadSettingsFromStorage(appOptions);
                return appOptions;
            });
        }

        [Test]
        public void Should_Use_Deseralized_String_From_Store_For_AppOption_Property_Get()
        {
            Should_Use_Deseralized_String_From_Store_For_AppOption_Property(() =>
            {
                var appOptions = appOptionsProvider.Get();
                return appOptions;
            });
        }


        internal void Should_Use_Deseralized_String_From_Store_For_AppOption_Property(Func<IAppOptions> act)
        {
            Dictionary<string, object> deserializedValues = new Dictionary<string, object>
            {
                { nameof(IAppOptions.AdjacentBuildOutput), false},
                { nameof(IAppOptions.AttributesExclude), new string[]{ "aexclude"}},
                { nameof(IAppOptions.AttributesInclude), new string[]{ "ainclude"}},
                { nameof(IAppOptions.CompanyNamesExclude), new string[]{ "cexclude"}},
                { nameof(IAppOptions.CompanyNamesInclude), new string[]{ "cinclude"}},
                { nameof(IAppOptions.CoverletCollectorDirectoryPath), "p"},
                { nameof(IAppOptions.CoverletConsoleCustomPath), "cp"},
                { nameof(IAppOptions.CoverletConsoleGlobal), true},
                { nameof(IAppOptions.CoverletConsoleLocal), true},
                { nameof(IAppOptions.Enabled), true},
                 { nameof(IAppOptions.DisabledNoCoverage), true},
                { nameof(IAppOptions.Exclude), new string[]{"exclude" } },
                { nameof(IAppOptions.ExcludeByAttribute), new string[]{ "ebyatt"} },
                { nameof(IAppOptions.ExcludeByFile), new string[]{ "ebyfile"} },
                { nameof(IAppOptions.FCCSolutionOutputDirectoryName), "FCCSolutionOutputDirectoryName"},
                { nameof(IAppOptions.FunctionsExclude), new string[]{ "FunctionsExclude" } },
                { nameof(IAppOptions.FunctionsInclude), new string[]{ "FunctionsInclude" } },
                { nameof(IAppOptions.HideFullyCovered), true },
                { nameof(IAppOptions.Hide0Coverable),true },
                { nameof(IAppOptions.Hide0Coverage),true },
                { nameof(IAppOptions.Include), new string[]{ "Include" } },
                { nameof(IAppOptions.IncludeReferencedProjects),true},
                { nameof(IAppOptions.IncludeTestAssembly),true},
                { nameof(IAppOptions.ModulePathsExclude),new string[]{ "ModulePathsExclude" }},
                { nameof(IAppOptions.ModulePathsInclude),new string[]{ "ModulePathsInclude" }},
                { nameof(IAppOptions.NamespacedClasses),true},
                { nameof(IAppOptions.OpenCoverCustomPath),"OpenCoverCustomPath"},
                { nameof(IAppOptions.PublicKeyTokensExclude),new string[]{ "PublicKeyTokensExclude" }},
                { nameof(IAppOptions.PublicKeyTokensInclude),new string[]{ "PublicKeyTokensInclude" }},
                { nameof(IAppOptions.RunInParallel),true},
                { nameof(IAppOptions.RunSettingsOnly),true},
                { nameof(IAppOptions.RunWhenTestsExceed),1},
                { nameof(IAppOptions.RunWhenTestsFail),true},
                { nameof(IAppOptions.SourcesExclude),new string[]{ "SourcesExclude" }},
                { nameof(IAppOptions.SourcesInclude),new string[]{ "SourcesInclude" }},
                { nameof(IAppOptions.StickyCoverageTable),true},
                { nameof(IAppOptions.ThresholdForCrapScore),1},
                { nameof(IAppOptions.ThresholdForCyclomaticComplexity),1},
                { nameof(IAppOptions.ThresholdForNPathComplexity),1},
                { nameof(IAppOptions.ToolsDirectory),"ToolsDirectory"},
                { nameof(IAppOptions.RunMsCodeCoverage), RunMsCodeCoverage.IfInRunSettings},
                { nameof(IAppOptions.ShowCoverageInOverviewMargin),true},
                { nameof(IAppOptions.ShowCoveredInOverviewMargin),true},
                { nameof(IAppOptions.ShowPartiallyCoveredInOverviewMargin),true},
                { nameof(IAppOptions.ShowDirtyInOverviewMargin), true },
                { nameof(IAppOptions.ShowNewInOverviewMargin), true },
                { nameof(IAppOptions.ShowUncoveredInOverviewMargin),true},
                { nameof(IAppOptions.ShowNotIncludedInOverviewMargin),true},
                { nameof(IAppOptions.ShowToolWindowToolbar),true},
                {nameof(IAppOptions.ExcludeAssemblies),new string[]{ "Exclude"} },
                {nameof(IAppOptions.IncludeAssemblies),new string[]{ "Include"} },
                {nameof(IAppOptions.NamespaceQualification),NamespaceQualification.AlwaysUnqualified },
                {nameof(IAppOptions.OpenCoverRegister),OpenCoverRegister.Default },
                {nameof(IAppOptions.OpenCoverTarget),"" },
                {nameof(IAppOptions.OpenCoverTargetArgs),"" },
                {nameof(IAppOptions.ShowEditorCoverage),true },
                {nameof(IAppOptions.ShowCoverageInGlyphMargin),true },
                {nameof(IAppOptions.ShowCoveredInGlyphMargin),true },
                {nameof(IAppOptions.ShowPartiallyCoveredInGlyphMargin),true },
                {nameof(IAppOptions.ShowUncoveredInGlyphMargin),true },
                {nameof(IAppOptions.ShowDirtyInGlyphMargin),true },
                {nameof(IAppOptions.ShowNewInGlyphMargin),true },
                {nameof(IAppOptions.ShowNotIncludedInGlyphMargin),true },
                {nameof(IAppOptions.ShowLineCoverageHighlighting),true },
                {nameof(IAppOptions.ShowLineCoveredHighlighting),true },
                {nameof(IAppOptions.ShowLinePartiallyCoveredHighlighting),true },
                {nameof(IAppOptions.ShowLineUncoveredHighlighting),true },
                {nameof(IAppOptions.ShowLineDirtyHighlighting),true },
                {nameof(IAppOptions.ShowLineNewHighlighting),true },
                {nameof(IAppOptions.ShowLineNotIncludedHighlighting),true },
                {nameof(IAppOptions.UseEnterpriseFontsAndColors),true },
                {nameof(IAppOptions.EditorCoverageColouringMode), EditorCoverageColouringMode.UseRoslynWhenTextChanges },
                {nameof(IAppOptions.BlazorCoverageLinesFromGeneratedSource), true }
            };
            var mockJsonConvertService = autoMocker.GetMock<IJsonConvertService>();
            mockJsonConvertService.Setup(
                jsonConvertService =>
                jsonConvertService.DeserializeObject(It.IsAny<string>(), It.IsAny<Type>())
            ).Returns<string, Type>((serializedValueFromStore, _) => {
                if (deserializedValues.ContainsKey(serializedValueFromStore))
                {
                    return deserializedValues[serializedValueFromStore];
                }
                return null;
            });

            mockWritableSettingsStore.Setup(
                writableSettingsStore => writableSettingsStore.PropertyExists("FineCodeCoverage", It.IsAny<string>())
            ).Returns(true);

            mockWritableSettingsStore.Setup(
                writableSettingsStore => writableSettingsStore.GetString("FineCodeCoverage", It.IsAny<string>())
            ).Returns<string, string>((_, propertyName) => propertyName);

            var appOptions = act();

            var appOptionsPropertyInfos = typeof(IAppOptions).GetPublicProperties();
            foreach(var appOptionsPropertyInfo in appOptionsPropertyInfos)
            {
                if (appOptionsPropertyInfo.PropertyType.IsValueType)
                {
                    Assert.AreEqual(deserializedValues[appOptionsPropertyInfo.Name], appOptionsPropertyInfo.GetValue(appOptions));
                }
                else
                {
                    Assert.AreSame(deserializedValues[appOptionsPropertyInfo.Name], appOptionsPropertyInfo.GetValue(appOptions));
                }
            }

        }
    

        [Test]
        public void Should_Log_Exception_Thrown_In_LoadSettingsFromStorage()
        {
            var exception = new Exception("msg");
            string _propertyName = null;
            mockWritableSettingsStore.Setup(
                writableSettingsStore => writableSettingsStore.PropertyExists("FineCodeCoverage", It.IsAny<string>())
            ).Returns(true);
            mockWritableSettingsStore.Setup(
                writableSettingsStore => writableSettingsStore.GetString("FineCodeCoverage", It.IsAny<string>())
            ).Callback<string,string>((_,propertyName) => _propertyName = propertyName).Throws(exception);

            appOptionsProvider.LoadSettingsFromStorage(new Mock<IAppOptions>().Object);

            autoMocker.Verify<ILogger>(logger => logger.Log($"Failed to load '{_propertyName}' setting", exception));
        }

        [Test]
        public void IAppOptions_Should_Have_A_Getter_And_Setter_For_Each_Property()
        {
            var propertyInfos = typeof(IAppOptions).GetPublicProperties();
            Assert.True(propertyInfos.All(pi => pi.GetMethod != null && pi.SetMethod != null));
        }

        [Test]
        public void Should_Write_The_Serialized_Property_Value_To_The_Store()
        {
            var propertyValue = new string[] { "CompanyNamesExclude" };
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(appOptions => appOptions.CompanyNamesExclude).Returns(propertyValue);

            var mockJsonConvertService = autoMocker.GetMock<IJsonConvertService>();
            mockJsonConvertService.Setup(
                jsonConvertService =>
                jsonConvertService.SerializeObject(propertyValue)
            ).Returns("Serialized");

            appOptionsProvider.SaveSettingsToStorage(mockAppOptions.Object);

            mockWritableSettingsStore.Verify(
                writableSettingsStore => writableSettingsStore.SetString("FineCodeCoverage", nameof(IAppOptions.CompanyNamesExclude), "Serialized")
            );

        }

        [Test]
        public void Should_Raise_Options_Changed_When_SaveSettingsToStorage()
        {
            var mockAppOptions = new Mock<IAppOptions>();
            var appOptions = mockAppOptions.Object;

            IAppOptions changedOptions = null;
            appOptionsProvider.OptionsChanged += (options) =>
            {
                changedOptions = options;
            };

            appOptionsProvider.SaveSettingsToStorage(appOptions);

            Assert.AreSame(appOptions, changedOptions);
        }

        [Test]
        public void Should_Log_If_Exception_When_SaveSettingsToStorage()
        {
            var exception = new Exception();
            mockWritableSettingsStore.Setup(
                writeableSettingsStore => writeableSettingsStore.SetString("FineCodeCoverage", nameof(IAppOptions.Enabled), It.IsAny<string>())
            ).Throws(exception);

            var mockAppOptions = new Mock<IAppOptions>();

            appOptionsProvider.SaveSettingsToStorage(mockAppOptions.Object);

            autoMocker.Verify<ILogger>(logger => logger.Log($"Failed to save '{nameof(IAppOptions.Enabled)}' setting", exception));
        }
    }  
    
    internal class NullStringArrayDefaultValueProvider : LookupOrFallbackDefaultValueProvider
    {
        public NullStringArrayDefaultValueProvider()
        {
            base.Register(typeof(string[]), (_, __) => null);
        }
    }
}
