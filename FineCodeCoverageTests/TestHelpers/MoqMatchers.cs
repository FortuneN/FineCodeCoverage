using System;
using System.Collections.Generic;
using System.Linq;
using Moq;

namespace FineCodeCoverageTests.TestHelpers
{
    internal static class MoqMatchers
    {
        internal static IEnumerable<T> EnumerableExpected<T>(IEnumerable<T> expected, Func<T, T, bool> comparer)
        {
            return Match.Create<IEnumerable<T>>(enumerableArg =>
            {
                var list = enumerableArg.ToList();
                var expectedList = expected.ToList();
                if (list.Count != expectedList.Count)
                {
                    return false;
                }
                for (var i = 0; i < list.Count; i++)
                {
                    if (!comparer(list[i], expectedList[i]))
                    {
                        return false;
                    }
                }
                return true;
            });
        }
    }
}
