using System.Runtime.CompilerServices;
using Native; // Assuming NativeLogger<T> is here

namespace WDBJsonTool;

// 1. Define a dummy class to represent this Assembly's Identity
internal class WDBLIB {}

// 2. The Global Static Facade
internal static class Log
{
    // This creates the single instance named "[WPD_SCOPE]"
    private static readonly NativeLogger<WDBLIB> Logger = new();

    // 3. Forward the calls
    // CRITICAL: You must repeat the [Caller...] attributes here!
    // If you don't, the logger will think the error came from "Log.cs" instead of your actual code.
    
    public static void Debug(string message, 
        [CallerFilePath] string path = "", 
        [CallerLineNumber] int line = 0)
    {
        Logger.Debug(message, path, line);
    } 
    
    public static void Info(string message, 
        [CallerFilePath] string path = "", 
        [CallerLineNumber] int line = 0)
    {
        Logger.Info(message, path, line);
    }

    public static void Warn(string message, 
        [CallerFilePath] string path = "", 
        [CallerLineNumber] int line = 0)
    {
        Logger.Warn(message, path, line);
    }

    public static void Error(string message, 
        [CallerFilePath] string path = "", 
        [CallerLineNumber] int line = 0)
    {
        Logger.Error(message, path, line);
    }
}