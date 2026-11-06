using System.Runtime.InteropServices;

namespace Tests.NativePlugin;

public static class NativeCalls
{
    private static Timer? _timer;

    [DllImport("libsodium", EntryPoint = "sodium_library_version_major")]
    private static extern int SodiumMajorVersion();

    public static int Read() => SodiumMajorVersion();

    /// <summary>A library the tests compile themselves, because unlike libsodium the OS can always unmap it.</summary>
    [DllImport("answer", EntryPoint = "answer")]
    private static extern int NativeAnswer();

    public static int Answer() => NativeAnswer();

    /// <summary>What a badly behaved plugin does: nothing can collect its load context while this is running.</summary>
    public static void LeaveTimerRunning() => _timer = new Timer(_ => GC.KeepAlive(typeof(NativeCalls)), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(50));
}
