using System.Runtime.InteropServices;

namespace CppCompatibilityModule.Extern;

public static class Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ProcessObject(ref object obj);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ExecuteDelegateFunction();
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetExternFunctionPointerDelegate(IntPtr funcPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CsharpFunctionDelegate();
}
