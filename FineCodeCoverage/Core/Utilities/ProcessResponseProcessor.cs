using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IProcessResponseProcessor))]
    internal class ProcessResponseProcessor : IProcessResponseProcessor
    {
        private readonly ILogger logger;

        [ImportingConstructor]
        public ProcessResponseProcessor(ILogger logger)
        {
            this.logger = logger;
        }
        public async Task<bool> ProcessAsync(ExecuteResponse result, Func<int, bool> exitCodeSuccessPredicate, bool throwError, string title,Func<Task> successCallback = null)
        {
            if (result != null)
            {
                if (!exitCodeSuccessPredicate(result.ExitCode))
                {
                    if (throwError)
                    {
                        throw new Exception(result.Output);
                    }

                    logger.Log($"{title} Error", result.Output);
                    return false;
                }

                logger.Log(title, result.Output);
                if(successCallback != null)
                {
                    await successCallback();
                }
                return true;
            }
            return false;

        }
    }
}
