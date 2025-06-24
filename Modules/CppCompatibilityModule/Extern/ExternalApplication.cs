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
    
    internal T GetDelegateForFunctionPointer<T>(string methodName) where T : Delegate
    {
        return _ExternLibrary.GetDelegateForFunctionPointer<T>(methodName);
    }
    
    internal object? SetExternFunctionToPointToFunction<TLocalFunctionDelegate>(string externalFunctionName, TLocalFunctionDelegate localFunction) where TLocalFunctionDelegate : Delegate
    {
        return _ExternLibrary.SetExternFunctionSetterPointerToCustomDelegate<Delegates.SetExternFunctionPointerDelegate, TLocalFunctionDelegate>(externalFunctionName, localFunction);
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
