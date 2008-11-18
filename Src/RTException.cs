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
        protected string _message = "An RT exception has occurred.";

        /// <summary>
        /// It's probably a good idea to never use this constructor, since it
        /// leaves the message uninitialised.
        /// </summary>
        public RTException()
        {
        }

        /// <summary>
        /// A non-formatting constructor: simply uses the specified message.
        /// </summary>
        public RTException(string message)
        {
            _message = message;
        }

        /// <summary>
        /// A non-formatting constructor: simply uses the specified message.
        /// </summary>
        public RTException(string message, Exception innerException)
            : base(null, innerException)
        {
            _message = message;
        }

        /// <summary>
        /// A formatting constructor: string.Format's the arguments
        /// into the supplied string.
        /// </summary>
        public RTException(string message, params object[] args)
        {
            _message = string.Format(message, args);
        }

        /// <summary>
        /// A formatting constructor: string.Format's the arguments
        /// into the supplied string.
        /// </summary>
        public RTException(string message, Exception innerException, params object[] args)
            : base(null, innerException)
        {
            _message = string.Format(message, args);
        }

    }
}
