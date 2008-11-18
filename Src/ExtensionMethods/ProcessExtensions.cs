using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Management;
using RT.Util;
using System.Runtime.InteropServices;
using RT.Util.Collections;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Contains extension methods for the Process class, as well as utility methods
    /// which are logically static extensions on the Process class but have to be
    /// invoked as static methods of this class.
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// For each process in the system, enumerates a tuple of parent-process-id,process-id.
        /// </summary>
        public static IEnumerable<Tuple<int,int>> ParentChildProcessIds()
        {
            WinAPI.PROCESSENTRY32 procEntry = new WinAPI.PROCESSENTRY32();
            procEntry.dwSize = (uint)Marshal.SizeOf(typeof(WinAPI.PROCESSENTRY32));
            IntPtr handleToSnapshot = WinAPI.CreateToolhelp32Snapshot((uint)WinAPI.SnapshotFlags.Process, 0);
            try
            {
                if (WinAPI.Process32First(handleToSnapshot, ref procEntry))
                {
                    do
                    {
                        yield return new Tuple<int, int>((int)procEntry.th32ParentProcessID, (int)procEntry.th32ProcessID);
                    } while (WinAPI.Process32Next(handleToSnapshot, ref procEntry));
                }
                else
                {
                    throw new RTException("Process enumeration failed; win32 error code: {0}", Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                WinAPI.CloseHandle(handleToSnapshot);
            }
        }

        /// <summary>
        /// Returns a list of all child processes of this process, enumerated recursively.
        /// </summary>
        public static List<int> ChildProcessIds(this Process process, bool recursive)
        {
            Dictionary<int, List<int>> tree = new Dictionary<int, List<int>>();
            foreach (var pair in ParentChildProcessIds())
                tree.AddSafe(pair.E1, pair.E2);

            if (!recursive)
            {
                if (tree.ContainsKey(process.Id))
                    return tree[process.Id];
                else
                    return new List<int>();
            }
            else
            {
                List<int> children = new List<int>();
                Queue<int> todo = new Queue<int>();
                todo.Enqueue(process.Id);
                while (todo.Count > 0)
                {
                    int id = todo.Dequeue();
                    if (tree.ContainsKey(id))
                        foreach (int child_id in tree[id])
                        {
                            children.Add(child_id);
                            todo.Enqueue(child_id);
                        }
                }
                return children;
            }
        }

        /// <summary>
        /// Kills this process and all children. Returns a list of Process instances
        /// containing this process as well as all children. Swallows all exceptions
        /// and does not wait for processes to die or check that they died.
        /// </summary>
        public static void KillWithChildren(this Process process)
        {
            List<int> tokill = process.ChildProcessIds(true);
            tokill.Add(process.Id);

            foreach (int id in tokill)
                try { Process.GetProcessById(id).Kill(); }
                catch { }
        }
    }
}
