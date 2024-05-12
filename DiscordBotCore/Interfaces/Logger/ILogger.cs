using DiscordBotCore.Others;

using System;
using System.Collections.Generic;

namespace DiscordBotCore.Interfaces.Logger
{
    public interface ILogger
    {
        public struct FormattedMessage { public string Message; public LogType Type; }
        public string LogMessageFormat { get; set; }

        public void Log(ILogMessage message);
        public void LogException(Exception exception, object Sender);

        public event EventHandler<FormattedMessage> OnFormattedLog;
        public event EventHandler<ILogMessage> OnRawLog;
    }
}
