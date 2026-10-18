# Native (C/C++) libraries

A plugin can call into a native shared library (`.so` on Linux, `.dylib` on macOS, `.dll` on Windows), for example to reuse existing C++ code. You can do this with plain .NET interop (`NativeLibrary`, `Marshal.GetDelegateForFunctionPointer`), or with the helper module in `Modules/CppCompatibilityModule`, which the `CppModuleDemo` sample uses.

## The native side

Export plain C functions (`extern "C"`) so their names aren't mangled, and keep the data types interop-friendly:

```cpp
// include/MyNative.h
#pragma once

#ifdef _WIN32
  #define MY_API __declspec(dllexport)
#else
  #define MY_API
#endif

extern "C" {

struct ExampleComplexObject
{
    int integerValue;
    double doubleValue;
    char stringValue[256];
};

using ManagedCallback = void (*)();

MY_API void modifyComplexObject(ExampleComplexObject* obj);
MY_API void setManagedCallback(ManagedCallback callback);
MY_API int invokeManagedCallback();

}
```

Build it as a shared library. With CMake:

```cmake
cmake_minimum_required(VERSION 3.20)
project(MyNative LANGUAGES CXX)
set(CMAKE_CXX_STANDARD 17)

add_library(MyNative SHARED src/MyNative.cpp)
target_include_directories(MyNative PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/include)
```

```bash
cmake -S Native -B Native/build && cmake --build Native/build
```

Build it for every OS (and CPU architecture) you deploy to; a library built on macOS doesn't load on Linux.

## The managed side

### Matching types and delegates

```csharp
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct ExampleComplexObject
{
    public int IntegerValue;
    public double DoubleValue;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string StringValue;
}

public static class Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ModifyComplexObject(ref ExampleComplexObject obj);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ManagedCallback();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int InvokeManagedCallback();
}
```

The struct fields must be in the same order with the same sizes as in C++. A C++ `T*` parameter becomes `ref T`.

### With `CppCompatibilityModule`

```csharp
using CppCompatibilityModule;

var handler = new ExternalApplicationHandler(args.Logger);

// Load the library; Guid.Empty means it failed (the reason is in the console).
Guid id = handler.CreateApplication(Path.Combine(args.PluginBaseDirectory.FullName, "libMyNative.so"));

// Call a C++ function.
var modify = handler.GetFunctionDelegate<Delegates.ModifyComplexObject>(id, "modifyComplexObject");
var value = new ExampleComplexObject { IntegerValue = 1, DoubleValue = 2.0, StringValue = "hi" };
modify(ref value);

// Let C++ call C#: pass a delegate to an exported setter that takes a function pointer.
Delegates.ManagedCallback callback = () => args.Logger.Log("C++ called us");
KeepAlive = callback; // see below
handler.SetExternFunctionToPointToFunction(id, "setManagedCallback", callback);

// Unload the library.
handler.StopApplication(id);
```

| Method | Does |
| --- | --- |
| `CreateApplication(path)` | Loads the library, returns an id (`Guid.Empty` on failure). |
| `GetFunctionDelegate<T>(id, name)` | Looks up an exported function and wraps it in delegate type `T`. Throws `EntryPointNotFoundException` if it isn't exported. |
| `SetExternFunctionToPointToFunction(id, setterName, @delegate)` | Calls the exported `setterName(void* fn)` with a function pointer to your C# delegate. |
| `StopApplication(id)` | Frees the library. |

`CppCompatibilityModule.dll` isn't part of the bot. Reference the project normally (it's copied to your output) and ship the DLL in `Libraries/<your plugin assembly>/`; see [Dependencies](dependencies.md).

### With plain .NET

```csharp
IntPtr lib = NativeLibrary.Load(fullPath);
IntPtr fn = NativeLibrary.GetExport(lib, "modifyComplexObject");
var modify = Marshal.GetDelegateForFunctionPointer<Delegates.ModifyComplexObject>(fn);
// …
NativeLibrary.Free(lib);
```

No extra assembly to ship.

## Where to put the native library

Load it by **full path**. A good place is the plugin's data folder, which an event receives as `PluginBaseDirectory` (`Data/Resources/<plugin assembly name>/`). Pick the file name by OS:

```csharp
static string NativeFileName() =>
    OperatingSystem.IsWindows() ? "MyNative.dll" :
    OperatingSystem.IsLinux()   ? "libMyNative.so" :
                                  "libMyNative.dylib";
```

## Pitfalls

- **Keep delegates passed to native code alive.** If the delegate object is garbage-collected, the function pointer C++ holds becomes invalid and the next call crashes the process. Store it in a static field (`CppModuleDemo` keeps it in `InternalSettings.ManagedCallbackReference`) until the library is freed.
- A crash inside native code (access violation, abort) takes down the whole bot, not just the plugin. Validate inputs before calling native code.
- Native libraries aren't unloaded when plugins reload. Free them yourself (the demo has a `/stop-cpp-module-demo` command), or load them only once.
- On macOS, unsigned libraries downloaded from the internet can be blocked by Gatekeeper; build them locally or remove the quarantine attribute.

See `Plugins/CppModuleDemo` for the complete example and [Sample plugins](../using-plugins/sample-plugins.md#cppmoduledemo) for how to run it.
