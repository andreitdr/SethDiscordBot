using System.Collections.Generic;

namespace PluginManager.Interfaces.Exceptions;

public interface IException
{
    public List<string> Messages { get; set; }
    public bool isFatal { get; }
    public string GenerateFullMessage();
    public void HandleException();
    
    public IException AppendError(string message);
    
    public IException AppendError(List<string> messages);
    public IException IsFatal(bool isFatal = true);
    
}