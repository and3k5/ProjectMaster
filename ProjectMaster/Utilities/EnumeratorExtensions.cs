using System.Collections;
using System.Collections.Generic;

namespace ProjectMaster.Utilities
{
    public static class EnumeratorExtensions
    {
        public static T[] IterateThrough<T>(this IEnumerator<T> enumerator)
        {
            var list = new List<T>();

            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            return list.ToArray();
        }

        public static T[] IterateThrough<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.GetEnumerator().IterateThrough();
        }

        public static T[] IterateThrough<T>(this IEnumerator enumerator)
        {
            var list = new List<T>();

            while (enumerator.MoveNext())
                list.Add((T) enumerator.Current);

            return list.ToArray();
        }

        public static T[] IterateThrough<T>(this IEnumerable enumerable)
        {
            return enumerable.GetEnumerator().IterateThrough<T>();
        }
    }
}