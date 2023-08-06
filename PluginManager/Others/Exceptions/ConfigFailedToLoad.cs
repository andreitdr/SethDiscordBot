using System;
using System.Collections.Generic;
using PluginManager.Interfaces.Exceptions;

namespace PluginManager.Others.Exceptions;

public class ConfigFailedToLoad : IException
{
    public List<string>? Messages { get; set; }
    public bool isFatal { get; private set; }
    public string? File { get; }
    
    
    public ConfigFailedToLoad(string message, bool isFatal, string file)
    {
        this.isFatal = isFatal;
        Messages = new List<string>() {message};
        this.File = file;
    }
    
    public ConfigFailedToLoad(string message, bool isFatal)
    {
        this.isFatal = isFatal;
        Messages = new List<string>() {message};
        this.File = null;
    }
    
    public ConfigFailedToLoad(string message)
    {
        this.isFatal = false;
        Messages = new List<string>() {message};
        this.File = null;
    }
    
    public string GenerateFullMessage()
    {
        string messages = "";
        foreach (var message in Messages)
        {
            messages += message + "\n";
        }
        return $"\nMessage: {messages}\nIsFatal: {isFatal}\nFile: {File ?? "null"}";
    }
    
    public void HandleException()
    {
        if (isFatal)
        {
            Config.Logger.Log(GenerateFullMessage(), LogLevel.CRITICAL, true);
            Environment.Exit((int)ExceptionExitCode.CONFIG_FAILED_TO_LOAD);
            
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
    
    
    public static ConfigFailedToLoad CreateError(string message, bool isFatal, string? file = null)
    {
        if (file is not null)
            return new ConfigFailedToLoad(message, isFatal, file);
        return new ConfigFailedToLoad(message, isFatal);
    }
    
    public static ConfigFailedToLoad CreateError(string message)
    {
        return new ConfigFailedToLoad(message);
    }
    
    
    
    
}