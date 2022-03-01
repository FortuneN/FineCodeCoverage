using System.Threading.Tasks;

namespace FineCodeCoverage.Engine
{
    internal interface ISourceFileOpener
    {
        Task OpenFileAsync(string assemblyName, string qualifiedClassName, int file, int line);
    }

}