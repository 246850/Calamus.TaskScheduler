using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure
{
    public static class WhereIfExtensions
    {
        public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
        {
            return condition ? source.Where(predicate) : source;
        }
    }
}
