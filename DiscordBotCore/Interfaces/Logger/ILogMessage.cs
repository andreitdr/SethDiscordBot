﻿using DiscordBotCore.Others;

using System;

namespace DiscordBotCore.Interfaces.Logger
{
    public interface ILogMessage
    {
        public string Message { get; protected set; }
        public DateTime ThrowTime { get; protected set; }
        public string SenderName { get; protected set; }
        public LogType LogMessageType { get; protected set; }

    }
}
