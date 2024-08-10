using DiscordBotCore.Interfaces.Modules;
using DiscordBotCore.Others;

namespace LoggerModule
{
    public class Entry : IModule
    {
        public string Name => "LoggerModule";
        public ModuleType ModuleType => ModuleType.Logger;

        public IDictionary<string, string> MethodMapping => new Dictionary<string, string>
        {
            {"BaseLog", "LogMessage"},
            {"LogWithTypeAndFormat", "LogMessageWithTypeAndFormat"},
            {"LogWithType", "LogMessageWithType"},
            {"LogWithSender", "LogMessageWithSender"},
            {"LogWithTypeAndSender", "LogMessageWithTypeAndSender"},
            {"BaseLogException", "LogExceptionWithSenderAndFullStack"},
            {"SetPrintFunction", "SetOutFunction"},
        };

        const string _LogFolder = "./Data/Logs/";
        const string _LogFormat = "{ThrowTime} {SenderName} {Message}";
        
        public ILogger Module { get; private set; } = null!;

        public Task Initialize()
        {
            ILogger logger = new Logger(_LogFolder, _LogFormat);
            Module = logger;
            return Task.CompletedTask;
        }
        
        public void SetOutFunction(Action<string> outFunction)
        {
            Module.SetOutFunction(outFunction);
        }



        public void LogMessage(string message) => Module.Log(message);
        public void LogMessageWithTypeAndFormat(string message, LogType logType, string format) => Module.Log(message, logType, format);
        public void LogMessageWithType(string message, LogType logType) => Module.Log(message, logType);
        public void LogMessageWithSender(string message, object Sender) => Module.Log(message, Sender);
        public void LogMessageWithTypeAndSender(string message, object Sender, LogType type) => Module.Log(message, Sender, type);
        public void LogExceptionWithSenderAndFullStack(Exception exception, object Sender, bool logFullStack = false) => Module.LogException(exception, Sender, logFullStack);
        
    }
}
