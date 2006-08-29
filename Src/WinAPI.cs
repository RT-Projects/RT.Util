/// WinAPI.cs  -  class defining some WinAPI function wrappers

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace RT.Util
{
    /// <summary>
    /// WinAPI function wrappers
    /// </summary>
    public static class WinAPI
    {
        static WinAPI()
        {
            QueryPerformanceFrequency(out PerformanceFreq);
        }

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        public static readonly long PerformanceFreq;
    }

}
