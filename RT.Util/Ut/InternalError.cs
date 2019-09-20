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
        /// <param name="errorCode">Some error code to help the developer pinpoint the problem.</param>
        public static void InternalError(int errorCode)
        {
            InternalErrorPrivate(errorCode.ToString());
        }

        /// <summary>
        /// Informs the user nicely that an internal error has occurred. Does not
        /// actually terminate the program, so if the problem is not fatal the user
        /// could save their work etc.
        /// </summary>
        /// <param name="errorMsg">An error message to help the developer pinpoint the problem.</param>
        public static void InternalError(string errorMsg)
        {
            InternalErrorPrivate(errorMsg);
        }

        /// <summary>
        /// Raises an internal error. The reason why this is private is so that only
        /// the other InternalError functions can call it. This ensures that the
        /// function can always backtrack exactly 2 stack frames to get to the place
        /// which invoked InternalError.
        /// </summary>
        private static void InternalErrorPrivate(string errorMsg)
        {
            string str = "Internal application error has occurred. Please inform the developer.\n";
            if (errorMsg != null) str += "\nError message: " + errorMsg;
            StackFrame sf = new StackFrame(2, true);
            str += "\nMethod: " + sf.GetMethod();
            if (sf.GetFileLineNumber() != 0)
                // The line number is zero if there is no program database in the
                // application directory (which is the case when a normal user runs it)
                str += "\nFile: " + sf.GetFileName() + "\nLine: " + sf.GetFileLineNumber();

            DlgMessage.Show(str, "Internal error", DlgType.Error, "OK");
        }

    }
}
