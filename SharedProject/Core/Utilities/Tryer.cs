using System;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
    internal static class Tryer
    {
        public static async Task TryAsync(Func<Task> func)
        {
            try
            {
                await func();
            }
            catch { }
        }

    }
}
