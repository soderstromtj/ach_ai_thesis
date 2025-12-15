namespace NIU.ACHAI.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a group chat manager encounters an error during orchestration.
    /// </summary>
    public class ChatManagerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChatManagerException"/> class.
        /// </summary>
        public ChatManagerException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatManagerException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ChatManagerException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatManagerException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ChatManagerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
