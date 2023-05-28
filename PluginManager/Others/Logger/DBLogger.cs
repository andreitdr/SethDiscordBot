using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Others.Logger
{
    public class DBLogger
    {
        
        private List<LogMessage> LogHistory = new List<LogMessage>();
        private List<LogMessage> ErrorHistory = new List<LogMessage>();

        public IReadOnlyList<LogMessage> Logs => LogHistory;
        public IReadOnlyList<LogMessage> Errors => ErrorHistory;

        public delegate void LogHandler(string message, LogLevel logType);
        public event LogHandler LogEvent;

        private string _logFolder;
        private string _errFolder;

        public DBLogger()
        {
            _logFolder = Config.Data["LogFolder"];
            _errFolder = Config.Data["ErrorFolder"];
        }

        public void Log(string message, string sender = "unknown", LogLevel type = LogLevel.INFO) => Log(new LogMessage(message, type, sender));

        public void Log(LogMessage message)
        {
            if(LogEvent is not null)
                LogEvent?.Invoke(message.Message, message.Type);

            if (message.Type != LogLevel.NONE)
                LogHistory.Add(message);
            else
                ErrorHistory.Add(message);
        }

        public void Log(string message, object sender, LogLevel type = LogLevel.NONE) => Log(message, sender.GetType().Name, type);

        public async void SaveToFile()
        {
            await Functions.SaveToJsonFile(_logFolder + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".json", LogHistory);
            await Functions.SaveToJsonFile(_errFolder + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".json", ErrorHistory);
        }
    }
}
