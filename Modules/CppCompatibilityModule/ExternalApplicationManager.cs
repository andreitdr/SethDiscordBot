using CppCompatibilityModule.Extern;
using DiscordBotCore;
using DiscordBotCore.Logging;

namespace CppCompatibilityModule;

internal class ExternalApplicationManager
{
    private readonly ILogger _Logger;
    private List<ExternalApplication> _ExternalApplications;
    
    public ExternalApplicationManager(ILogger logger)
    {
        _Logger = logger;
        _ExternalApplications = new List<ExternalApplication>();
    }
    
    public bool TryCreateApplication(string applicationFileName, out Guid applicationId)
    {
        ExternalApplication? externalApplication = ExternalApplication.CreateFromDllFile(_Logger, applicationFileName);
        
        if(externalApplication is null)
        {
            applicationId = Guid.Empty;
            return false;
        }
        
        _ExternalApplications.Add(externalApplication);
        
        applicationId = externalApplication.ApplicationId;
        return true;
    }
    
    public void FreeApplication(Guid applicationId)
    {
        var application = _ExternalApplications.FirstOrDefault(app => app.ApplicationId == applicationId, null);
        if(application is null)
        {
            _Logger.Log($"Couldn't find application with id {applicationId}");
            return;
        }
        
        application.FreeLibrary();
        _ExternalApplications.Remove(application);
        
        _Logger.Log($"Application with id {applicationId} freed successfully");
    }
    
    public T GetFunctionDelegate<T>(Guid applicationId, string functionName) where T : Delegate
    {
        var application = _ExternalApplications.FirstOrDefault(app => app.ApplicationId == applicationId, null);
        if(application is null)
        {
            _Logger.Log($"Couldn't find application with id {applicationId}");
            return default!;
        }
        
        return application.GetDelegateForFunctionPointer<T>(functionName);
    }
    
    public object? SetExternFunctionToPointToFunction<TLocalFunctionDelegate>(Guid applicationId, string externalFunctionName, TLocalFunctionDelegate localFunction) where TLocalFunctionDelegate : Delegate
    {
        var application = _ExternalApplications.FirstOrDefault(app => app.ApplicationId == applicationId, null);
        if(application is null)
        {
            _Logger.Log($"Couldn't find application with id {applicationId}");
            return null;
        }
        
        return application.SetExternFunctionToPointToFunction<TLocalFunctionDelegate>(externalFunctionName, localFunction);
    }
    
}
