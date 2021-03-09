using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface ICoverletDataCollectorGeneratedCobertura
    {
        Task CorrectPathAsync(string coverageOutputFolder, string coverageOutputFile);
    }
}
