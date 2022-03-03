using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            var ct = CancellationToken.None;
            coverageUtilManager.Initialize("AppDataFolder", ct);
            mocker.Verify<ICoverletUtil>(coverletUtil => coverletUtil.Initialize("AppDataFolder", ct));
            mocker.Verify<IOpenCoverUtil>(coverletUtil => coverletUtil.Initialize("AppDataFolder", ct));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_Run_The_Appropriate_Cover_Tool_Based_On_IsDotNetSdkStyle(bool isDotNetSdkStyle)
        {
            var mockProject = new Mock<ICoverageProject>();
            mockProject.Setup(cp => cp.IsDotNetSdkStyle()).Returns(isDotNetSdkStyle);
            var mockedProject = mockProject.Object;

            var coverageUtilManager = mocker.Create<CoverageUtilManager>();
            var ct = CancellationToken.None;
            await coverageUtilManager.RunCoverageAsync(mockedProject, ct);

            if (isDotNetSdkStyle)
            {
                mocker.Verify<ICoverletUtil>(coverletUtil => coverletUtil.RunCoverletAsync(mockedProject, ct));
            }
            else
            {
                mocker.Verify<IOpenCoverUtil>(openCoverUtil => openCoverUtil.RunOpenCoverAsync(mockedProject, ct));
            }
        }
    }
}
