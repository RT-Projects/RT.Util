using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

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
            public DateTime Timestamp;
            public string Message;
            public string Filename;
            public string Method;
            public int LineNumber;
            public Thread Thread;

            public override string ToString()
            {
                return ToString("{time}: {file}[{line}] - fault in {func}. {msg}{thread}");
            }

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
        /// The list of all fault entries
        /// 
        /// Multi-threading: if you access the Entries list directly, you should either
        /// ensure that no other thread calls AddMT, or lock the Entries list for the
        /// duration of the processing.
        /// </summary>
        public static List<FaultEntry> Entries = new List<FaultEntry>();

        /// <summary>
        /// Adds a message to the Fault list. The fault entry will contain the specified
        /// message as well as a timestamp and full information about the function
        /// calling Add, including file &amp; line number.
        /// 
        /// Multi-threading: use AddMT instead.
        /// </summary>
        public static void Add(string Message)
        {
            // Skip the stack frame for the current function
            StackFrame sf = new StackFrame(1, true);

            // Create the new fault entry
            FaultEntry FE = new FaultEntry();
            FE.Timestamp = DateTime.Now;
            FE.Message = Message;
            FE.Filename = sf.GetFileName();
            FE.Method = sf.GetMethod().Name;
            FE.LineNumber = sf.GetFileLineNumber();
            FE.Thread = null;

            // Add to the list
            Entries.Add(FE);
        }

        /// <summary>
        /// Adds a message to the Fault list. This method can be safely called from
        /// multiple threads. This method will also store a reference to the thread
        /// which invoked it. See also information about Add.
        /// </summary>
        public static void AddMT(string Message)
        {
            // Skip the stack frame for the current function
            StackFrame sf = new StackFrame(1, true);

            // Create the new fault entry
            FaultEntry FE = new FaultEntry();
            FE.Timestamp = DateTime.Now;
            FE.Message = Message;
            FE.Filename = sf.GetFileName();
            FE.Method = sf.GetMethod().Name;
            FE.LineNumber = sf.GetFileLineNumber();
            FE.Thread = Thread.CurrentThread;

            // Add to the list
            lock (Entries)
                Entries.Add(FE);
        }
    }
}
