using AutoMoq;
using FineCodeCoverage.Engine.OpenCover;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FineCodeCoverageTests
{
    internal class OpenCoverUtil_Tests
    {
        private AutoMoqer mocker;
        private OpenCoverUtil openCoverUtil;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            openCoverUtil = mocker.Create<OpenCoverUtil>();
        }

        [Test]
        public void Should_Ensure_Unzipped()
        {
            
        }
    }
}
