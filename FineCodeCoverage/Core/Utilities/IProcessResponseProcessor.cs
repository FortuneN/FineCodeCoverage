using System;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
    internal interface IProcessResponseProcessor
    {
        Task<bool> ProcessAsync(ExecuteResponse executeResponse, Func<int, bool> exitCodeSuccessPredicate, bool throwError, string title, Func<Task> successCallback = null);
    }
}
