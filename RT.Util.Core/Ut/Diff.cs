using RT.Util.ExtensionMethods;

namespace RT.Util;

public static partial class Ut
{
    /// <summary>
    ///     Computes a representation of the differences between <paramref name="old"/> and <paramref name="new"/> using the
    ///     specified options.</summary>
    /// <typeparam name="T">
    ///     The type of items to compare.</typeparam>
    /// <param name="old">
    ///     The first sequence of elements. Elements only in this sequence are considered "deleted".</param>
    /// <param name="new">
    ///     The second sequence of elements. Elements only in this sequence are considered "inserted".</param>
    /// <param name="comparer">
    ///     The equality comparer to use to compare items in the two sequences, or null to use the default comparer.</param>
    /// <param name="predicate">
    ///     If not null, determines which elements are "hard matches" (true) and which are "soft matches" (false). A "hard
    ///     match" element is one that can always be matched. A "soft match" element is only matched if it is adjacent to a
    ///     hard match.</param>
    /// <param name="postProcessor">
    ///     If not null, provides a post-processing step for parts of the diff in between consecutive matches. Without a
    ///     post-processing step, or when the post-processor returns null, these parts are returned as a sequence of deletes
    ///     followed by a sequence of inserts.</param>
    /// <returns>
    ///     An <c>IEnumerable&lt;Tuple&lt;T, DiffOp&gt;&gt;</c> representing the differences between <paramref name="old"/>
    ///     and <paramref name="new"/>. Each element in the returned <c>IEnumerable&lt;Tuple&lt;T, DiffOp&gt;&gt;</c>
    ///     corresponds either to an element present only in <paramref name="old"/> (the element is considered “deleted”), an
    ///     element present only in <paramref name="new"/> (the element is considered “inserted”) or an element present in
    ///     both.</returns>
    public static IEnumerable<(T item, DiffOp op)> Diff<T>(IEnumerable<T> old, IEnumerable<T> @new,
        IEqualityComparer<T> comparer = null, Func<T, bool> predicate = null,
        Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<(T item, DiffOp op)>> postProcessor = null)
    {
        if (old == null)
            throw new ArgumentNullException(nameof(old));
        if (@new == null)
            throw new ArgumentNullException(nameof(@new));
        return diffIterator(old as IList<T> ?? old.ToArray(), @new as IList<T> ?? @new.ToArray(), comparer ?? EqualityComparer<T>.Default, predicate, postProcessor);
    }

    private static IEnumerable<(T item, DiffOp op)> diffIterator<T>(IList<T> old, IList<T> @new,
        IEqualityComparer<T> comparer, Func<T, bool> predicate,
        Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<(T item, DiffOp op)>> postProcessor)
    {
        var startMatchIndex = 0;
        while (startMatchIndex < old.Count && startMatchIndex < @new.Count && comparer.Equals(old[startMatchIndex], @new[startMatchIndex]))
        {
            yield return (old[startMatchIndex], DiffOp.None);
            startMatchIndex++;
        }

        int endMatchIndex = 0;
        while (endMatchIndex < old.Count - startMatchIndex && endMatchIndex < @new.Count - startMatchIndex && comparer.Equals(old[old.Count - 1 - endMatchIndex], @new[@new.Count - 1 - endMatchIndex]))
            endMatchIndex++;

        if (old.Count > startMatchIndex + endMatchIndex && @new.Count > startMatchIndex + endMatchIndex)
            foreach (var x in diffImpl(old, @new, comparer, predicate, postProcessor, startMatchIndex, endMatchIndex))
                yield return x;
        else
        {
            for (int i = startMatchIndex; i < old.Count - endMatchIndex; i++)
                yield return (old[i], DiffOp.Del);
            for (int i = startMatchIndex; i < @new.Count - endMatchIndex; i++)
                yield return (@new[i], DiffOp.Ins);
        }

        for (int i = old.Count - endMatchIndex; i < old.Count; i++)
            yield return (old[i], DiffOp.None);
    }

