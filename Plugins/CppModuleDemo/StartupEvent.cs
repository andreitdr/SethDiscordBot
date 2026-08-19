using CppCompatibilityModule;
using CppCompatibilityModule.Extern;
using DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;
using DiscordBotCore.PluginCore.Interfaces;
using System.Runtime.InteropServices;

namespace CppModuleDemo;

public class StartupEvent : IDbEvent
{
    public string Name => "CppModuleDemoStartupEvent";
    public string Description => "A demo event to showcase the C++ module integration with Discord Bot Core on startup.";
    
    private static string GetNativeModuleFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "CppModuleDemo.dll";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "libCppModuleDemo.so";
        return "libCppModuleDemo.dylib";
    }
    
    public void Start(IDbEventExecutingArgument args)
    {
        args.PluginBaseDirectory.Create();
        InternalSettings.ExternalApplicationHandler = new ExternalApplicationHandler(args.Logger);
        string fullPath = Path.Combine(args.PluginBaseDirectory.FullName, GetNativeModuleFileName());
        Guid id = InternalSettings.ExternalApplicationHandler.CreateApplication(fullPath);

        if (id == Guid.Empty)
        {
            args.Logger.Log("Failed to create the C++ module application. Please check the DLL path and ensure it is correct.", this);
            return;
        }
        
        args.Logger.Log($"CppModuleDemo started successfully with application ID: {id}", this);

        InternalSettings.ManagedCallbackReference = OnManagedCallbackInvoked;
        try
        {
            InternalSettings.ExternalApplicationHandler.SetExternFunctionToPointToFunction(
                id,
                "setManagedCallback",
                InternalSettings.ManagedCallbackReference);
        }
        catch (EntryPointNotFoundException ex)
        {
            args.Logger.Log($"Failed to bind managed callback to native library: {ex.Message}", this);
            InternalSettings.ExternalApplicationHandler.StopApplication(id);
            InternalSettings.DemoModuleInternalId = Guid.Empty;
            InternalSettings.ManagedCallbackReference = null;
            return;
        }

        InternalSettings.DemoModuleInternalId = id;
    }

    private static void OnManagedCallbackInvoked()
    {
        InternalSettings.ManagedCallbackInvocationCount++;
    }
}