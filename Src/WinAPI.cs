using System;
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

        /// <summary>
        /// This field is statically initialised by calling QueryPerformanceFrequency.
        /// It contains the frequency of the performance counter for the current system.
        /// </summary>
        public static readonly long PerformanceFreq;

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

        #region Enums / flags

        /// <summary>Specifies a sound to be played back when displaying a message dialog.</summary>
        public enum MessageBeepType
        {
            /// <summary>Specifies the default sound.</summary>
            Default=-1,
            /// <summary>Specifies the OK sound.</summary>
            Ok=0x00000000,
            /// <summary>Specifies the error sound.</summary>
            Error=0x00000010,
            /// <summary>Specifies the question sound.</summary>
            Question=0x00000020,
            /// <summary>Specifies the warning sound.</summary>
            Warning=0x00000030,
            /// <summary>Specifies the information sound.</summary>
            Information=0x00000040,
        }

        [Flags]
        public enum SnapshotFlags: uint
        {
            HeapList=0x00000001,
            Process=0x00000002,
            Thread=0x00000004,
            Module=0x00000008,
            Module32=0x00000010,
            Inherit=0x80000000,
            All=0x0000001F
        }

        #endregion

        #region Structs

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

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
            public string szExeFile;
        }

        #endregion

        #region Function imports

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MessageBeep(
            MessageBeepType type);

        [DllImport("kernel32.dll")]
        public static extern void GlobalMemoryStatus(out MemoryStatus mem);

        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags,
           uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        public static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll")]
        public static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        #endregion

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

    }

}
