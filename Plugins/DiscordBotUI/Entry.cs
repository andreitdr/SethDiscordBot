using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

using CppWrapper.Objects;
using CppWrapper.LibraryManagement;
using DiscordBotCore;
using CppWrapper;

namespace DiscordBotUI;

public class Entry : ICommandAction
{
    public string ActionName => "cppui";

    public string? Description => "A C++ linker to the C++ UI for the bot";

    public string? Usage => "cppui";

    public IEnumerable<InternalActionOption> ListOfOptions => [];

    public InternalActionRunType RunType => InternalActionRunType.OnStartupAndCall;
    
    public bool RequireOtherThread => false;

    public async Task Execute(string[]? args)
    {
        try{
        
            string appUiComponent = "./Data/Test/libtestlib.dll";

            ExternLibrary externalLibrary = new ExternLibrary(appUiComponent);
            externalLibrary.InitializeLibrary();

            externalLibrary.SetExternFunctionSetterPointerToCustomDelegate<Delegates.SetCsharpFunctionPointerDelegate, Delegates.CsharpFunctionDelegate>("setCSharpFunctionPointer", () =>
            {
                Console.WriteLine("Hello from C#. This code is called from the C# function");
            });

            Delegates.ProcessComplexObject processObj = externalLibrary.GetDelegateForFunctionPointer<Delegates.ProcessComplexObject>("ProcessComplexObject");

            ComplexObject complexObject = new ComplexObject(10, 10.5, "Hello from C#");
            processObj(ref complexObject);

            Console.WriteLine($"Integer: {complexObject.Integer}");
            Console.WriteLine($"Double: {complexObject.DoubleValue}");
            Console.WriteLine($"String: {complexObject.strValue}");

            externalLibrary.FreeLibrary();
        } catch (Exception dllException) {
            Application.CurrentApplication.Logger.LogException(dllException, this);
        }
    }
}
