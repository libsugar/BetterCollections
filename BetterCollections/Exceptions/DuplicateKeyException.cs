using System;

namespace BetterCollections.Exceptions;

public class DuplicateKeyException : ArgumentException
{
    public DuplicateKeyException() { }
    public DuplicateKeyException(string message) : base(message) { }
    public DuplicateKeyException(string message, Exception inner) : base(message, inner) { }
}
