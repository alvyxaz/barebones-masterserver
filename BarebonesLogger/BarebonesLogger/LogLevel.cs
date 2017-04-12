namespace Barebones.Logging
{
    public enum LogLevel : byte
    {
        All,
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal,

        Global,
        Off
    }
}