using System;
using System.ComponentModel.Composition;

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
        public bool Process(ExecuteResponse result, Func<int, bool> exitCodeSuccessPredicate, bool throwError, string title, Action successCallback = null)
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
                successCallback?.Invoke();
                return true;
            }
            return false;

        }
    }
}
