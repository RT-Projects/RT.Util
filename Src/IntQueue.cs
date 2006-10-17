using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
    public class IntQueue
    {
        private int[] FContents;
        private int FFirstElement, FLastElementPlusOne;
        private bool FEmpty;

        public bool Empty { get { return FEmpty; } }
        public int Count
        {
            get
            {
                if (FEmpty) return 0;
                int i = FLastElementPlusOne - FFirstElement;
                return (i <= 0) ? i + FContents.Length : i;
            }
        }

        public IntQueue()
        {
            FContents = new int[64];
            FFirstElement = 0;
            FLastElementPlusOne = 0;
            FEmpty = true;
        }
        public void Add(int i)
        {
            if (FFirstElement == FLastElementPlusOne && !FEmpty)
            {
                int newLength = 2*FContents.Length;
                int[] newContents = new int[newLength];
                for (int j = 0; j < FFirstElement; j++)
                    newContents[j] = FContents[j];
                for (int j = FFirstElement; j < FContents.Length; j++)
                    newContents[j+FContents.Length] = FContents[j];
                FFirstElement += FContents.Length;
                FContents = newContents;
            }
            FContents[FLastElementPlusOne] = i;
            FLastElementPlusOne++;
            if (FLastElementPlusOne == FContents.Length) FLastElementPlusOne = 0;
            FEmpty = false;
        }
        public int Extract()
        {
            int Result = FContents[FFirstElement];
            FFirstElement++;
            if (FFirstElement == FContents.Length) FFirstElement = 0;
            if (FFirstElement == FLastElementPlusOne) FEmpty = true;
            return Result;
        }
    }
}
