using DiscordBotCore;
using DiscordBotCore.Interfaces.Modules;
using DiscordBotCore.Others;

namespace CppCompatibilityModule;

public class Entry : IModule
{
    public ModuleType ModuleType => ModuleType.Compatibility;
    public string Name => "CppCompatibility";
    public IDictionary<string, string> MethodMapping => new Dictionary<string, string>()
    {
        {"create_application", "CreateApplication"},
        {"stop_application", "StopApplication"},
        {"execute_function_with_parameter", "CallFunctionWithParameter"},
        {"execute_function_without_parameter", "CallFunctionWithoutParameter"}
    };

    private ExternalApplicationManager? _ExternalApplicationManager;

    public Task Initialize()
    {
        _ExternalApplicationManager = new ExternalApplicationManager();
        return Task.CompletedTask;
    }

    public Guid CreateApplication(string dllFilePath)
    {

        if (_ExternalApplicationManager is null)
        {
            Application.Logger.Log("Failed to create application because the manager is not initialized. This should have never happened in the first place !!!", this, LogType.Critical);
            return Guid.Empty;
        }

        if (_ExternalApplicationManager.TryCreateApplication(dllFilePath, out Guid appId))
        {
            return appId;
        }

        return Guid.Empty;
    }

    public void StopApplication(Guid applicationId)
    {
        if (_ExternalApplicationManager is null)
        {
            Application.Logger.Log("Failed to stop application because the manager is not initialized. This should have never happened in the first place!!!", this, LogType.Critical);
            return;
        }

        _ExternalApplicationManager.FreeApplication(applicationId);
    }

    public void CallFunctionWithParameter(Guid appId, string functionName, ref object parameter)
    {
        if (_ExternalApplicationManager is null)
        {
            Application.Logger.Log("Failed to call function because the manager is not initialized. This should have never happened in the first place!!!", this, LogType.Critical);
            return;
        }

        _ExternalApplicationManager.ExecuteApplicationFunctionWithParameter(appId, functionName, ref parameter);
    }

    public void CallFunctionWithoutParameter(Guid appId, string functionName)
    {
        if (_ExternalApplicationManager is null)
        {
            Application.Logger.Log("Failed to call function because the manager is not initialized. This should have never happened in the first place!!!", this, LogType.Critical);
            return;
        }

        _ExternalApplicationManager.ExecuteApplicationFunctionWithoutParameter(appId, functionName);
    }
}