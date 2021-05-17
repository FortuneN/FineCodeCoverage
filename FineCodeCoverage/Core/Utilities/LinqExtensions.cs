using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
    internal static class LinqExtensions
    {
        public static TTransformed SelectFirstNonNull<T, TTransformed>(this IEnumerable<T> source, Func<T, TTransformed> select) where TTransformed : class
        {
            foreach (var element in source)
            {
                var selected = select(element);
                if (selected != null)
                {
                    return selected;
                }

            }
            return null;
        }
    }
}
