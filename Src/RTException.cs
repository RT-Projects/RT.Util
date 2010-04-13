using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util
{
    /// <summary>
    /// Base class for all custom exceptions we define. You don't have to use it,
    /// but you'll probably want to, as it provides a few crutches for the standard
    /// Exception class.
    /// </summary>
    /// 
    /// <example>
    /// <para>Copy and paste the following constructors to your class for "default" behaviour:</para>
    /// 
    /// <code>
    /// /// &lt;summary&gt;Creates an exception instance with the specified message.&lt;/summary&gt;
    /// public MyException(string message) : base(message) { }
    ///
    /// /// &lt;summary&gt;Creates an exception instance with the specified message and inner exception.&lt;/summary&gt;
    /// public MyException(string message, Exception innerException) : base(null, innerException) { }
    /// </code>
    /// </example>
    public class RTException : Exception
    {
        /// <summary>
        /// The base Exception class does not give access to the private
        /// "message" field to the derived classes. What were they thinking...
        /// Use this to initialize more complex messages in the constructor.
        /// </summary>
        public override string Message
        {
            get
            {
                return _message;
            }
        }

        /// <summary>
        /// Set this field to change the message stored in this exception.
        /// </summary>
        protected string _message = "An RT exception has occurred";

        /// <summary>
        /// Should only be used by constructors which initialise <see cref="_message"/> in
        /// the constructor body.
        /// </summary>
        protected RTException()
        {
        }

        /// <summary>
        /// Creates an exception instance with the specified initial message.
        /// </summary>
        public RTException(string message)
        {
            _message = message;
        }

        /// <summary>
        /// Creates an exception instance with the specified initial message and
        /// inner exception.
        /// </summary>
        public RTException(string message, Exception innerException)
            : base(null, innerException)
        {
            _message = message;
        }

    }

    /// <summary>
    /// Represents an internal error in the code. Any place where the code is able
    /// to verify its own consistency is where this exception should be thrown, for
    /// example in "unreachable" code safeguards.
    /// </summary>
    public sealed class InternalError : RTException
    {
        /// <summary>Creates an exception instance with the specified message.</summary>
        public InternalError(string message)
            : base(message)
        { }

        /// <summary>Creates an exception instance with the specified message and inner exception.</summary>
        public InternalError(string message, Exception innerException)
            : base(null, innerException)
        { }
    }
}
