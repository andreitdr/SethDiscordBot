using System.Runtime.InteropServices;

using CppWrapper.Objects;

namespace DiscordBotUI
{
    public abstract class Delegates
    {
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProcessApplicationData(ref ApplicationStruct appData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProcessComplexObject(ref ComplexObject complexObject);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CsharpFunctionDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetCsharpFunctionPointerDelegate(IntPtr funcPtr);
    }
}
