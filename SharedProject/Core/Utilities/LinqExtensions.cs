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

        public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, System.Func<T, bool> predicate)
            => source == null
                ? throw new ArgumentNullException(nameof(source))
                : predicate == null ? throw new ArgumentNullException(nameof(predicate)) :
                TakeUntilIterator<T>(source, predicate);

        private static IEnumerable<T> TakeUntilIterator<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (T item in source)
            {
                yield return item;
                if (predicate(item))
                    yield break;
            }
        }
    }
}
