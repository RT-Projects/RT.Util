using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RT.Util.Collections;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="Process"/> type, as well as utility methods which are logically 
    /// static extensions on the Process type but have to be invoked as static methods of this class.
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// For each process in the system, enumerates a tuple of parent-process-id,process-id.
        /// </summary>
        public static IEnumerable<(int parentProcessId, int processId)> ParentChildProcessIds()
        {
            var procEntry = new WinAPI.PROCESSENTRY32();
            procEntry.dwSize = (uint) Marshal.SizeOf(typeof(WinAPI.PROCESSENTRY32));
            var handleToSnapshot = WinAPI.CreateToolhelp32Snapshot((uint) WinAPI.SnapshotFlags.Process, 0);
            try
            {
                if (WinAPI.Process32First(handleToSnapshot, ref procEntry))
                {
                    do
                        yield return ((int) procEntry.th32ParentProcessID, (int) procEntry.th32ProcessID);
                    while (WinAPI.Process32Next(handleToSnapshot, ref procEntry));
                }
                else
                {
                    throw new InternalErrorException("Process enumeration failed; win32 error code: {0}".Fmt(Marshal.GetLastWin32Error()));
                }
            }
            finally
            {
                WinAPI.CloseHandle(handleToSnapshot);
            }
        }

        /// <summary>Returns a list of child processes of this process.</summary>
        /// <param name="process">The process to return the children of.</param>
        /// <param name="recursive">If true, all the children's children are included recursively. If false, only direct children are included.</param>
        public static List<int> ChildProcessIds(this Process process, bool recursive)
        {
            var tree = new Dictionary<int, List<int>>();
            foreach (var pair in ParentChildProcessIds())
                tree.AddSafe(pair.parentProcessId, pair.processId);

            if (!recursive)
            {
                if (tree.ContainsKey(process.Id))
                    return tree[process.Id];
                else
                    return new List<int>();
            }
            else
            {
                var children = new List<int>();
                var todo = new Queue<int>();
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
        /// Kills this process and all children. Swallows all exceptions
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
