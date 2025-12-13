using System.Runtime.CompilerServices;
using Native;
using Native.Logging; // Assuming NativeLogger<T> is here

namespace WDBJsonTool;

// 1. Define a dummy class to represent this Assembly's Identity


// 2. The Global Static Facade
internal static class Log
{
    private class WDBLIB {}
    // This creates the single instance named "[WDBLIB]"
    private static readonly NativeLogger<WDBLIB> Logger = new();

    // 3. Forward the calls
    // CRITICAL: You must repeat the [Caller...] attributes here!
    // If you don't, the logger will think the error came from "Log.cs" instead of your actual code.
    
    public static void Finest(string message,
        [CallerFilePath] string path = "",
        [CallerLineNumber] int line = 0)
    {
        Logger.Finest(message, path, line);
    }

    public static void Fine(string message,
        [CallerFilePath] string path = "",
        [CallerLineNumber] int line = 0)
    {
        Logger.Fine(message, path, line);
    }

    public static void Info(string message,
        [CallerFilePath] string path = "",
        [CallerLineNumber] int line = 0)
    {
        Logger.Info(message, path, line);
    }

    public static void Warning(string message,
        [CallerFilePath] string path = "",
        [CallerLineNumber] int line = 0)
    {
        Logger.Warning(message, path, line);
    }

    public static void Fatal(string message,
        [CallerFilePath] string path = "",
        [CallerLineNumber] int line = 0)
    {
        Logger.Fatal(message, path, line);
    }
}