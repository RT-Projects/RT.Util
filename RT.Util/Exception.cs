using System;

namespace RT.Util
{
    /// <summary>
    ///     Represents an internal error in the code. Any place where the code is able to verify its own consistency is where
    ///     this exception should be thrown, for example in "unreachable" code safeguards.</summary>
    public sealed class InternalErrorException : Exception
    {
        /// <summary>Creates an exception instance with the specified message.</summary>
        public InternalErrorException(string message)
            : base(message)
        { }

        /// <summary>Creates an exception instance with the specified message and inner exception.</summary>
        public InternalErrorException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
