using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Engine;
using NUnit.Framework;

namespace FineCodeCoverageTests
{
    class CoverageToolOutputManager_Tests
    {
        private AutoMoqer mocker;
        private CoverageToolOutputManager coverageToolOutputManager;
        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            coverageToolOutputManager = mocker.Create<CoverageToolOutputManager>();
        }

        [Test]
        public void Should_Set_CoverageOutputFolder_To_Sub_Folder_Of_CoverageProject_FCCOutputFolder_For_All_When_Do_Not_Specify()
        {

        }

        [Test]
        public void Should_Set_CoverageOutputFolder_To_ProjectName_Sub_Folder_Of_First_Providing_AllProjectsCoverageOutputFolder()
        {

        }
    }
}
