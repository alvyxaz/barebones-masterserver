using System;
using System.Collections;
using System.Linq;
using Barebones.Logging;
using UnityEngine;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Starts the master server
    /// </summary>
    public class MasterServerBehaviour : ServerBehaviour
    {
        #region Unity Inspector Fields

        [Header("Master Server")]
        public HelpBox _hbInfo = new HelpBox()
        {
            Text = "This component is responsible for starting a Master Server " +
                   "and initializing its modules",
            Type = HelpBoxType.Info
        };

        [Tooltip("Port, to which master will listen")]
        public int Port = 5000;

        [Tooltip("Log level of this script")]
        public LogLevel LogLevel = LogLevel.Info;

        [Header("Editor settings")]
        public bool AutoStartInEditor = true;

        public HelpBox _hbEditor = new HelpBox()
        {
            Text = "Editor settings are used only while running in editor",
            Type = HelpBoxType.Warning
        };

        #endregion

        public BmLogger Logger = Msf.Create.Logger(typeof(MasterServerBehaviour).Name);

        public static event Action<MasterServerBehaviour> MasterStarted;
        public static event Action<MasterServerBehaviour> MasterStopped;
        public static bool IsMasterRunning { get; protected set; }

        public bool WillBeStarted { get; private set; }

        private static MasterServerBehaviour _instance;

        protected override void Awake()
        {
            base.Awake();

            if (_instance != null)
            {
                // Destroy, if this is not the first instance
                Destroy(gameObject);
                return;
            }

            _instance = this;

            Logger.LogLevel = LogLevel;

            // Move to root, so that it won't be destroyed
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);

            if (Msf.Args.IsProvided(Msf.Args.Names.MasterPort))
            {
                // If master port is provided via cmd arguments
                Port = Msf.Args.MasterPort;
            }
        }

        protected virtual void Start()
        {
            if (Msf.Args.StartMaster || (Msf.Runtime.IsEditor && AutoStartInEditor))
            {
                // Start the master server on next frame
                StartCoroutine(StartOnNextFrame());
            }
        }

        public IEnumerator StartOnNextFrame()
        {
            yield return null;

            StartServer(Port);
        }

        public void StartServer()
        {
            StartServer(Port);
            IsMasterRunning = base.IsRunning;
        }

        public override void StartServer(int port)
        {
            if (IsRunning)
                return;

            Logger.Debug("Starting on port: " + port + "...");

            base.StartServer(port);

            Logger.Info("Started on port: " + port);

            // Notify about uninitialized modules
            var uninitializedModules = GetUninitializedModules();
            if (uninitializedModules.Count > 0)
            {
                Logger.Warn("Some of the Master Server modules failed to initialize: \n" +
                            string.Join(" \n", uninitializedModules.Select(m => m.GetType().ToString()).ToArray()));
            }

            // Notify about initialized modules
            if (Logger.IsLogging(LogLevel.Debug))
            {
                Logger.Warn("Successfully initialized modules: \n" +
                            string.Join(" \n", GetInitializedModules().Select(m => m.GetType().ToString()).ToArray()));
            }

            OnStarted();

            IsMasterRunning = IsRunning;

            // Invoke the event
            if (MasterStarted != null)
                MasterStarted.Invoke(this);
        }

        protected virtual void OnStarted()
        {
            
        }

        protected override void OnServerStopped()
        {
            base.OnServerStopped();

            IsMasterRunning = IsRunning;

            // Invoke the event
            if (MasterStopped != null)
                MasterStopped.Invoke(this);
        }
    }
}