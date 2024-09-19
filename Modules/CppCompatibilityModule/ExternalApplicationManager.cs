using CppCompatibilityModule.Extern;
using DiscordBotCore;

namespace CppCompatibilityModule;

public class ExternalApplicationManager
{
    private List<ExternalApplication> _ExternalApplications;
    
    public ExternalApplicationManager()
    {
        _ExternalApplications = new List<ExternalApplication>();
    }
    
    public bool TryCreateApplication(string applicationFileName, out Guid applicationId)
    {
        ExternalApplication? externalApplication = ExternalApplication.CreateFromDllFile(applicationFileName);
        
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
            Application.Logger.Log($"Couldn't find application with id {applicationId}");
            return;
        }
        
        application.FreeLibrary();
        _ExternalApplications.Remove(application);
        
        Application.Logger.Log($"Application with id {applicationId} freed successfully");
    }
    
    public void ExecuteApplicationFunctionWithParameter(Guid appId, string functionName, ref object parameter)
    {
        var application = _ExternalApplications.FirstOrDefault(app => app.ApplicationId == appId);
        if(application is null)
        {
            Application.Logger.Log($"Couldn't find application with id {appId}");
            return;
        }
        
        application.CallFunction(functionName, ref parameter);
    }
    
    public void ExecuteApplicationFunctionWithoutParameter(Guid appId, string functionName)
    {
        var application = _ExternalApplications.FirstOrDefault(app => app.ApplicationId == appId);
        if(application is null)
        {
            Application.Logger.Log($"Couldn't find application with id {appId}");
            return;
        }
        
        application.CallFunction(functionName);
        
    }
    
    
}
