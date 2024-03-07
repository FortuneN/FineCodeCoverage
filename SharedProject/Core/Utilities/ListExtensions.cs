using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Core.Utilities
{
    public static class List
    {
        // To be performed on a sorted list
        // Returns -1 for empty list or when all elements are outside the lower bounds
        // Compare fn to return 0 for element considered the lower bound
        // > 0 for lower bound greater than element

        public static int LowerBound<T>(this IList<T> list, Func<T, int> compare)
        {
            int first = 0;
            int count = list.Count;
            if (count == 0) return -1;

            while (count > 0)
            {
                int step = count / 2;
                int index = first + step;
                var result = compare(list[index]);
                if (result == 0)
                {
                    return index;
                }
                else if (result > 0)
                {
                    first = ++index;
                    count -= step + 1;
                }
                else
                {
                    count = step;
                }
            }

            return first != list.Count ? first : -1;
        }
    }
}
