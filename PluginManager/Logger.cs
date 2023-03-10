using System;
using System.IO;
using System.Numerics;
using Discord;

namespace PluginManager
{
    public static class Logger
    {

        public static bool isConsole { get; private set; }
        private static bool isInitialized;

        private static string? logFolder;
        private static string? errFolder;

        public static void Initialize(bool console)
        {

            if (isInitialized) throw new Exception("Logger is already initialized");

            if (!Config.Variables.Exists("LogFolder"))
                Config.Variables.Add("LogFolder", "./Data/Output/Logs/");

            if (!Config.Variables.Exists("ErrorFolder"))
                Config.Variables.Add("ErrorFolder", "./Data/Output/Errors/");

            isInitialized = true;
            logFolder = Config.Variables.GetValue("LogFolder");
            errFolder = Config.Variables.GetValue("ErrorFolder");
            isConsole = console;
        }


        public delegate void LogEventHandler(string Message);
        public static event LogEventHandler LogEvent;

        public static void Log(string Message)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            LogEvent?.Invoke(Message);
        }

        public static void Log(string Message, params object[] Args)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            LogEvent?.Invoke(string.Format(Message, Args));
        }

        public static void Log(IMessage message, bool newLine)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            LogEvent?.Invoke(message.Content);
            if (newLine)
                LogEvent?.Invoke("\n");
        }

        public static void WriteLine(string? message)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            if (message is not null)
                LogEvent?.Invoke(message + '\n');
        }

        public static void LogError(System.Exception ex)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            string message = "[ERROR]" + ex.Message;
            LogEvent?.Invoke(message + '\n');
        }

        public static void LogError(string? message)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            if (message is not null)
                LogEvent?.Invoke("[ERROR]" + message + '\n');
        }


        public static void WriteLine()
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            LogEvent?.Invoke("\n");
        }

        public static void Write(string message)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            LogEvent?.Invoke(message);
        }


        public static void Write<T>(T c)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            LogEvent?.Invoke($"{c}");
        }

        public static void Write<T>(T c, params object[] Args)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            LogEvent?.Invoke(string.Format($"{c}", Args));
        }

        public static void WriteColored(string message, ConsoleColor color)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            if(!isConsole) {
                LogEvent?.Invoke(message);
                return;
            }
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            LogEvent?.Invoke(message);
            Console.ForegroundColor = oldColor;
        }

        /// <summary>
        ///     Write logs to file
        /// </summary>
        /// <param name="LogMessage">The message to be wrote</param>
        public static void WriteLogFile(string LogMessage)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            var logsPath = logFolder + $"{DateTime.Today.ToShortDateString().Replace("/", "-").Replace("\\", "-")} Log.txt";
            Directory.CreateDirectory(logFolder);
            File.AppendAllText(logsPath, $"[{DateTime.Today.ToShortTimeString()}] {LogMessage} \n");
        }

        /// <summary>
        ///     Write error to file
        /// </summary>
        /// <param name="ErrMessage">The message to be wrote</param>
        public static void WriteErrFile(string ErrMessage)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            var errPath = errFolder +
                          $"{DateTime.Today.ToShortDateString().Replace("/", "-").Replace("\\", "-")} Error.txt";
            Directory.CreateDirectory(errFolder);
            File.AppendAllText(errPath, $"[{DateTime.Today.ToShortTimeString()}] {ErrMessage} \n");
        }

        public static void WriteErrFile(this Exception ex)
        {
            if (!isInitialized) throw new Exception("Logger is not initialized");
            WriteErrFile(ex.ToString());
        }

    }
}
