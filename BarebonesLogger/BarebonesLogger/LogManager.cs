using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Barebones.Logging
{
    public static class LogManager
    {
        private static LogLevel _globalLogLevel;
        private static LogHandler _appenders;

        private static Dictionary<string, BmLogger> _loggers = new Dictionary<string, BmLogger>();

        private static Queue<PooledLog> _pooledLogs;

        public static bool EnableCurrentClassLogger = true;

        /// <summary>
        /// Overrides logging sett
        /// </summary>
        public static LogLevel GlobalLogLevel { get; set; }

        /// <summary>
        /// This overrides all logging settings
        /// </summary>
        public static LogLevel ForceLogLevel { get; set; }

        public static bool IsInitialized { get; private set; }

        public static int InitializationPoolSize = 100;

        static LogManager()
        {
            ForceLogLevel = LogLevel.Off;
            _pooledLogs = new Queue<PooledLog>();
        }

        public static void Initialize(IEnumerable<LogHandler> appenders, LogLevel globalLogLevel)
        {
            GlobalLogLevel = globalLogLevel;

            foreach (var appender in appenders)
            {
                AddAppender(appender);
            }

            IsInitialized = true;

            // Disable pre-initialization pooling
            foreach (var logger in _loggers.Values)
            {
                logger.OnLog -= OnPooledLoggerLog;
            }

            // Push logger messages from pool to loggers
            while (_pooledLogs.Count > 0)
            {
                var log = _pooledLogs.Dequeue();
                log.BmLogger.Log(log.LogLevel, log.Message);
            }

            _pooledLogs.Clear();
        }

        public static void AddAppender(LogHandler appender)
        {
            _appenders += appender;
            foreach (var logger in _loggers.Values)
            {
                logger.OnLog += appender;
            }
        }

        public static void RemoveAppender(LogHandler appender)
        {
            _appenders -= appender;
            foreach (var logger in _loggers.Values)
            {
                logger.OnLog -= appender;
            }
        }

        public static BmLogger GetLogger(string name)
        {
            return GetLogger(name, true);
        }

        public static BmLogger GetCurrentClassLogger()
        {
            if (EnableCurrentClassLogger)
            {
                var frame = new StackFrame(1, false);
                return GetLogger(frame.GetMethod().DeclaringType.FullName);
            }
            else
            {
                return Logs.Logger;
            }
            
        }

        public static BmLogger GetLogger(string name, bool poolUntilInitialized)
        {
            BmLogger logger;
            if (!_loggers.TryGetValue(name, out logger))
            {
                logger = CreateLogger(name);
                _loggers.Add(name, logger);
            }

            if (!IsInitialized && poolUntilInitialized)
            {
                // Register to pre-initialization pooling
                logger.OnLog += OnPooledLoggerLog;
            }

            return logger;
        }

        private static void OnPooledLoggerLog(BmLogger logger, LogLevel level, object message)
        {
            var log = _pooledLogs.Count >= InitializationPoolSize ? _pooledLogs.Dequeue() : new PooledLog();

            log.LogLevel = level;
            log.BmLogger = logger;
            log.Message = message;
            log.Date = DateTime.Now;

            _pooledLogs.Enqueue(log);
        }

        public static void Reset()
        {
            _loggers.Clear();
            _appenders = null;
        }

        private static BmLogger CreateLogger(string name)
        {
            var logger = new BmLogger(name)
            {
                LogLevel = GlobalLogLevel
            };
            logger.OnLog += _appenders;
            return logger;
        }

        private class PooledLog
        {
            public DateTime Date;
            public LogLevel LogLevel;
            public BmLogger BmLogger;
            public object Message;
        }
    }
}