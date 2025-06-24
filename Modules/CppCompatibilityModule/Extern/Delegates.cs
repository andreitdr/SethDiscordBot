using System.Runtime.InteropServices;

namespace CppCompatibilityModule.Extern;

public static class Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetExternFunctionPointerDelegate(IntPtr funcPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CsharpFunctionDelegate();
}
