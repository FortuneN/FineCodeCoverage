using System.Collections.Generic;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Coverlet;
using NUnit.Framework;

namespace FineCodeCoverageTests
{
    public static class DotNetToolListOutput
    {
        public static readonly string GlobalWithTool = "Package Id            Version Commands\r\n-------------------------------------------\r\ncoverlet.console      3.0.3        coverlet\r\n";
        public static readonly string GlobalToolPathNoTool = "Package Id      Version      Commands\r\n-------------------------------------\r\n";
        public static readonly string LocalWithTool = "Package Id            Version      Commands      Manifest                                                           \r\n--------------------------------------------------------------------------------------------------------------------\r\ncoverlet.console      3.0.3        coverlet      C:\\Users\\tonyh\\Source\\Repos\\SUT WithSpace\\.config\\dotnet-tools.json\r\n";
        public static readonly string LocalNoTool = "Package Id      Version Commands      Manifest\r\n---------------------------------------------------\r\n";
    }

    public class DotNetToolListParser_Tests
    {
        [Test]
        public void Should_Return_Empty_List_If_No_Tools_Installed()
        {
            var dotnetToolListParser = new DotNetToolListParser();
            Assert.IsEmpty(dotnetToolListParser.Parse(DotNetToolListOutput.LocalNoTool));
        }

        [Test]
        public void Should_Parse_Local()
        {
            var dotnetToolListParser = new DotNetToolListParser();
            var parsed = dotnetToolListParser.Parse(DotNetToolListOutput.LocalWithTool);
            Assert.AreEqual(parsed.Count, 1);
            var installedTool = parsed[0];
            Assert.AreEqual("coverlet", installedTool.Commands);
            Assert.AreEqual("3.0.3", installedTool.Version);
            Assert.AreEqual("coverlet.console", installedTool.PackageId);
        }

        [Test]
        public void Should_Parse_Global()
        {
            var dotnetToolListParser = new DotNetToolListParser();
            var parsed = dotnetToolListParser.Parse(DotNetToolListOutput.GlobalWithTool);
            Assert.AreEqual(parsed.Count, 1);
            var installedTool = parsed[0];
            Assert.AreEqual("coverlet", installedTool.Commands);
            Assert.AreEqual("3.0.3", installedTool.Version);
            Assert.AreEqual("coverlet.console", installedTool.PackageId);
        }
    }

    public class DotNetToolListCoverlet_Tests
    {
        private AutoMoqer mocker;
        private DotNetToolListCoverlet dotNetToolListCoverlet;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            dotNetToolListCoverlet = mocker.Create<DotNetToolListCoverlet>();
        }

        [Test]
        public void Should_Execute_And_Parse_Global_Installed()
        {
            var mockExecutor = mocker.GetMock<IDotNetToolListExecutor>();
            var globalOutput = "global";
            mockExecutor.Setup(executor => executor.Global()).Returns(new DotNetToolListExecutionResult { ExitCode = 0, Output = globalOutput });
            var mockParser = mocker.GetMock<IDotNetToolListParser>();
            mockParser.Setup(parser => parser.Parse(globalOutput)).Returns(new List<DotNetTool> { new DotNetTool { PackageId = "coverlet.console", Commands = "theCommand", Version = "theVersion" } });
            var coverletToolDetails = dotNetToolListCoverlet.Global();
            Assert.AreEqual("theCommand", coverletToolDetails.Command);
            Assert.AreEqual("theVersion", coverletToolDetails.Version);
        }

        [Test]
        public void Should_Execute_And_Parse_Global_Not_Installed()
        {
            var mockExecutor = mocker.GetMock<IDotNetToolListExecutor>();
            var globalOutput = "global";
            mockExecutor.Setup(executor => executor.Global()).Returns(new DotNetToolListExecutionResult { ExitCode = 0, Output = globalOutput });
            var mockParser = mocker.GetMock<IDotNetToolListParser>();
            mockParser.Setup(parser => parser.Parse(globalOutput)).Returns(new List<DotNetTool> { new DotNetTool { PackageId = "not.coverlet.console", Commands = "theCommand", Version = "theVersion" } });
            var coverletToolDetails = dotNetToolListCoverlet.Global();
            Assert.IsNull(coverletToolDetails);
        }

        [Test]
        public void Should_Log_Output_And_Return_Null_When_Parsing_Error()
        {
            var parsing = "this will be parsed";
            var mockExecutor = mocker.GetMock<IDotNetToolListExecutor>();
            mockExecutor.Setup(executor => executor.Global()).Returns(new DotNetToolListExecutionResult { ExitCode = 0, Output = parsing });
            var mockParser = mocker.GetMock<IDotNetToolListParser>();
            mockParser.Setup(parser => parser.Parse(parsing)).Throws(new System.Exception());
            var coverletToolDetails = dotNetToolListCoverlet.Global();
            Assert.IsNull(coverletToolDetails);
            mocker.Verify<ILogger>(l => l.Log("Dotnet tool list Coverlet Error parsing", parsing));
        }

        [Test]
        public void Should_Log_Output_When_Executor_Error()
        {
            var mockExecutor = mocker.GetMock<IDotNetToolListExecutor>();
            var globalErrorOutput = "this is an error";
            mockExecutor.Setup(executor => executor.Global()).Returns(new DotNetToolListExecutionResult { ExitCode = 1, Output = globalErrorOutput });
            var coverletToolDetails = dotNetToolListCoverlet.Global();
            Assert.IsNull(coverletToolDetails);
            mocker.Verify<ILogger>(l => l.Log("Dotnet tool list Coverlet Error", globalErrorOutput));
        }

        [Test]
        public void Should_Execute_And_Parse_Local_Installed()
        {
            var localDirectory = "localDir";
            var mockExecutor = mocker.GetMock<IDotNetToolListExecutor>();
            var localOutput = "local";
            mockExecutor.Setup(executor => executor.Local(localDirectory)).Returns(new DotNetToolListExecutionResult { ExitCode = 0, Output = localOutput });
            var mockParser = mocker.GetMock<IDotNetToolListParser>();
            mockParser.Setup(parser => parser.Parse(localOutput)).Returns(new List<DotNetTool> { new DotNetTool { PackageId = "coverlet.console", Commands = "theCommand", Version = "theVersion" } });
            var coverletToolDetails = dotNetToolListCoverlet.Local(localDirectory);
            Assert.AreEqual("theCommand", coverletToolDetails.Command);
            Assert.AreEqual("theVersion", coverletToolDetails.Version);
        }

        [Test]
        public void Should_Execute_And_Parse_Global_Tools_Path_Installed()
        {
            var globalToolsDirectory = "globalToolsDir";
            var mockExecutor = mocker.GetMock<IDotNetToolListExecutor>();
            var globalToolsOutput = "local";
            mockExecutor.Setup(executor => executor.GlobalToolsPath(globalToolsDirectory)).Returns(new DotNetToolListExecutionResult { ExitCode = 0, Output = globalToolsOutput });
            var mockParser = mocker.GetMock<IDotNetToolListParser>();
            mockParser.Setup(parser => parser.Parse(globalToolsOutput)).Returns(new List<DotNetTool> { new DotNetTool { PackageId = "coverlet.console", Commands = "theCommand", Version = "theVersion" } });
            var coverletToolDetails = dotNetToolListCoverlet.GlobalToolsPath(globalToolsDirectory);
            Assert.AreEqual("theCommand", coverletToolDetails.Command);
            Assert.AreEqual("theVersion", coverletToolDetails.Version);
        }
    }
}
