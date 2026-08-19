using System.Runtime.InteropServices;

namespace CppModuleDemo;

public static class Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ModifyComplexObject(ref ExampleComplexObject obj);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ManagedCallback();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int InvokeManagedCallback();
}