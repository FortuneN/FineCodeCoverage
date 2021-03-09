using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine.Coverlet
{

    [Export(typeof(ICoverletDataCollectorGeneratedCobertura))]
    internal class CoverletDataCollectorGeneratedCobertura : ICoverletDataCollectorGeneratedCobertura
    {
        internal const string collectorGeneratedCobertura = "coverage.cobertura.xml";
        private readonly IDirectoryFilePoller directoryFilePoller;
        internal const int fileWaitMs = 10000;

        [ImportingConstructor]
        public CoverletDataCollectorGeneratedCobertura(IDirectoryFilePoller directoryFilePoller)
        {
            this.directoryFilePoller = directoryFilePoller;
        }
        private Task<FileInfo> GetCoberturaFileAsync(string coverageOutputFolder)
        {
            //C:\\Users\\tonyh\\Source\\Repos\\DataCollectorXUnit\\XUnitTestProject1\\bin\\Debug\\netcoreapp3.1\\fine-code-coverage\\coverage-tool-output\\7ba6447d-a89f-4836-bffc-aeb4799e48ab\\coverage.cobertura.xml\r\nP

            //should only be the one
            return directoryFilePoller.PollAsync(
                coverageOutputFolder, 
                collectorGeneratedCobertura, 
                fileWaitMs, 
                file => file.OrderBy(f => f.LastWriteTime).Last(),
                SearchOption.AllDirectories);
        }
        public async Task CorrectPathAsync(string coverageOutputFolder, string coverageOutputFile)
        {
            var coberturaFile = await GetCoberturaFileAsync(coverageOutputFolder);
            if (coberturaFile == null)
            {
                throw new Exception($"Data collector did not generate {collectorGeneratedCobertura}");
            }
            coberturaFile.MoveTo(coverageOutputFile);
        }
    }
}
