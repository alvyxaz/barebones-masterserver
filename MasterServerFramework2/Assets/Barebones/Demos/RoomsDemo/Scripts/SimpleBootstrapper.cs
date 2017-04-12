using System;
using System.Collections;
using System.Collections.Generic;
using Barebones.Logging;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleBootstrapper : MonoBehaviour
{
    #region Unity's inspector
    public HelpBox _header = new HelpBox()
    {
        Text = "This script contains general code, which ",
        Type = HelpBoxType.Info
    };

    [Header("General")]
    [Tooltip("This log level only affects loggin in this script")]
    public LogLevel BootstrapperLogLevel = LogLevel.Info;
    [Tooltip("This logging level will be used whenever you log with static 'Logs' object, " +
             "like Logs.Warn and etc.")]
    public LogLevel GlobalLogsLevel = LogLevel.Info;

    [Header("Scene loading")]
    [Tooltip("If true, when argument with scene name is found, bootstrapper will load that scene")]
    public bool EnableSceneLoading = true;
    [Tooltip("If true, will switch scenes only after connection to 'master' is established")]
    public bool OnlyAfterConnected = true;
    public HelpBox _headerSceneA = new HelpBox()
    {
        Text = string.Format("If enabled, these settings will look for '{0}' argument, " +
                             "and load that scene.", Msf.Args.Names.LoadScene),
        Type = HelpBoxType.Info
    };

    public HelpBox _headerSceneB = new HelpBox()
    {
        Text = string.Format("If you don't use this script, you'll need to handle scene loading " +
                             "manually"),
        Type = HelpBoxType.Warning
    };

    #endregion

    private IClientSocket _connection;

    public BmLogger Logger = Msf.Create.Logger(typeof(SimpleBootstrapper).Name);

    void Awake()
    {
        Logs.Logger.LogLevel = GlobalLogsLevel;
        Logger.LogLevel = BootstrapperLogLevel;

        _connection = Msf.Connection;

        // Subscribe to connection event
        _connection.AddConnectionListener(OnConnectedToMaster, true);

        if (EnableSceneLoading // If scene loading is enabled
            && !OnlyAfterConnected // and not just after connecting
            && Msf.Args.IsProvided(Msf.Args.Names.LoadScene) // and argument is provided
            && SceneManager.GetActiveScene().name != Msf.Args.LoadScene) // and current scene is not the one we need
        {
            // Load the scene
            SceneManager.LoadScene(Msf.Args.LoadScene);
        }
    }

    /// <summary>
    /// Called, when connected to master server
    /// </summary>
    protected virtual void OnConnectedToMaster()
    {
        // Load another scene
        if (EnableSceneLoading // If scene loading is enabled
            && OnlyAfterConnected // and only load after connected to master
            && Msf.Args.IsProvided(Msf.Args.Names.LoadScene) // and argument is provided
            && SceneManager.GetActiveScene().name != Msf.Args.LoadScene) // and current scene is not the one we need
        {
            // Load the scene
            SceneManager.LoadScene(Msf.Args.LoadScene);
        }
    }

    void OnDestroy()
    {
        // Remove listener
        _connection.RemoveConnectionListener(OnConnectedToMaster);
    }
}
