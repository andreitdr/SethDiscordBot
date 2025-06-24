using CppCompatibilityModule;
using CppCompatibilityModule.Extern;
using DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;
using DiscordBotCore.PluginCore.Interfaces;

namespace CppModuleDemo;

public class StartupEvent : IDbEvent
{
    public string Name => "CppModuleDemoStartupEvent";
    public string Description => "A demo event to showcase the C++ module integration with Discord Bot Core on startup.";
    
    private static string _DllModule = "libCppModuleDemo.dylib";
    
    public void Start(IDbEventExecutingArgument args)
    {
        args.PluginBaseDirectory.Create();
        InternalSettings.ExternalApplicationHandler = new ExternalApplicationHandler(args.Logger);
        string fullPath = Path.Combine(args.PluginBaseDirectory.FullName, _DllModule);
        Guid id = InternalSettings.ExternalApplicationHandler.CreateApplication(fullPath);

        if (id == Guid.Empty)
        {
            args.Logger.Log("Failed to create the C++ module application. Please check the DLL path and ensure it is correct.", this);
            return;
        }
        
        args.Logger.Log($"CppModuleDemo started successfully with application ID: {id}", this);
        InternalSettings.DemoModuleInternalId = id;
    }
}