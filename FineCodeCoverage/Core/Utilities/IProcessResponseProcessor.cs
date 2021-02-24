using System;

namespace FineCodeCoverage.Core.Utilities
{
    internal interface IProcessResponseProcessor
    {
        bool Process(ExecuteResponse executeResponse, Func<int, bool> exitCodeSuccessPredicate, bool throwError, string title, Action successCallback = null);
    }
}
