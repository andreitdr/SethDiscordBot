using System;

namespace PluginManager.Others.Exceptions
{
    [System.Serializable]
    public class APIException : Exception
    {
        public string? Function { get; }
        public Error? ErrorCode { get; }

        public APIException(string message, string? function, Error? errorCode) : base(message)
        {
            ErrorCode = errorCode;
            Function = function;
        }

        public APIException(string message, string? function) : base(message)
        {
            ErrorCode = Error.UNKNOWN_ERROR;
            Function = function;
        }

        public APIException(string message) : base(message)
        {
            ErrorCode = Error.UNKNOWN_ERROR;
            Function = "Unspecified_Function";
        }

        public void Print()
        {
            Console.WriteLine("Message Content: " + Message);
            Console.WriteLine("Function: " + Function);
            Console.WriteLine("Error Code: " + ErrorCode.ToString());
        }
    }

}