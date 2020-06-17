using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace FineCodeCoverage.Impl
{
    [DataCollectorFriendlyName("SampleDataCollector")]
    [DataCollectorTypeUri("datacollector://SampleDataCollector/1.0")]
    public class SampleDataCollector : DataCollector
    {
        private DataCollectionLogger _logger;

        #region DataCollector Implementation

        public override void Initialize(XmlElement configurationElement, DataCollectionEvents events, DataCollectionSink dataSink, DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            _logger = logger;

            events.SessionStart += Events_SessionStart;
            events.SessionEnd += Events_SessionEnd;

            events.TestCaseStart += Events_TestCaseStart;
            events.TestCaseEnd += Events_TestCaseEnd;
        }

        #endregion

        #region Session Events

        private void Events_SessionStart(object sender, SessionStartEventArgs e)
        {
            _logger.LogWarning(e.Context, "SampleAdapter: Session start. Session id: " + e.Context.SessionId);
        }

        private void Events_SessionEnd(object sender, SessionEndEventArgs e)
        {
            _logger.LogWarning(e.Context, "SampleAdapter: Session end. Session id: " + e.Context.SessionId);
        }

        #endregion

        #region TestCase Events

        private void Events_TestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            _logger.LogWarning(e.Context, "SampleAdapter: Test Case start. Test case name: " + e.TestCaseName);
        }

        private void Events_TestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            _logger.LogWarning(e.Context, "SampleAdapter: Test Case end. Test case end: " + e.TestCaseName);
        }

        #endregion
    }
}