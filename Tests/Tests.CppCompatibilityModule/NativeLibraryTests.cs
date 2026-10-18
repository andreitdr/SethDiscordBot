using System.Runtime.InteropServices;
using CppCompatibilityModule;
using CppCompatibilityModule.Extern;
using DiscordBotCore.Logging;
using Moq;

namespace Tests.CppCompatibilityModule;

/// <summary>Uses the C runtime that every machine has, and its <c>abs</c> function, as the native library.</summary>
public class NativeLibraryTests
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AbsDelegate(int value);

    private static string CRuntime =>
        OperatingSystem.IsWindows() ? "msvcrt.dll" :
        OperatingSystem.IsMacOS() ? "/usr/lib/libSystem.B.dylib" :
        "libc.so.6";

    private static readonly string MissingLibrary = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.so");

    private readonly ILogger _logger = new Mock<ILogger>().Object;

    #region ExternLibrary

    [Fact]
    public void InitializeLibrary_LoadsAnExistingLibrary()
    {
        var library = new ExternLibrary(_logger, CRuntime);

        Assert.True(library.InitializeLibrary().IsSuccess);
        Assert.NotEqual(IntPtr.Zero, library.LibraryHandle);
        Assert.Equal(CRuntime, library.LibraryPath);

        library.FreeLibrary();
    }

    [Fact]
    public void InitializeLibrary_Twice_KeepsTheSameHandle()
    {
        var library = new ExternLibrary(_logger, CRuntime);
        library.InitializeLibrary();
        IntPtr handle = library.LibraryHandle;

        Assert.True(library.InitializeLibrary().IsSuccess);
        Assert.Equal(handle, library.LibraryHandle);

        library.FreeLibrary();
    }

    [Fact]
    public void InitializeLibrary_FailsForAMissingLibrary()
    {
        var library = new ExternLibrary(_logger, MissingLibrary);

        var result = library.InitializeLibrary();

        Assert.False(result.IsSuccess);
        Assert.IsType<DllNotFoundException>(result.Match<Exception?>(() => null, ex => ex));
        Assert.Equal(IntPtr.Zero, library.LibraryHandle);
    }

    [Fact]
    public void GetDelegateForFunctionPointer_CallsTheNativeFunction()
    {
        var library = new ExternLibrary(_logger, CRuntime);
        library.InitializeLibrary();

        var abs = library.GetDelegateForFunctionPointer<AbsDelegate>("abs");

        Assert.Equal(42, abs(-42));
        library.FreeLibrary();
    }

    [Fact]
    public void GetDelegateForFunctionPointer_ThrowsForAMissingFunction()
    {
        var library = new ExternLibrary(_logger, CRuntime);
        library.InitializeLibrary();

        Assert.Throws<EntryPointNotFoundException>(() => library.GetDelegateForFunctionPointer<AbsDelegate>("no_such_function_here"));
        library.FreeLibrary();
    }

    [Fact]
    public void GetDelegateForFunctionPointer_NeedsALoadedLibrary()
    {
        var library = new ExternLibrary(_logger, CRuntime);

        Assert.Throws<InvalidOperationException>(() => library.GetDelegateForFunctionPointer<AbsDelegate>("abs"));
    }

    [Fact]
    public void FreeLibrary_ResetsTheHandleAndCanBeCalledAgain()
    {
        var library = new ExternLibrary(_logger, CRuntime);
        library.InitializeLibrary();

        library.FreeLibrary();
        library.FreeLibrary();

        Assert.Equal(IntPtr.Zero, library.LibraryHandle);
    }

    #endregion

    #region ExternalApplication and its handler

    [Fact]
    public void CreateFromDllFile_ReturnsNullForAMissingLibrary()
    {
        Assert.Null(ExternalApplication.CreateFromDllFile(_logger, MissingLibrary));
    }

    [Fact]
    public void CreateFromDllFile_GivesTheApplicationAnId()
    {
        ExternalApplication? application = ExternalApplication.CreateFromDllFile(_logger, CRuntime);

        Assert.NotNull(application);
        Assert.NotEqual(Guid.Empty, application.ApplicationId);
    }

    [Fact]
    public void Handler_CreatesApplicationsAndCallsTheirFunctions()
    {
        var handler = new ExternalApplicationHandler(_logger);

        Guid first = handler.CreateApplication(CRuntime);
        Guid second = handler.CreateApplication(CRuntime);

        Assert.NotEqual(Guid.Empty, first);
        Assert.NotEqual(first, second);
        Assert.Equal(7, handler.GetFunctionDelegate<AbsDelegate>(first, "abs")(-7));

        handler.StopApplication(first);
        handler.StopApplication(second);
    }

    [Fact]
    public void Handler_ReturnsAnEmptyIdForAMissingLibrary()
    {
        Assert.Equal(Guid.Empty, new ExternalApplicationHandler(_logger).CreateApplication(MissingLibrary));
    }

    [Fact]
    public void Handler_UnknownOrStoppedApplications_HaveNoFunctions()
    {
        var handler = new ExternalApplicationHandler(_logger);
        Guid id = handler.CreateApplication(CRuntime);
        handler.StopApplication(id);

        Assert.Null(handler.GetFunctionDelegate<AbsDelegate>(id, "abs"));
        Assert.Null(handler.GetFunctionDelegate<AbsDelegate>(Guid.NewGuid(), "abs"));
        Assert.Null(handler.SetExternFunctionToPointToFunction(Guid.NewGuid(), "set", () => { }));

        // Stopping again only logs
        handler.StopApplication(id);
    }

    #endregion
}
