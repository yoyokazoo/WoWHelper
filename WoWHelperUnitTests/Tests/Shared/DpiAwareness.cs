using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public static class DpiBootstrap
{
    [AssemblyInitialize]
    public static void Init(TestContext _)
    {
        // Must run before any window/framework initializes.
        DpiAwareness.EnablePerMonitorV2();
    }
}

internal static class DpiAwareness
{
    // Windows 10+ constant: DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = (HANDLE)-4
    private static readonly IntPtr PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    public static void EnablePerMonitorV2()
    {
        // If this returns false, the process may already be "locked in" or OS too old.
        SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2);
    }
}
