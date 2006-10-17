using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
    public class PackedBooleans
    {
        private byte[] FBooleans;
        public PackedBooleans(int Length) { FBooleans = new byte[(Length+7)/8]; }
        public bool Get(int Index) { return (FBooleans[Index/8] & (1 << (Index % 8))) != 0; }
        public void Set(int Index, bool Value)
        {
            if (Value)
                FBooleans[Index/8] |= (byte) (1 << (Index % 8));
            else
                FBooleans[Index/8] &= (byte) (~(1 << (Index % 8)));
        }
    }
}
