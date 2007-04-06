/// InternalError.cs  -  classes to allow programs to easily report internal errors

using System;
using System.Diagnostics;
using RT.Util.Dialogs;

namespace RT.Util
{
    public static partial class Ut
    {
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

    }
}
