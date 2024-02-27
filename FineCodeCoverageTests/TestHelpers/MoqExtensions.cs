using Moq;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.TestHelpers
{
    internal static class MoqExtensions
    {
        public static IEnumerable<IInvocation> GetMethodInvocations(this IInvocationList invocationList, string methodName)
        {
            return invocationList.Where(invocation => invocation.Method.Name == methodName);
        }

        public static IEnumerable<IReadOnlyList<object>> GetMethodInvocationArguments(this IInvocationList invocationList, string methodName)
        {
            return invocationList.GetMethodInvocations(methodName).Select(invocation => invocation.Arguments);
        }

        public static IEnumerable<T> GetMethodInvocationSingleArgument<T>(this IInvocationList invocationList, string methodName)
        {
            return invocationList.GetMethodInvocationArguments(methodName).Select(args => (T)args.Single());
        }
    }
}
