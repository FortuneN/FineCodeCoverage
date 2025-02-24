﻿using FineCodeCoverage.Engine.Model;
using NUnit.Framework;
using System.IO;

namespace FineCodeCoverageTests
{
    public class CoverageProject_Tests
    {
        private string tempProjectFilePath;
        private CoverageProject coverageProject;

        [SetUp]
        public void SetUp()
        {
            coverageProject = new CoverageProject(null, null, null, null, null, false);
            tempProjectFilePath = Path.Combine(Path.GetTempPath(), "testproject.csproj");
            coverageProject.ProjectFile = tempProjectFilePath;
        }

        [Test]
        public void Should_Be_An_Sdk_Project_When_Project_Element_Has_Sdk_Attribute()
        {
            Test(@"<Project Sdk=""My.Custom.Sdk/1.2.3""/>", true);
        }

        [Test]
        public void Should_Be_An_Sdk_Project_When_Project_Element_Has_Child_Sdk_Element()
        {
            Test(@"<Project>
<Sdk Name=""My.Custom.Sdk"" Version=""1.2.3"" />
</Project>
", true);
        }

        [Test]
        public void Should_be_An_Sdk_Project_When_Project_Element_Has_Child_Import_With_Sdk_Attribute()
        {
            Test(@"<Project>
					<PropertyGroup>
						<MyProperty>Value</MyProperty>
					</PropertyGroup>
					<Import Project=""Sdk.props"" Sdk=""My.Custom.Sdk"" />
				</Project>
", true);
        }

        [Test]
        public void Should_Not_Be_An_Sdk_Project_When_Is_Not()
        {
            Test(@"<Project ToolsVersion=""15.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""..\packages\Microsoft.CodeCoverage.17.12.0\build\netstandard2.0\Microsoft.CodeCoverage.props"" Condition=""Exists('..\packages\Microsoft.CodeCoverage.17.12.0\build\netstandard2.0\Microsoft.CodeCoverage.props')"" />
  <Import Project=""..\packages\MSTest.TestAdapter.2.2.10\build\net46\MSTest.TestAdapter.props"" Condition=""Exists('..\packages\MSTest.TestAdapter.2.2.10\build\net46\MSTest.TestAdapter.props')"" />
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{F4B73E7F-F91D-4C95-A3CE-B8DA3A94BB90}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>NetFrameworkUnitTest</RootNamespace>
    <AssemblyName>NetFrameworkUnitTest</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <ProjectTypeGuids>{3AC096D0-A1C2-E12C-1390-A8335801FDAB};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
    <VisualStudioVersion Condition=""'$(VisualStudioVersion)' == ''"">15.0</VisualStudioVersion>
    <VSToolsPath Condition=""'$(VSToolsPath)' == ''"">$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)</VSToolsPath>
    <ReferencePath>$(ProgramFiles)\Common Files\microsoft shared\VSTT\$(VisualStudioVersion)\UITestExtensionPackages</ReferencePath>
    <IsCodedUITest>False</IsCodedUITest>
    <TestProjectType>UnitTest</TestProjectType>
    <NuGetPackageImportStamp>
    </NuGetPackageImportStamp>
    <RunSettingsFilePath>
    </RunSettingsFilePath>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""Microsoft.VisualStudio.CodeCoverage.Shim, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"">
      <HintPath>..\packages\Microsoft.CodeCoverage.17.12.0\lib\net462\Microsoft.VisualStudio.CodeCoverage.Shim.dll</HintPath>
    </Reference>
    <Reference Include=""Microsoft.VisualStudio.TestPlatform.TestFramework, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"">
      <HintPath>..\packages\MSTest.TestFramework.2.2.10\lib\net45\Microsoft.VisualStudio.TestPlatform.TestFramework.dll</HintPath>
    </Reference>
    <Reference Include=""Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"">
      <HintPath>..\packages\MSTest.TestFramework.2.2.10\lib\net45\Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions.dll</HintPath>
    </Reference>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""UnitTest1.cs"" />
    <Compile Include=""Properties\AssemblyInfo.cs"" />
  </ItemGroup>
  <ItemGroup>
    <None Include=""packages.config"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\DemoOpenCover\DemoOpenCover.csproj"">
      <Project>{9ef61906-87e7-42ac-b977-058fc55f74d8}</Project>
      <Name>DemoOpenCover</Name>
    </ProjectReference>
    <ProjectReference Include=""..\Exclude\Exclude.csproj"">
      <Project>{fa82235f-9590-410b-9840-daa224abb17e}</Project>
      <Name>Exclude</Name>
    </ProjectReference>
  </ItemGroup>
  <Import Project=""$(VSToolsPath)\TeamTest\Microsoft.TestTools.targets"" Condition=""Exists('$(VSToolsPath)\TeamTest\Microsoft.TestTools.targets')"" />
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
  <Target Name=""EnsureNuGetPackageBuildImports"" BeforeTargets=""PrepareForBuild"">
    <PropertyGroup>
      <ErrorText>This project references NuGet package(s) that are missing on this computer. Use NuGet Package Restore to download them.  For more information, see http://go.microsoft.com/fwlink/?LinkID=322105. The missing file is {0}.</ErrorText>
    </PropertyGroup>
    <Error Condition=""!Exists('..\packages\MSTest.TestAdapter.2.2.10\build\net46\MSTest.TestAdapter.props')"" Text=""$([System.String]::Format('$(ErrorText)', '..\packages\MSTest.TestAdapter.2.2.10\build\net46\MSTest.TestAdapter.props'))"" />
    <Error Condition=""!Exists('..\packages\MSTest.TestAdapter.2.2.10\build\net46\MSTest.TestAdapter.targets')"" Text=""$([System.String]::Format('$(ErrorText)', '..\packages\MSTest.TestAdapter.2.2.10\build\net46\MSTest.TestAdapter.targets'))"" />
    <Error Condition=""!Exists('..\packages\Microsoft.CodeCoverage.17.12.0\build\netstandard2.0\Microsoft.CodeCoverage.props')"" Text=""$([System.String]::Format('$(ErrorText)', '..\packages\Microsoft.CodeCoverage.17.12.0\build\netstandard2.0\Microsoft.CodeCoverage.props'))"" />
    <Error Condition=""!Exists('..\packages\Microsoft.CodeCoverage.17.12.0\build\netstandard2.0\Microsoft.CodeCoverage.targets')"" Text=""$([System.String]::Format('$(ErrorText)', '..\packages\Microsoft.CodeCoverage.17.12.0\build\netstandard2.0\Microsoft.CodeCoverage.targets'))"" />
  </Target>
  <Import Project=""..\packages\MSTest.TestAdapter.2.2.10\build\net46\MSTest.TestAdapter.targets"" Condition=""Exists('..\packages\MSTest.TestAdapter.2.2.10\build\net46\MSTest.TestAdapter.targets')"" />
  <Import Project=""..\packages\Microsoft.CodeCoverage.17.12.0\build\netstandard2.0\Microsoft.CodeCoverage.targets"" Condition=""Exists('..\packages\Microsoft.CodeCoverage.17.12.0\build\netstandard2.0\Microsoft.CodeCoverage.targets')"" />
</Project>", false);
        }

        private void Test(string project, bool expectedIsSdkStyle)
        {
            var xmlDeclaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n";
            File.WriteAllText(tempProjectFilePath, $"{xmlDeclaration}{project}");
            Assert.That(coverageProject.IsDotNetSdkStyle(), Is.EqualTo(expectedIsSdkStyle));
        }

        [TearDown]
        public void Delete_ProjectFile()
        {
            File.Delete(tempProjectFilePath);
        }
    }

}
