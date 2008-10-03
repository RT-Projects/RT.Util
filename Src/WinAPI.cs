using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace RT.Util
{
    /// <summary>Specifies a sound to be played back when displaying a message dialog.</summary>
    public enum MessageBeepType
    {
        /// <summary>Specifies the default sound.</summary>
        Default = -1,
        /// <summary>Specifies the OK sound.</summary>
        Ok = 0x00000000,
        /// <summary>Specifies the error sound.</summary>
        Error = 0x00000010,
        /// <summary>Specifies the question sound.</summary>
        Question = 0x00000020,
        /// <summary>Specifies the warning sound.</summary>
        Warning = 0x00000030,
        /// <summary>Specifies the information sound.</summary>
        Information = 0x00000040,
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

        [DllImport("user32.dll", SetLastError = true)]
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
