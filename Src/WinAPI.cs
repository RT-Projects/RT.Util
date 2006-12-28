/// WinAPI.cs  -  class defining some WinAPI function wrappers

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace RT.Util
{
    public enum MessageBeepType
    {
        Default=-1,
        Ok=0x00000000,
        Error=0x00000010,
        Question=0x00000020,
        Warning=0x00000030,
        Information=0x00000040,
    }

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
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        /// <summary>
        /// This field is statically initialised by calling QueryPerformanceFrequency.
        /// </summary>
        public static readonly long PerformanceFreq;

        [DllImport("user32.dll", SetLastError=true)]
        public static extern bool MessageBeep(
            MessageBeepType type);

        public struct MemoryStatus
        {

            public uint Length; //Length of struct
            public uint MemoryLoad; //Value from 0-100 represents memory usage
            public uint TotalPhysical;
            public uint AvailablePhysical;
            public uint TotalPageFile;
            public uint AvailablePageFile;
            public uint TotalVirtual;
            public uint AvailableVirtual;

        }

        [DllImport("kernel32.dll")]
        public static extern void GlobalMemoryStatus(out MemoryStatus mem);
    }

}
