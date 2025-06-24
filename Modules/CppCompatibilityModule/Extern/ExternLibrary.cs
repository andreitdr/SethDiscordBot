﻿using System.Net.Mime;
using System.Runtime.InteropServices;
using DiscordBotCore;
using DiscordBotCore.Logging;
using DiscordBotCore.Others;
using DiscordBotCore.Utilities;

namespace CppCompatibilityModule.Extern
{
    public sealed class ExternLibrary
    {
        private readonly ILogger _Logger;
        public string LibraryPath { get; init; }
        public IntPtr LibraryHandle { get; private set; }

        public ExternLibrary(ILogger logger, string libraryPath)
        {
            LibraryPath = libraryPath;
            LibraryHandle = IntPtr.Zero;
            _Logger = logger;
        }

        public Result InitializeLibrary()
        {
            if(LibraryHandle != IntPtr.Zero)
            {
                return Result.Success();
            }

            _Logger.Log($"Loading library {LibraryPath}");


            if(!NativeLibrary.TryLoad(LibraryPath, out IntPtr hModule))
            {
                return Result.Failure(new DllNotFoundException($"Unable to load library {LibraryPath}"));
            }

            _Logger.Log($"Library {LibraryPath} loaded successfully [{hModule}]");

            LibraryHandle = hModule;
            
            return Result.Success();
        }

        public void FreeLibrary()
        {
            if(LibraryHandle == IntPtr.Zero)
            {
                return;
            }

            NativeLibrary.Free(LibraryHandle);
            LibraryHandle = IntPtr.Zero;

            _Logger.Log($"Library {LibraryPath} freed successfully");
        }

        private IntPtr GetFunctionPointer(string functionName)
        {
            if(LibraryHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Library is not loaded");
            }

            if(!NativeLibrary.TryGetExport(LibraryHandle, functionName, out IntPtr functionPointer))
            {
                throw new EntryPointNotFoundException($"Unable to find function {functionName}");
            }

            return functionPointer;
        }

        public T GetDelegateForFunctionPointer<T>(string methodName) where T : Delegate
        {
            IntPtr functionPointer = GetFunctionPointer(methodName);

            _Logger.Log($"Function pointer for {methodName} obtained successfully [address: {functionPointer}]");
            
            T result = (T)Marshal.GetDelegateForFunctionPointer(functionPointer, typeof(T));

            _Logger.Log($"Delegate for {methodName} created successfully");

            return result;
        }

        private IntPtr GetFunctionPointerForDelegate<T>(T functionDelegate) where T : Delegate
        {
            IntPtr functionPointer = Marshal.GetFunctionPointerForDelegate(functionDelegate);

            _Logger.Log($"Function pointer for delegate {functionDelegate.Method.Name} obtained successfully [address: {functionPointer}]");

            return functionPointer;
        }

        /// <summary>
        /// Tells the extern setter function to point its function to this C# function instead.
        /// This function takes the name of the extern setter function and the C# function to be executed.
        /// <para><b>How it works:</b></para>
        /// Find the external setter method by its name. It should take one parameter, which is the pointer to the function to be executed.
        /// Take the delegate function that should be executed and get its function pointer.
        /// Call the external setter with the new function memory address. This should replace the old C++ function with the new C# function.
        /// </summary>
        /// <param name="setterExternFunctionName">The setter function name</param>
        /// <param name="executableFunction">The function that the C++ setter will make its internal function to point to</param>
        /// <typeparam name="ExecuteDelegate">A delegate that reflects the executable function structure</typeparam>
        /// <typeparam name="SetDelegate">The Setter delegate </typeparam>
        /// <returns>A response if it exists as an object</returns>
        public object? SetExternFunctionSetterPointerToCustomDelegate<SetDelegate, ExecuteDelegate>(string setterExternFunctionName, ExecuteDelegate executableFunction) where ExecuteDelegate : Delegate where SetDelegate : Delegate
        {
            SetDelegate setterDelegate        = GetDelegateForFunctionPointer<SetDelegate>(setterExternFunctionName);
            IntPtr                                     executableFunctionPtr = GetFunctionPointerForDelegate(executableFunction);

            var result = setterDelegate.DynamicInvoke(executableFunctionPtr);

            _Logger.Log($"Function {setterExternFunctionName} bound to local action successfully");

            return result;
        }
        
    }
}
