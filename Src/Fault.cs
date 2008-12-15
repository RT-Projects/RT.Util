using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace RT.Util
{
    /// <summary>
    /// This class can be used to keep track of non-fatal problems which are not reported
    /// to the user directly but can be obtained by the developer.
    /// </summary>
    public static class Fault
    {
        /// <summary>
        /// Represents an entry in the set of recorded faults.
        /// </summary>
        public class FaultEntry
        {
            /// <summary>The time at which the fault was recorded</summary>
            public DateTime Timestamp;
            /// <summary>The message that was provided</summary>
            public string Message;
            /// <summary>Name of the code file containing the line of code which reported the fault</summary>
            public string Filename;
            /// <summary>Name of the method which reported the fault</summary>
            public string Method;
            /// <summary>The number of the line which reported the fault</summary>
            public int LineNumber;
            /// <summary>Identifies the thread reporting the fault</summary>
            public Thread Thread;

            /// <summary>
            /// Converts an entry to a human-readable string describing the fault.
            /// </summary>
            public override string ToString()
            {
                return ToString("{time}: {file}[{line}] - fault in {func}. {msg}{thread}");
            }

            /// <summary>
            /// Converts an entry to a human-readable string describing the fault, using the specified format string.
            /// The following fields are substituted with values:
            /// <list type="bullet">
            /// <item>{time}</item>
            /// <item>{file}</item>
            /// <item>{line}</item>
            /// <item>{func}</item>
            /// <item>{msg}</item>
            /// <item>{thread}</item>
            /// </list>
            /// </summary>
            public string ToString(string fmt)
            {
                string s = fmt;
                s = s.Replace("{time}", Timestamp.ToShortTimeString());
                s = s.Replace("{file}", Filename);
                s = s.Replace("{line}", LineNumber.ToString());
                s = s.Replace("{func}", Method);
                s = s.Replace("{msg}", Message);
                if (Thread != null)
                    s = s.Replace("{thread}", "(thread:" + Thread.Name + ")");
                return s;
            }
        }

        /// <summary>
        /// <para>The list of all fault entries.</para>
        /// 
        /// <para>Multi-threading: if you access the Entries list directly, you should either
        /// ensure that no other thread calls AddMT, or lock the Entries list for the
        /// duration of the processing.</para>
        /// </summary>
        public static List<FaultEntry> Entries = new List<FaultEntry>();

        /// <summary>
        /// Adds a message to the Fault list. The fault entry will contain the specified
        /// message as well as a timestamp and full information about the function
        /// calling Add, including file &amp; line number.
        /// 
        /// Multi-threading: use AddMT instead.
        /// </summary>
        public static void Add(string message)
        {
            // Skip the stack frame for the current function
            StackFrame sf = new StackFrame(1, true);

            // Create the new fault entry
            FaultEntry fe = new FaultEntry();
            fe.Timestamp = DateTime.Now;
            fe.Message = message;
            fe.Filename = sf.GetFileName();
            fe.Method = sf.GetMethod().Name;
            fe.LineNumber = sf.GetFileLineNumber();
            fe.Thread = null;

            // Add to the list
            Entries.Add(fe);
        }

        /// <summary>
        /// Adds a message to the Fault list. This method can be safely called from
        /// multiple threads. This method will also store a reference to the thread
        /// which invoked it. See also information about Add.
        /// </summary>
        public static void AddMT(string message)
        {
            // Skip the stack frame for the current function
            StackFrame sf = new StackFrame(1, true);

            // Create the new fault entry
            FaultEntry fe = new FaultEntry();
            fe.Timestamp = DateTime.Now;
            fe.Message = message;
            fe.Filename = sf.GetFileName();
            fe.Method = sf.GetMethod().Name;
            fe.LineNumber = sf.GetFileLineNumber();
            fe.Thread = Thread.CurrentThread;

            // Add to the list
            lock (Entries)
                Entries.Add(fe);
        }
    }
}