    private sealed class DiffSeqLink
    {
        public int x;
        public int y;
        public DiffSeqLink prev;
        public override string ToString() => "{0}, {1}{2}".Fmt(x, y, prev == null ? null : " >");
    }

    private static IEnumerable<(T item, DiffOp op)> diffImpl<T>(IList<T> olda, IList<T> newa, IEqualityComparer<T> comparer, Func<T, bool> predicate, Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<(T item, DiffOp op)>> postProcessor, int startMatch, int endMatch)
    {
        var newhash = new Dictionary<T, List<int>>(comparer);
        for (int i = startMatch; i < newa.Count - endMatch; i++)
            if (predicate == null || predicate(newa[i]))
                newhash.AddSafe(newa[i], i);

        var sequences = new DiffSeqLink[olda.Count - startMatch - endMatch + 1];
        var newSequences = new DiffSeqLink[olda.Count - startMatch - endMatch + 1];
        var seqCount = 0;

        for (int xindex = startMatch; xindex < olda.Count - endMatch; xindex++)
        {
            var xpiece = olda[xindex];
            if (!newhash.ContainsKey(xpiece))
                continue;

            int k = 0;

            foreach (var yindex in newhash[xpiece])
            {
                while (k < seqCount && yindex > sequences[k].y)
                    k++;
                var last = k > 0 ? sequences[k - 1] : null;
                if (((k == seqCount) || (yindex < sequences[k].y)) &&
                    ((k == 0) || (yindex > last.y)))
                {
                    newSequences[k] = new DiffSeqLink { x = xindex, y = yindex, prev = last };
                    k++;
                    if (k > seqCount)
                    {
                        seqCount = k;
                        break;
                    }
                }
            }

            if (k > 0)
                Array.Copy(newSequences, sequences, k);
        }

        DiffSeqLink[] sequence = new DiffSeqLink[seqCount + 1];
        var index = 0;
        var sequenceRev = new DiffSeqLink { x = olda.Count - endMatch, y = newa.Count - endMatch, prev = seqCount > 0 ? sequences[seqCount - 1] : null };
        while (sequenceRev != null)
        {
            sequence[seqCount - index] = sequenceRev;
            sequenceRev = sequenceRev.prev;
            index++;
        }

        int curold = startMatch;
        int curnew = startMatch;
        foreach (var match in sequence)
        {
            while (curold < match.x && curnew < match.y && comparer.Equals(olda[curold], newa[curnew]))
            {
                yield return (olda[curold], DiffOp.None);
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
                // postProcessor can be null, or it can return null
                var postProcessed = postProcessor.NullOr(p => p(olda.Skip(curold).Take(mx - curold), newa.Skip(curnew).Take(my - curnew)));

                if (postProcessed != null)
                {
                    foreach (var token in postProcessed)
                        yield return token;
                    curold = mx;
                    curnew = my;
                }
                else
                {
                    for (; curold < mx; curold++)
                        yield return (olda[curold], DiffOp.Del);
                    for (; curnew < my; curnew++)
                        yield return (newa[curnew], DiffOp.Ins);
                }
            }

            while (curold < match.x && curnew < match.y)
            {
                yield return (olda[curold], DiffOp.None);
                curold++;
                curnew++;
            }
            if (curold < olda.Count - endMatch && curnew < newa.Count - endMatch)
            {
                yield return (olda[curold], DiffOp.None);
                curold++;
                curnew++;
            }
        }
    }
}

/// <summary>Indicates insertions and deletions in the output of <see cref="Ut.Diff{T}"/>.</summary>
public enum DiffOp
{
    /// <summary>Indicates that the relevant item has been deleted.</summary>
    Del = -1,

    /// <summary>Indicates that the relevant item has not changed.</summary>
    None = 0,

    /// <summary>Indicates that the relevant item has been inserted.</summary>
    Ins = 1
};
