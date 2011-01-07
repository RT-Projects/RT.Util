using System;
using System.Runtime.InteropServices;
using System.Text;

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

        /// <summary>
        /// Normally an unhandled exception will result in Windows showing a generic error dialog, while keeping the
        /// process alive and stopped. This method will disable that: an unhandled exception will result in the process
        /// terminating quietly.
        /// </summary>
        public static void DisableGeneralProtectionFaultErrorBox()
        {
            SetErrorMode(SetErrorMode(0) | ErrorModes.SEM_NOGPFAULTERRORBOX);
        }

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

        #region Enums / flags

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

        [Flags]
        public enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F
        }

        [Flags]
        public enum ErrorModes : uint
        {
            SYSTEM_DEFAULT = 0x0,
            SEM_FAILCRITICALERRORS = 0x0001,
            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
            SEM_NOGPFAULTERRORBOX = 0x0002,
            SEM_NOOPENFILEERRORBOX = 0x8000
        }

        #endregion

        #region Constants

        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // Low-Level Keyboard Constants
        public const int HC_ACTION = 0;
        public const int LLKHF_EXTENDED = 0x1;
        public const int LLKHF_INJECTED = 0x10;
        public const int LLKHF_ALTDOWN = 0x20;
        public const int LLKHF_UP = 0x80;
        public const int WH_KEYBOARD_LL = 13;

        public const int WM_ACTIVATE = 0x0006;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_CHAR = 0x102;
        public const int WM_SYSKEYDOWN = 0x104;
        public const int WM_SYSKEYUP = 0x105;

        public const int HWND_TOPMOST = -1;
        public const int HWND_NOTOPMOST = -2;
        public const int MA_NOACTIVATE = 3;

        // SetWindowPos constants
        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOZORDER = 0x0004;
        public const int SWP_NOACTIVATE = 0x0010;
        public const int SWP_SHOWWINDOW = 0x0040;
        public const int SWP_HIDEWINDOW = 0x0080;
        public const int SWP_ASYNCWINDOWPOS = 0x4000;
        public const int TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE; // ?!

        // ShowWindow constants
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MAXIMIZE = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        public const int SW_FORCEMINIMIZE = 11;

        // Virtual-key code to scancode conversion constants
        public const uint MAPVK_VK_TO_VSC = 0x00;
        public const uint MAPVK_VSC_TO_VK = 0x01;
        public const uint MAPVK_VK_TO_CHAR = 0x02;
        public const uint MAPVK_VSC_TO_VK_EX = 0x03;
        public const uint MAPVK_VK_TO_VSC_EX = 0x04;

        // GetStdHandle constants
        public const int STD_INPUT_HANDLE = -10;
        public const int STD_OUTPUT_HANDLE = -11;
        public const int STD_ERROR_HANDLE = -12;

        // GetFileType constants
        public const int FILE_TYPE_UNKNOWN = 0x0000;
        public const int FILE_TYPE_DISK = 0x0001;
        public const int FILE_TYPE_CHAR = 0x0002;
        public const int FILE_TYPE_PIPE = 0x0003;

        // For keybd_event
        public const int KEYEVENTF_KEYUP = 0x2;
        public const int KEYEVENTF_UNICODE = 0x4;

        // For the 'type' field in the INPUT struct
        public const int INPUT_MOUSE = 0;
        public const int INPUT_KEYBOARD = 1;
        public const int INPUT_HARDWARE = 2;

        public const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        public const uint DONT_RESOLVE_DLL_REFERENCES = 0x00000001;
        public const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
        public const uint LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010;
        public const uint LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040;

        // ListBox
        public const uint LB_SETCARETINDEX = 0x019E;
        public const uint LB_GETCARETINDEX = 0x019F;

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
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        public struct KeyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
        }

        public struct INPUT
        {
            public int Type;
            public MOUSEKEYBDHARDWAREINPUT SpecificInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        /// <summary>
        /// Defines the callback type for a keyboard hook procedure.
        /// </summary>
        public delegate int KeyboardHookProc(int code, int wParam, ref KeyboardHookStruct lParam);

        #region Function imports

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MessageBeep(MessageBeepType type);

        [DllImport("kernel32.dll")]
        public static extern void GlobalMemoryStatus(out MemoryStatus mem);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        public static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll")]
        public static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern int UnhookWindowsHookEx(int hHook);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        public static extern int GetLastError();

        [DllImport("kernel32.dll")]
        public static extern uint GetFileType(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardLayoutName([Out] StringBuilder pwszKLID);

        [DllImport("user32.dll")]
        public static extern uint GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);

        [DllImport("user32.dll")]
        public static extern int ActivateKeyboardLayout(IntPtr nkl, uint Flags);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Sets the windows hook, do the desired event, one of hInstance or threadId must be non-null
        /// </summary>
        /// <param name="idHook">The id of the event you want to hook</param>
        /// <param name="callback">The callback.</param>
        /// <param name="hInstance">The handle you want to attach the event to, can be null</param>
        /// <param name="threadId">The thread you want to attach the event to, can be null</param>
        /// <returns>a handle to the desired hook</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, WinAPI.KeyboardHookProc callback, IntPtr hInstance, uint threadId);

        /// <summary>
        /// Unhooks the windows hook.
        /// </summary>
        /// <param name="hInstance">The hook handle that was returned from SetWindowsHookEx</param>
        /// <returns>True if successful, false otherwise</returns>
        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        /// <summary>
        /// Calls the next hook.
        /// </summary>
        /// <param name="idHook">The hook id</param>
        /// <param name="nCode">The hook code</param>
        /// <param name="wParam">The wparam.</param>
        /// <param name="lParam">The lparam.</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref KeyboardHookStruct lParam);

        /// <summary>
        /// Loads the library.
        /// </summary>
        /// <param name="lpFileName">Name of the library</param>
        /// <returns>A handle to the library</returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleOutputCP(uint wCodePageID);

        /// <summary>Returns the window handle of the console window associated with the current process, or IntPtr.Zero if there is no console window.</summary>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        /// <summary>Shows or hides the window identified by <paramref name="hWnd"/>. For <paramref name="nCmdShow"/>, use <see cref="SW_SHOW"/> et al.</summary>
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        static extern ErrorModes SetErrorMode(ErrorModes uMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryThreadCycleTime(IntPtr threadHandle, out ulong cycleTime);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThread();

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint SendMessage(IntPtr hWnd, uint msg, uint wParam, uint lParam);

        #endregion

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Retrieves the window handle of the focused control in the currently active window.
        /// </summary>
        /// <param name="ownHandle">The handle to one of your own windows.</param>
        public static IntPtr GetFocusedControlInActiveWindow(IntPtr ownHandle)
        {
            IntPtr activeWindowHandle = GetForegroundWindow();

            IntPtr activeWindowThread = GetWindowThreadProcessId(activeWindowHandle, IntPtr.Zero);
            IntPtr thisWindowThread = GetWindowThreadProcessId(ownHandle, IntPtr.Zero);

            AttachThreadInput(activeWindowThread, thisWindowThread, true);
            IntPtr focusedControlHandle = GetFocus();
            AttachThreadInput(activeWindowThread, thisWindowThread, false);

            return focusedControlHandle;
        }
    }
}
