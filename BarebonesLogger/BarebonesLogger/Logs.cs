using Barebones.Logging;

public class Logs
{
    private static BmLogger _genericLogger;

    public static BmLogger Logger { get; private set; }

    static Logs()
    {
        _genericLogger = LogManager.GetLogger("Logs");
        Logger = _genericLogger;
    }

    public static void Trace(object message)
    {
        Log(LogLevel.Trace, message);
    }

    public static void Trace(bool condition, object message)
    {
        if (condition)
            Log(LogLevel.Trace, message);
    }

    public static void Debug(object message)
    {
        Log(LogLevel.Debug, message);
    }

    public static void Debug(bool condition, object message)
    {
        if (condition)
            Log(LogLevel.Debug, message);
    }

    public static void Info(object message)
    {
        Log(LogLevel.Info, message);
    }

    public static void Info(bool condition, object message)
    {
        if (condition)
            Log(LogLevel.Info, message);
    }

    public static void Warn(object message)
    {
        Log(LogLevel.Warn, message);
    }

    public static void Warn(bool condition, object message)
    {
        if (condition)
            Log(LogLevel.Warn, message);
    }

    public static void Error(object message)
    {
        Log(LogLevel.Error, message);
    }

    public static void Error(bool condition, object message)
    {
        if (condition)
            Log(LogLevel.Error, message);
    }

    public static void Fatal(object message)
    {
        Log(LogLevel.Fatal, message);
    }

    public static void Fatal(bool condition, object message)
    {
        if (condition)
            Log(LogLevel.Fatal, message);
    }

    public static void Log(LogLevel logLvl, object message)
    {
        _genericLogger.Log(logLvl, message);
    }

    public static void Log(bool condition, LogLevel logLvl, object message)
    {
        if (condition)
            Log(logLvl, message);
    }
}