#if EXPORT_UTIL
namespace RT.Util.ExtensionMethods.Obsolete;
#else
namespace RT.Internal;
#endif

#if !NET5_0_OR_GREATER
/// <summary>
///     Extension methods that exist in netstandard2.1 but not netstandard2.0 - so we want to keep them available but outside
///     of the main namespace where they mess with the framework-supplied versions. In hindsight multi-targeting would have
///     been a better idea.</summary>
#if EXPORT_UTIL
public
#endif
static class ObsoleteExtensions
{
    /// <summary>Creates a <see cref="HashSet{T}"/> from an enumerable collection.</summary>
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        return comparer == null ? new HashSet<T>(source) : new HashSet<T>(source, comparer);
    }

    /// <summary>
    ///     Returns a collection containing only the last <paramref name="count"/> items of the input collection. This method
    ///     enumerates the entire collection to the end once before returning. Note also that the memory usage of this method
    ///     is proportional to <paramref name="count"/>.</summary>
    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative.");
        if (count == 0)
            return Enumerable.Empty<T>();

        if (source is IList<T> list)
        {
            // Make this a local iterator-block function so that list.Count is only evaluated when enumeration begins
            IEnumerable<T> takeLastFromList()
            {
                for (int i = Math.Max(0, list.Count - count); i < list.Count; i++)
                    yield return list[i];
            }
            return takeLastFromList();
        }
        else if (source is ICollection<T> collection)
        {
            // Make this a local iterator-block function so that collection.Count is only evaluated when enumeration begins
            IEnumerable<T> takeLastFromCollection()
            {
                foreach (var elem in collection.Skip(Math.Max(0, collection.Count - count)))
                    yield return elem;
            }
            return takeLastFromCollection();
        }
        else
        {
            IEnumerable<T> takeLast()
            {
                var queue = new Queue<T>(count + 1);
                foreach (var item in source)
                {
                    if (queue.Count == count)
                        queue.Dequeue();
                    queue.Enqueue(item);
                }
                foreach (var item in queue)
                    yield return item;
            }
            return takeLast();
        }
    }
}
#endif
