using Discord;

namespace PluginManager
{
    public static class Logger
    {

        public static bool isConsole = true;


        public delegate void LogEventHandler(string Message);
        public static event LogEventHandler LogEvent;

        public static void Log(string Message)
        {
            LogEvent?.Invoke(Message);
        }

        public static void Log(string Message, params object[] Args)
        {
            LogEvent?.Invoke(string.Format(Message, Args));
        }

        public static void Log(IMessage message, bool newLine)
        {
            LogEvent?.Invoke(message.Content);
            if (newLine)
                LogEvent?.Invoke("\n");
        }

        public static void WriteLine(string? message)
        {
            if (message is not null)
                LogEvent?.Invoke(message + '\n');
        }

        public static void LogError(System.Exception ex)
        {
            string message = "[ERROR]" + ex.Message;
            LogEvent?.Invoke(message + '\n');
        }

        public static void LogError(string? message)
        {
            if (message is not null)
                LogEvent?.Invoke("[ERROR]" + message + '\n');
        }


        public static void WriteLine()
        {
            LogEvent?.Invoke("\n");
        }

        public static void Write(string message)
        {
            LogEvent?.Invoke(message);
        }

        public static void Write(char c)
        {
            LogEvent?.Invoke($"{c}");
        }
    }
}
