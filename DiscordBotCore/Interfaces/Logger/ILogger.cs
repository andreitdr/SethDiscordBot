using DiscordBotCore.Interfaces.Modules;
using DiscordBotCore.Others;

using System;

namespace DiscordBotCore.Interfaces.Logger
{
    public interface ILogger : IBaseModule
    {
        public struct FormattedMessage { 
            public string Message;
            public LogType Type;
        }

        string LogMessageFormat { get; set; }

        event EventHandler<FormattedMessage> OnFormattedLog;
        event EventHandler<ILogMessage> OnRawLog;

        void Log(ILogMessage message);
        void Log(ILogMessage message, string format);
        void Log(string message);
        void Log(string message, LogType logType);
        void Log(string message, LogType logType, string format);
        void Log(string message, object Sender);
        void Log(string message, object Sender, LogType type);
        void LogException(Exception exception, object Sender, bool logFullStack = false);
    }
}
