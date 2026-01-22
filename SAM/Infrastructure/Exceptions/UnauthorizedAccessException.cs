namespace SAM.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to access a resource they don't have permission for.
/// </summary>
public class UnauthorizedResourceAccessException : Exception
{
    public UnauthorizedResourceAccessException() : base()
    {
    }

    public UnauthorizedResourceAccessException(string message) : base(message)
    {
    }

    public UnauthorizedResourceAccessException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

