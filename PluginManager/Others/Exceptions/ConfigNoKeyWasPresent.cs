using System;
using System.Collections.Generic;
using PluginManager.Interfaces.Exceptions;

namespace PluginManager.Others.Exceptions;

public class ConfigNoKeyWasPresent: IException
{
    public List<string> Messages { get; set; }
    public bool isFatal { get; private set; }
    
    public ConfigNoKeyWasPresent(string message, bool isFatal)
    {
        this.Messages = new List<string>() { message };
        this.isFatal = isFatal;
    }
    
    public ConfigNoKeyWasPresent(string message)
    {
        this.Messages = new List<string>() { message };
        this.isFatal = false;
    }
    
    public string GenerateFullMessage()
    {
        string messages = "";
        foreach (var message in Messages)
        {
            messages += message + "\n";
        }
        return $"\nMessage: {messages}\nIsFatal: {isFatal}";
    }

    public void HandleException()
    {
        if (isFatal)
        {
            
            Config.Logger.Log(GenerateFullMessage(), LogLevel.CRITICAL, true);
            Environment.Exit((int)ExceptionExitCode.CONFIG_KEY_NOT_FOUND);
            
        }
        
        Config.Logger.Log(GenerateFullMessage(), LogLevel.WARNING);
    }
    
    public IException AppendError(string message)
    {
        Messages.Add(message);
        return this;
    }
    
    public IException AppendError(List<string> messages)
    {
        Messages.AddRange(messages);
        return this;
    }
    
    public IException IsFatal(bool isFatal = true)
    {
        this.isFatal = isFatal;
        return this;
    }
    
    public static ConfigNoKeyWasPresent CreateError(string message)
    {
        return new ConfigNoKeyWasPresent(message);
    }
    
    public static ConfigNoKeyWasPresent CreateError(string message,  bool isFatal)
    {
        return new ConfigNoKeyWasPresent(message, isFatal);
    }
    
}