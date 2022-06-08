using System;

namespace PluginManager.Others.Exceptions;

/// <summary>
///     Custom Exception for PluginManager
/// </summary>
[Serializable]
public class APIException : Exception
{
    /// <summary>
    ///     The APIException contructor
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="function">The function where the message was triggered</param>
    /// <param name="possible_cause">The possible cause of the error</param>
    /// <param name="error">The error code</param>
    public APIException(string message, string? function, string possible_cause, Error error) : base(message)
    {
        ErrorCode     = error;
        Function      = function;
        PossibleCause = possible_cause;
    }

    /// <summary>
    ///     The APIException contructor
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="function">The function where the message was triggered</param>
    /// <param name="errorCode">The error code</param>
    public APIException(string message, string? function, Error? errorCode) : base(message)
    {
        ErrorCode = errorCode;
        Function  = function;
    }

    /// <summary>
    ///     The APIException contructor
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="function">The function where the message was triggered</param>
    public APIException(string message, string? function) : base(message)
    {
        Function = function;
    }

    /// <summary>
    ///     The APIException contructor
    /// </summary>
    /// <param name="message">The error message</param>
    public APIException(string message) : base(message)
    {
    }

    /// <summary>
    ///     The APIException constructor
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="errorLocation">The class where the error was thrown</param>
    public APIException(string message, Type errorLocation) : base(message)
    {
        Function = errorLocation.FullName;
    }

    /// <summary>
    ///     The function where the error occurred
    /// </summary>
    public string? Function { get; } = "not specified";

    /// <summary>
    ///     The error code
    /// </summary>
    public Error? ErrorCode { get; } = Error.UNKNOWN_ERROR;

    /// <summary>
    ///     The possible cause that determined the error
    /// </summary>
    public string? PossibleCause { get; } = "not specified";

    /// <summary>
    ///     Method to print the error to <see cref="Console" />
    /// </summary>
    public void Print()
    {
        Console.WriteLine("Message Content: " + Message);
        Console.WriteLine("Function: " + Function);
        Console.WriteLine("Error Code: " + ErrorCode);
        Console.WriteLine("Possible cause: " + PossibleCause);
        if (StackTrace != null) Functions.WriteErrFile(StackTrace);
    }
}
