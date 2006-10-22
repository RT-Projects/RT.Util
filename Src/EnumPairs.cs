using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
    /// <summary>
    /// A class which can enumerate all pairs of items in an IList.
    /// 
    /// Usage example: foreach (Pair<T,T> p in new EnumPairs(TheList)) {...}
    /// </summary>
    /// <typeparam name="T">The type of an item in the IList</typeparam>
    public class EnumPairs<T>
    {
        private IList<T> A;

        private EnumPairs() { }

        public EnumPairs(IList<T> List)
        {
            A = List;
        }

        public IEnumerator<Tuple<T, T>> GetEnumerator()
        {
            for (int i=0; i<A.Count-1; i++)
                for (int j=i+1; j<A.Count; j++)
                    yield return new Tuple<T, T>(A[i], A[j]);
        }
    }
}
