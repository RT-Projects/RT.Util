/// Utils.cs  -  utility functions and classes

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using RT.Util.Dialogs;

namespace RT.Util
{
    #region ConfirmEvent

    /// <summary>
    /// EventArgs for the ConfirmEventHandler delegate.
    /// </summary>
    public class ConfirmEventArgs : EventArgs
    {
        /// <summary>
        /// Set this to true/false to confirm or cancel the action.ssss
        /// </summary>
        public bool ConfirmOK;
    }

    /// <summary>
    /// A general-purpose event type which lets the caller confirm an action.
    /// </summary>
    public delegate void ConfirmEventHandler(object sender, ConfirmEventArgs e);

    #endregion

    /// <summary>
    /// This class offers some generic static functions which are hard to categorize
    /// under any more specific classes.
    /// </summary>
    public static class Ut
    {
        /// <summary>
        /// An application-wide random number generator - use this generator if all you
        /// need is a random number. Create a new generator only if you really need to.
        /// </summary>
        public static Random Rnd = new Random();

        /// <summary>
        /// Stores a copy of the value generated by AppPath. This way AppPath
        /// only needs to generate it once.
        /// </summary>
        private static string CachedAppPath = "";

        /// <summary>
        /// Returns the application path with a directory separator char at the end.
        /// The expression 'Ut.AppPath + "FileName"' yields a valid fully qualified
        /// file name. Supports network paths.
        /// </summary>
        public static string AppPath
        {
            get
            {
                if (CachedAppPath == "")
                {
                    CachedAppPath = Application.ExecutablePath;
                    CachedAppPath = CachedAppPath.Remove(
                        CachedAppPath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
                }
                return CachedAppPath;
            }
        }

        /// <summary>
        /// This function returns a fully qualified name for the subpath, relative
        /// to the executable directory. This is for the purist programmers who can't
        /// handle AppPath returning something "invalid" :)
        /// </summary>
        public static string MakeAppSubpath(string subpath)
        {
            return Ut.AppPath + subpath;
        }

        /// <summary>
        /// Converts file size in bytes to a string in bytes, kbytes, Mbytes
        /// or Gbytes accordingly.
        /// </summary>
        /// <param name="size">Size in bytes</param>
        /// <returns>Converted string</returns>
        public static string SizeToString(long size)
        {
            if (size == 0)
            {
                return "0";
            }
            else if (size < 1024)
            {
                return size.ToString("#,###");
            }
            else if (size < 1024 * 1024)
            {
                return (size / 1024d).ToString("#,###.## k");
            }
            else if (size < 1024 * 1024 * 1024)
            {
                return (size / (1024d * 1024d)).ToString("#,###.## M");
            }
            else
            {
                return (size / (1024d * 1024d * 1024d)).ToString("#,###.## G");
            }
        }

        #region Internal error

        /// <summary>
        /// Informs the user nicely that an internal error has occurred. Does not
        /// actually terminate the program, so if the problem is not fatal the user
        /// could save their work etc.
        /// </summary>
        public static void InternalError()
        {
            InternalError(null);
        }

        /// <summary>
        /// Informs the user nicely that an internal error has occurred. Does not
        /// actually terminate the program, so if the problem is not fatal the user
        /// could save their work etc.
        /// </summary>
        /// <param name="ErrorCode">Some error code to help the developer pinpoint the problem.</param>
        public static void InternalError(int ErrorCode)
        {
            InternalErrorPrivate(ErrorCode.ToString());
        }

        /// <summary>
        /// Informs the user nicely that an internal error has occurred. Does not
        /// actually terminate the program, so if the problem is not fatal the user
        /// could save their work etc.
        /// </summary>
        /// <param name="ErrorCode">Some error message to help the developer pinpoint the problem.</param>
        public static void InternalError(string ErrorMsg)
        {
            InternalErrorPrivate(ErrorMsg);
        }

        /// <summary>
        /// Raises an internal error. The reason why this is private is so that only
        /// the other InternalError functions can call it. This ensures that the
        /// function can always backtrack exactly 2 stack frames to get to the place
        /// which invoked InternalError.
        /// </summary>
        private static void InternalErrorPrivate(string ErrorMsg)
        {
            string str = "Internal application error has occurred. Please inform the developer.\n";
            if (ErrorMsg != null) str += "\nError message: " + ErrorMsg;
            StackFrame sf = new StackFrame(2, true);
            str += "\nMethod: " + sf.GetMethod();
            if (sf.GetFileLineNumber() != 0)
                // The line number is zero if there is no program database in the
                // application directory (which is the case when a normal user runs it)
                str += "\nFile: " + sf.GetFileName() + "\nLine: " + sf.GetFileLineNumber();

            DlgMessage.ShowError(str, "Internal error", "OK");
        }

        #endregion
    }
}
