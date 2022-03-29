using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using NUnit.Framework;
using System.Xml.Linq;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    public class RunSettingsHelper_Tests
    {
        [Test]
        public void Should_Match_Ms_Data_Collector_By_FriendlyName()
        {
            var element = XElement.Parse(@"
<DataCollector friendlyName='Code Coverage'/>
");
            Assert.True(RunSettingsHelper.IsMsDataCollector(element));
        }

        [Test]
        public void Should_Match_Ms_Data_Collector_By_Uri()
        {
            var element = XElement.Parse(@"
<DataCollector uri='datacollector://Microsoft/CodeCoverage/2.0'/>
");
            Assert.True(RunSettingsHelper.IsMsDataCollector(element));
        }

        [Test]
        public void Should_Not_Be_Ms_Data_Collector_When_Different_Data_Collector()
        {
            var element = XElement.Parse(@"
<DataCollector uri='other'/>
");
            Assert.False(RunSettingsHelper.IsMsDataCollector(element));

            element = XElement.Parse(@"
<DataCollector friendlyName='Other'/>
");
            Assert.False(RunSettingsHelper.IsMsDataCollector(element));
        }

        [Test]
        public void Should_Not_Be_Ms_Data_Collector_When_No_Attributes()
        {
            var element = XElement.Parse(@"
<DataCollector />
");
            Assert.False(RunSettingsHelper.IsMsDataCollector(element));
        }
    }
}
