using AutoMoq;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.OpenCover;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests
{
    class CoverageUtilManager_Tests
    {
        private AutoMoqer mocker;
        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
        }

        [Test]
        public void Initialize_Should_Initialize_The_Coverage_Utils()
        {
            var coverageUtilManager = mocker.Create<CoverageUtilManager>();
            coverageUtilManager.Initialize("AppDataFolder");
            mocker.Verify<ICoverletUtil>(coverletUtil => coverletUtil.Initialize("AppDataFolder"));
            mocker.Verify<IOpenCoverUtil>(coverletUtil => coverletUtil.Initialize("AppDataFolder"));
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void Should_Run_The_Appropriate_Cover_Tool_Based_On_IsDotNetSdkStyle(bool isDotNetSdkStyle, bool throwError)
        {
            var mockProject = new Mock<ICoverageProject>();
            mockProject.Setup(cp => cp.IsDotNetSdkStyle()).Returns(isDotNetSdkStyle);
            var mockedProject = mockProject.Object;

            var coverageUtilManager = mocker.Create<CoverageUtilManager>();
            coverageUtilManager.RunCoverageAsync(mockedProject, throwError);

            if (isDotNetSdkStyle)
            {
                mocker.Verify<ICoverletUtil>(coverletUtil => coverletUtil.RunCoverletAsync(mockedProject, throwError));
            }
            else
            {
                mocker.Verify<IOpenCoverUtil>(openCoverUtil => openCoverUtil.RunOpenCoverAsync(mockedProject, throwError));
            }
        }
    }
}
