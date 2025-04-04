using DiscordBotCore;
using DiscordBotCore.Logging;
using DiscordBotCore.Others;

namespace CppCompatibilityModule.Extern;

public class ExternalApplication
{
    public Guid ApplicationId { get; private set; }
    private readonly ExternLibrary _ExternLibrary;
    private readonly ILogger _Logger;
    
    private ExternalApplication(ILogger logger, Guid applicationGuid, ExternLibrary library)
    {
        this.ApplicationId = applicationGuid;
        this._ExternLibrary = library;
        this._Logger = logger;
    }
    
    internal void CallFunction(string methodName, ref object parameter)
    {
        _ExternLibrary.CallFunction(methodName, ref parameter);
    }
    
    internal void CallFunction(string methodName)
    {
        _ExternLibrary.CallFunction(methodName);
    }
    
    internal T GetDelegateForFunctionPointer<T>(string methodName) where T : Delegate
    {
        return _ExternLibrary.GetDelegateForFunctionPointer<T>(methodName);
    }
    
    internal void SetExternFunctionToPointToFunction(string externalFunctionName, Delegates.CsharpFunctionDelegate localFunction)
    {
        _ExternLibrary.SetExternFunctionSetterPointerToCustomDelegate<Delegates.SetExternFunctionPointerDelegate, Delegates.CsharpFunctionDelegate>(externalFunctionName, localFunction);
    }
    
    internal void FreeLibrary()
    {
        _ExternLibrary.FreeLibrary();
    }
    
    public static ExternalApplication? CreateFromDllFile(ILogger logger, string dllFilePath)
    {
        ExternLibrary library = new ExternLibrary(logger, dllFilePath);
        var result = library.InitializeLibrary();

        return result.Match<ExternalApplication?>(
            () => new ExternalApplication(logger, Guid.NewGuid(), library),
            (ex) => {
            logger.Log(ex.Message, LogType.Error);
            library.FreeLibrary();
            return null;
        });
    }
}
