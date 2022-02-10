using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IResourceProvider))]
    internal class ResourceProvider : IResourceProvider
    {
        private readonly string resourcesDirectory;
        public ResourceProvider()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            resourcesDirectory = Path.Combine(assemblyDirectory, "Resources");

        }
        public string ReadResource(string resourceName)
        {
            return File.ReadAllText(Path.Combine(resourcesDirectory, resourceName));
        }
    }
}
