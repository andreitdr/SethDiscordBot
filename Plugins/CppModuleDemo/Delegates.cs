using System.Runtime.InteropServices;

namespace CppModuleDemo;

public static class Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ModifyComplexObject(ref ExampleComplexObject obj);
}