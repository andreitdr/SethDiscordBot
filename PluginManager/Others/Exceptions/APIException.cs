using System;

namespace PluginManager.Others.Exceptions
{
    [Serializable]
    public class APIException : Exception
    {
        public string? Function { get; } = "not specified";
        public Error? ErrorCode { get; } = Error.UNKNOWN_ERROR;
        public string? PossibleCause { get; } = "not specified";

        public APIException(string message, string? function, string possible_cause, Error error) : base(message)
        {
            ErrorCode = error;
            Function = function;
            PossibleCause = possible_cause;
        }

        public APIException(string message, string? function, Error? errorCode) : base(message)
        {
            ErrorCode = errorCode;
            Function = function;
        }

        public APIException(string message, string? function) : base(message)
        {
            Function = function;
        }

        public APIException(string message) : base(message)
        {

        }

        public void Print()
        {
            Console.WriteLine("Message Content: " + Message);
            Console.WriteLine("Function: " + Function);
            Console.WriteLine("Error Code: " + ErrorCode.ToString());
            Console.WriteLine("Possible cause: " + PossibleCause);
            if (this.StackTrace != null)
                Functions.WriteErrFile(this.StackTrace);
        }
    }

}