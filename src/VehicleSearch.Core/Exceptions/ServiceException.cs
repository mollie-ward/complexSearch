namespace VehicleSearch.Core.Exceptions;

/// <summary>
/// Exception thrown when a service operation fails.
/// </summary>
public class ServiceException : Exception
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    public ServiceException(string message, string errorCode = "SERVICE_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="errorCode">The error code.</param>
    public ServiceException(string message, Exception innerException, string errorCode = "SERVICE_ERROR")
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
