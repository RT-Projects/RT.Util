using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    public static partial class Ut
    {
        /// <summary>
        /// Computes a representation of the differences between <paramref name="old"/> and <paramref name="new"/> using the
        /// generic type parameter's default equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="old">The first sequence of elements. Elements only in this sequence are considered "deleted".</param>
        /// <param name="new">The second sequence of elements. Elements only in this sequence are considered "inserted".</param>
        /// <returns>An IEnumerable&lt;RT.Util.Collections.Tuple&lt;T, DiffOp&gt;&gt; representing the differences between <paramref name="old"/> and
        /// <paramref name="new"/>. Each element in the returned IEnumerable&lt;RT.Util.Collections.Tuple&lt;T, DiffOp&gt;&gt; corresponds either to an
        /// element present only in <paramref name="old"/> (the element is considered "deleted"), an element present only in 
        /// <paramref name="new"/> (the element is considered "inserted") or an element present in both.</returns>
        public static IEnumerable<RT.Util.Collections.Tuple<T, DiffOp>> Diff<T>(IEnumerable<T> old, IEnumerable<T> @new)
        {
            return Diff(old, @new, new DiffOptions<T>());
        }

        /// <summary>
        /// Computes a representation of the differences between <paramref name="old"/> and <paramref name="new"/> using the specified options.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="old">The first sequence of elements. Elements only in this sequence are considered "deleted".</param>
        /// <param name="new">The second sequence of elements. Elements only in this sequence are considered "inserted".</param>
        /// <param name="options">An instance of <see cref="DiffOptions&lt;T&gt;"/> which specifies additional options.</param>
        /// <returns>An IEnumerable&lt;RT.Util.Collections.Tuple&lt;T, DiffOp&gt;&gt; representing the differences between <paramref name="old"/> and
        /// <paramref name="new"/>. Each element in the returned IEnumerable&lt;RT.Util.Collections.Tuple&lt;T, DiffOp&gt;&gt; corresponds either to an
        /// element present only in <paramref name="old"/> (the element is considered "deleted"), an element present only in 
        /// <paramref name="new"/> (the element is considered "inserted") or an element present in both.</returns>
        public static IEnumerable<RT.Util.Collections.Tuple<T, DiffOp>> Diff<T>(IEnumerable<T> old, IEnumerable<T> @new, DiffOptions<T> options)
        {
            if (old == null)
                throw new ArgumentNullException("old");
            if (@new == null)
                throw new ArgumentNullException("new");

            IEqualityComparer<T> comparer = options.Comparer == null ? EqualityComparer<T>.Default : options.Comparer;

            var olda = old.ToArray();
            var newa = @new.ToArray();

            var startMatchIndex = 0;
            while (startMatchIndex < olda.Length && startMatchIndex < newa.Length && comparer.Equals(olda[startMatchIndex], newa[startMatchIndex]))
                startMatchIndex++;
            if (startMatchIndex > 0)
            {
                foreach (var x in olda.Take(startMatchIndex))
                    yield return new RT.Util.Collections.Tuple<T, DiffOp>(x, DiffOp.None);
                olda = olda.Skip(startMatchIndex).ToArray();
                newa = newa.Skip(startMatchIndex).ToArray();
            }

            IEnumerable<T> endmatch = null;
            if (olda.Length > 0 && newa.Length > 0)
            {
                int endMatchIndex = 0;
                while (endMatchIndex < olda.Length && endMatchIndex < newa.Length && comparer.Equals(olda[olda.Length - 1 - endMatchIndex], newa[newa.Length - 1 - endMatchIndex]))
                    endMatchIndex++;
                if (endMatchIndex > 0)
                {
                    endmatch = olda.Skip(olda.Length - endMatchIndex);
                    olda = olda.Take(olda.Length - endMatchIndex).ToArray();
                    newa = newa.Take(newa.Length - endMatchIndex).ToArray();
                }
            }

            if (olda.Length > 0 && newa.Length > 0)
                foreach (var x in diff(olda, newa, comparer, options.Predicate, options.PostProcessor))
                    yield return x;
            else if (olda.Length > 0)
                foreach (var x in olda)
                    yield return new RT.Util.Collections.Tuple<T, DiffOp>(x, DiffOp.Del);
            else if (newa.Length > 0)
                foreach (var x in newa)
                    yield return new RT.Util.Collections.Tuple<T, DiffOp>(x, DiffOp.Ins);

            if (endmatch != null)
                foreach (var x in endmatch)
                    yield return new RT.Util.Collections.Tuple<T, DiffOp>(x, DiffOp.None);
        }

        private sealed class diffSeqLink { public int x; public int y; public diffSeqLink prev; }
        private static IEnumerable<RT.Util.Collections.Tuple<T, DiffOp>> diff<T>(T[] olda, T[] newa, IEqualityComparer<T> comparer, Func<T, bool> predicate, Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<RT.Util.Collections.Tuple<T, DiffOp>>> postProcessor)
        {
            var newhash = new Dictionary<T, List<int>>();
            for (int i = 0; i < newa.Length; i++)
                if (predicate == null || predicate(newa[i]))
                    newhash.AddSafe(newa[i], i);

            Dictionary<int, diffSeqLink> sequences = new Dictionary<int, diffSeqLink> { { 0, new diffSeqLink { y = -1 } } };
            for (int xindex = 0; xindex < olda.Length; xindex++)
            {
                var xpiece = olda[xindex];
                if (!newhash.ContainsKey(xpiece))
                    continue;

                int k = 0;
                Dictionary<int, diffSeqLink> newSequences = new Dictionary<int, diffSeqLink>(sequences); // creates a copy
                int maxk = sequences.Count - 1;

                foreach (var yindex in newhash[xpiece])
                {
                    while (k < maxk && yindex > sequences[k + 1].y)
                        k++;
                    if (((k == maxk) || (yindex < sequences[k + 1].y)) &&
                        ((k == 0) || (yindex > sequences[k].y)))
                    {
                        k++;
                        if (k > 1)
                            newSequences[k] = new diffSeqLink { x = xindex, y = yindex, prev = sequences[k - 1] };
                        else
                            newSequences[k] = new diffSeqLink { x = xindex, y = yindex, prev = null };

                        if (k > maxk)
                            break;
                    }
                }
                sequences = newSequences;
            }

            diffSeqLink origSequenceRev = new diffSeqLink { x = olda.Length, y = newa.Length, prev = sequences.Count > 1 ? sequences[sequences.Count - 1] : null };

            var length = 0;
            for (var sequenceRev = origSequenceRev; sequenceRev != null; sequenceRev = sequenceRev.prev)
                length++;
            diffSeqLink[] sequence = new diffSeqLink[length];
            var index = 0;
            for (var sequenceRev = origSequenceRev; sequenceRev != null; sequenceRev = sequenceRev.prev)
                sequence[length - 1 - (index++)] = sequenceRev;

            int curold = 0;
            int curnew = 0;
            foreach (var match in sequence)
            {
                while (curold < match.x && curnew < match.y && comparer.Equals(olda[curold], newa[curnew]))
                {
                    yield return new RT.Util.Collections.Tuple<T, DiffOp>(olda[curold], DiffOp.None);
                    curold++;
                    curnew++;
                }

                var mx = match.x - 1;
                var my = match.y - 1;
                while (mx > curold && my > curnew && comparer.Equals(olda[mx], newa[my]))
                {
                    mx--;
                    my--;
                }
                mx++;
                my++;

                if (curold < mx || curnew < my)
                {
                    if (postProcessor != null)
                    {
                        foreach (var token in postProcessor(olda.Skip(curold).Take(mx - curold), newa.Skip(curnew).Take(my - curnew)))
                            yield return token;
                        curold = mx;
                        curnew = my;
                    }
                    else
                    {
                        for (; curold < mx; curold++)
                            yield return new RT.Util.Collections.Tuple<T, DiffOp>(olda[curold], DiffOp.Del);
                        for (; curnew < my; curnew++)
                            yield return new RT.Util.Collections.Tuple<T, DiffOp>(newa[curnew], DiffOp.Ins);
                    }
                }

                while (curold < match.x && curnew < match.y)
                {
                    yield return new RT.Util.Collections.Tuple<T, DiffOp>(olda[curold], DiffOp.None);
                    curold++;
                    curnew++;
                }
                if (curold < olda.Length && curnew < newa.Length)
                {
                    yield return new RT.Util.Collections.Tuple<T, DiffOp>(olda[curold], DiffOp.None);
                    curold++;
                    curnew++;
                }
            }
        }
    }

    /// <summary>
    /// Specifies various options for <see cref="Ut.Diff&lt;T&gt;(IEnumerable&lt;T&gt;, IEnumerable&lt;T&gt;, DiffOptions&lt;T&gt;)"/>.
    /// This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequences to compare.</typeparam>
    public struct DiffOptions<T>
    {
        /// <summary>The equality comparer to use to compare items in the two sequences.</summary>
        public IEqualityComparer<T> Comparer;

        /// <summary>If not null, determines which elements are "hard matches" (true) and which are "soft matches" (false).
        /// A "hard match" element is one that can always be matched. A "soft match" element is only matched if it is 
        /// completely surrounded by hard matches.</summary>
        public Func<T, bool> Predicate;

        /// <summary>If not null, provides a post-processing step for parts of the diff in between consecutive matches.
        /// Without a post-processing step, these parts are returned as a sequence of deletes followed by a sequence of
        /// inserts.</summary>
        public Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<RT.Util.Collections.Tuple<T, DiffOp>>> PostProcessor;
    }

    /// <summary>Indicates insertions and deletions in the output of
    /// <see cref="Ut.Diff&lt;T&gt;(IEnumerable&lt;T&gt;, IEnumerable&lt;T&gt;, DiffOptions&lt;T&gt;)"/>.
    /// </summary>
    public enum DiffOp
    {
        /// <summary>
        /// Indicates that the relevant item has not changed.
        /// </summary>
        None,

        /// <summary>
        /// Indicates that the relevant item has been inserted.
        /// </summary>
        Ins,

        /// <summary>
        /// Indicates that the relevant item has been deleted.
        /// </summary>
        Del
    };
}
