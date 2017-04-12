using System;

namespace Barebones.Logging
{
    public delegate void LogHandler(BmLogger logger, LogLevel logLevel, object message);

    public class BmLogger
    {
        public event LogHandler OnLog;

        public LogLevel LogLevel { get; set; }

        public string Name { get; private set; }

        public BmLogger(string name)
        {
            Name = name;
            LogLevel = LogLevel.Off;
        }

        /// <summary>
        /// Returns true, if message of this level will be logged
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool IsLogging(LogLevel level)
        {
            return (LogLevel <= level || (LogLevel == LogLevel.Global && level >= LogManager.GlobalLogLevel));
        }

        public void Trace(object message)
        {
            Log(LogLevel.Trace, message);
        }

        public void Trace(bool condition, object message)
        {
            if (condition)
                Log(LogLevel.Trace, message);
        }

        public void Debug(object message)
        {
            Log(LogLevel.Debug, message);
        }

        public void Debug(bool condition, object message)
        {
            if (condition)
                Log(LogLevel.Debug, message);
        }

        public void Info(object message)
        {
            Log(LogLevel.Info, message);
        }

        public void Info(bool condition, object message)
        {
            if (condition)
                Log(LogLevel.Info, message);
        }

        public void Warn(object message)
        {
            Log(LogLevel.Warn, message);
        }

        public void Warn(bool condition, object message)
        {
            if (condition)
                Log(LogLevel.Warn, message);
        }

        public void Error(object message)
        {
            Log(LogLevel.Error, message);
        }

        public void Error(bool condition, object message)
        {
            if (condition)
                Log(LogLevel.Error, message);
        }

        public void Fatal(object message)
        {
            Log(LogLevel.Fatal, message);
        }

        public void Fatal(bool condition, object message)
        {
            if (condition)
                Log(LogLevel.Fatal, message);
        }

        public void Log(bool condition, LogLevel logLvl, object message)
        {
            if (condition)
                Log(logLvl, message);
        }

        public void Log(LogLevel logLvl, object message)
        {
            // If nothing listens to logs,
            if (OnLog == null)
                return;

            if (LogManager.ForceLogLevel != LogLevel.Off && logLvl >= LogManager.ForceLogLevel)
            {
                OnLog(this, logLvl, message);
                return;
            }

            // If logging level is lower than what we're logging (including global)
            if (LogLevel <= logLvl || (LogLevel == LogLevel.Global && logLvl >= LogManager.GlobalLogLevel))
            {
                OnLog(this, logLvl, message);
            }
        }
    }
}