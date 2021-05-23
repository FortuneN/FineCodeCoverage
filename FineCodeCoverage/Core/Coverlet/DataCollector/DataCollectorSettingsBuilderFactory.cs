using System.ComponentModel.Composition;

namespace FineCodeCoverage.Engine.Coverlet
{
    [Export(typeof(IDataCollectorSettingsBuilderFactory))]
    internal class DataCollectorSettingsBuilderFactory : IDataCollectorSettingsBuilderFactory
    {

        public IDataCollectorSettingsBuilder Create()
        {
            return new DataCollectorSettingsBuilder();
        }
    }
}
