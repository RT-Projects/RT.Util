/// Delegates.cs  -  utility functions and classes

using System;

namespace RT.Util
{
    public delegate void VoidFuncVoid();
    public delegate void VoidFunc<TParam1>(TParam1 param1);
    public delegate void VoidFunc<TParam1, TParam2>(TParam1 param1, TParam2 param2);

    public delegate TRet RetFuncVoid<TRet>();
    public delegate TRet RetFunc<TRet, TParam1>(TParam1 param1);
    public delegate TRet RetFunc<TRet, TParam1, TParam2>(TParam1 param1, TParam2 param2);

    /// <summary>
    /// EventArgs for the ConfirmEventHandler delegate.
    /// </summary>
    public class ConfirmEventArgs : EventArgs
    {
        /// <summary>
        /// Set this to true/false to confirm or cancel the action.
        /// </summary>
        public bool ConfirmOK;
    }

    /// <summary>
    /// A general-purpose event type which lets the caller confirm an action.
    /// </summary>
    public delegate void ConfirmEventHandler(object sender, ConfirmEventArgs e);
}
