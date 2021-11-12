using System.ComponentModel.Composition;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Coverlet
{
    [Export(typeof(IDataCollectorSettingsBuilderFactory))]
    internal class DataCollectorSettingsBuilderFactory : IDataCollectorSettingsBuilderFactory
    {
        private readonly ILogger logger;

        [ImportingConstructor]
        public DataCollectorSettingsBuilderFactory(ILogger logger)
        {
            this.logger = logger;
        }
        public IDataCollectorSettingsBuilder Create()
        {
            return new DataCollectorSettingsBuilder(logger);
        }
    }
}
