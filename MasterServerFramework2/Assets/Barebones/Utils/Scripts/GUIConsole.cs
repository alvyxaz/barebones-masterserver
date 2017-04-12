/*-------------------------------------------------
 *    Big thanks to Emil Rainero for contributing this script!
 *--------------------------------------------------*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Barebones
{
    /// <summary>
    /// Holds one unity console message
    /// </summary>
    public class LogEntry
    {
        public DateTime DateTime { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public LogType Type { get; set; }

        public LogEntry(DateTime dateTime, string message, string stackTrace, LogType type)
        {
            DateTime = dateTime;
            Message = message;
            StackTrace = stackTrace;
            Type = type;
        }
    }

    /// <summary>
    /// Implements a GUIWindow with a visual console listing
    /// </summary>
    public class GUIConsole : MonoBehaviour
    {
        public string Title = "Console";
        public bool Show = true;
        public bool ShowDateTime = true;
        public bool ShowLatest = true;
        public bool CollapseDuplicates = false;
        public KeyCode toggleKey = KeyCode.Escape;
        public int MaxLogEntries = 1000;

        [Header("Margins")]
        public int LeftMargin = 20;
        public int TopMargin = 20;
        public int RightMargin = 20;
        public int BottomMargin = 20;


        private List<LogEntry> _logEntries = new List<LogEntry>();
        private Vector2 _scrollPosition;

        static readonly Dictionary<LogType, Color> _logEntryColors = new Dictionary<LogType, Color>()
        {
            { LogType.Log, Color.white },
            { LogType.Warning, Color.yellow },
            { LogType.Assert, Color.cyan },
            { LogType.Exception, Color.red },
            { LogType.Error, Color.red },
        };

        private Rect _windowRect;
        private Rect _titleBarRect = new Rect(0, 0, 2000, 20);

        private GUIContent _clearLogEntriesLabel = new GUIContent("Clear", "Clear all log entries");
        private GUIContent _showDateTimeLabel = new GUIContent("Show Date/Time", "Show date/time");
        private GUIContent _showLatestLabel = new GUIContent("Show Latest", "Show the latest log entries");
        private GUIContent _collapseDuplicatesLabel = new GUIContent("Collapse Duplicates", "Collapse duplicate log entries");

        void OnEnable()
        {
            Application.logMessageReceived += OnLogMessageReceived;
            SetWindowRect();
        }

        void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                Show = !Show;
                if (Show)
                {
                    SetWindowRect();
                }
            }
        }

        void OnGUI()
        {
            if (Show)
            {
                _windowRect = GUILayout.Window(666, _windowRect, ConsoleWindow,
                    string.Format("{0}  (Hit '{1}' to hide/show)", Title, toggleKey.ToString()));
            }
        }

        private void SetWindowRect()
        {
            _windowRect = new Rect(LeftMargin, TopMargin, Screen.width - (LeftMargin + RightMargin), Screen.height - (TopMargin + BottomMargin));
        }

        private void ConsoleWindow(int windowID)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            // Iterate through the recorded logs.
            lock (_logEntries)
            {
                for (int i = 0; i < _logEntries.Count; i++)
                {
                    var logEntry = _logEntries[i];
                    int duplicates = CollapseDuplicates ? CountDuplicates(i, logEntry) : 0;

                    GUI.contentColor = _logEntryColors[logEntry.Type]; // set the color

                    string message = FormatLogEntry(logEntry, duplicates);
                    GUILayout.Label(message); // display the message

                    if (CollapseDuplicates && duplicates > 0)
                    {
                        i += duplicates; // skip the duplicates
                    }
                }
            }

            if (ShowLatest)
                _scrollPosition.y += 9999; // force to end

            GUILayout.EndScrollView();

            GUI.contentColor = Color.white;

            // display a row of buttons/toggles at bottom of window
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(_clearLogEntriesLabel))
                {
                    _logEntries.Clear();
                }

                ShowLatest = GUILayout.Toggle(ShowLatest, _showLatestLabel, GUILayout.ExpandWidth(false));
                ShowDateTime = GUILayout.Toggle(ShowDateTime, _showDateTimeLabel, GUILayout.ExpandWidth(false));
                CollapseDuplicates = GUILayout.Toggle(CollapseDuplicates, _collapseDuplicatesLabel, GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            // allow the window to be dragged
            GUI.DragWindow(_titleBarRect);
        }

        /// <summary>
        /// Format a log entry with all the options
        /// </summary>
        private string FormatLogEntry(LogEntry logEntry, int duplicates)
        {
            string message = string.Empty;
            if (ShowDateTime)
            {
                message += logEntry.DateTime.ToString("HH:mm:ss.fff");
            }
            if (CollapseDuplicates && duplicates > 0)
            {
                if (message.Length > 0)
                    message += " ";
                message += string.Format("({0})", duplicates + 1);
            }
            if (message.Length > 0)
            {
                message += ": ";
            }
            message += logEntry.Message;

            if (logEntry.Type == LogType.Error)
                message += " " + logEntry.StackTrace;

            return message;
        }

        /// <summary>
        /// count consecutive log entries that are exact duplicates
        /// </summary>
        private int CountDuplicates(int startingLogIndex, LogEntry logEntry)
        {
            int duplicates = 0;

            for (int j = startingLogIndex + 1; j < _logEntries.Count; j++)
            {
                if (logEntry.Message == _logEntries[j].Message)
                {
                    duplicates++;
                }
                else
                {
                    break;
                }
            }

            return duplicates;
        }

        /// <summary>
        /// handle a log message received event - save the log message into a list
        /// </summary>
        private void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            lock (_logEntries)
            {
                _logEntries.Add(new LogEntry(DateTime.Now, message, stackTrace, type));

                // trim the log
                while (_logEntries.Count > MaxLogEntries)
                {
                    _logEntries.RemoveAt(0); // remove oldest
                }
            }
        }
    }

}
