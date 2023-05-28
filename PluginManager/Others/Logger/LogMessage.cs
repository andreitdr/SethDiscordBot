﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Others.Logger
{
    public class LogMessage
    {
        public string Message { get; set; }
        public LogLevel Type { get; set; }
        public string Time { get; set; }
        public string Sender { get; set; }
        public LogMessage(string message, LogLevel type)
        {
            Message = message;
            Type = type;
            Time = DateTime.Now.ToString("HH:mm:ss");
        }

        public LogMessage(string message, LogLevel type, string sender) : this(message, type)
        {
            Sender = sender;
        }

        public override string ToString()
        {
            return $"[{Time}] {Message}";
        }

        public static explicit operator LogMessage(string message)
        {
            return new LogMessage(message, LogLevel.INFO);
        }

        public static explicit operator LogMessage((string message, LogLevel type) tuple)
        {
            return new LogMessage(tuple.message, tuple.type);
        }

        public static explicit operator LogMessage((string message, LogLevel type, string sender) tuple)
        {
            return new LogMessage(tuple.message, tuple.type, tuple.sender);
        }
    }
}