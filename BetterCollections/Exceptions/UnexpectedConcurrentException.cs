using System;

namespace BetterCollections.Exceptions;

public class UnexpectedConcurrentException : Exception
{
    public UnexpectedConcurrentException() : this(
        "A concurrent call occurred on a container that does not allow concurrency, and the internal state may have been corrupted") { }

    public UnexpectedConcurrentException(string message) : base(message) { }
    public UnexpectedConcurrentException(string message, Exception inner) : base(message, inner) { }
}
