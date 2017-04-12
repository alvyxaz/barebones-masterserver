using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;

public class UnetGameTerminator : MonoBehaviour
{
    public HelpBox _header = new HelpBox()
    {
        Text = "This script quits the application if some of the conditions are met. " +
               "It's recommended that you create your own termination scripts"
    };

    public UnetGameRoom Room;

    [Tooltip("Terminates server if first player doesn't join in a given number of seconds")]
    public float FirstPlayerTimeoutSecs = 25;

    [Tooltip("Terminates if room is not registered in a given number of seconds")]
    public float RoomRegistrationTimeoutSecs = 15;

    [Tooltip("Once every given number of seconds checks if the room is empty." +
             " If it is - terminates it")]
    public float TerminateEmptyOnIntervals = 60;

    [Tooltip("Each second, will check if connected to master. If not - quits the application")]
    public bool TerminateIfNotConnected = true;

    [Tooltip("If true, quit the application immediately, when the last player quits")]
    public bool TerminateWhenLastPlayerQuits = true;

    private bool _hasFirstPlayerShowedUp = false;

    // Use this for initialization
    void Start () {

        if (!Msf.Args.IsProvided(Msf.Args.Names.SpawnCode))
        {
            // If this game server was not spawned by a spawner
            Destroy(gameObject);
            return;
        }

	    if (Room == null)
	    {
	        Logs.Error("Room is not set");
	        return;
	    }

	    Room.PlayerLeft += OnPlayerLeft;
	    Room.PlayerJoined += OnPlayerJoined;

        if (RoomRegistrationTimeoutSecs > 0)
            StartCoroutine(StartStartedTimeout(RoomRegistrationTimeoutSecs));

        if (FirstPlayerTimeoutSecs > 0)
            StartCoroutine(StartFirstPlayerTimeout(FirstPlayerTimeoutSecs));

        if (TerminateEmptyOnIntervals > 0)
            StartCoroutine(StartEmptyIntervalsCheck(TerminateEmptyOnIntervals));

        if (TerminateIfNotConnected)
            StartCoroutine(StartWaitingForConnectionLost());
    }

    private void OnPlayerJoined(UnetMsfPlayer obj)
    {
        _hasFirstPlayerShowedUp = true;
    }

    private void OnPlayerLeft(UnetMsfPlayer obj)
    {
        if (TerminateWhenLastPlayerQuits && Room.GetPlayers().Count == 0)
        {
            Application.Quit();
        }
    }

    /// <summary>
    ///     Each second checks if we're still connected, and if we are not,
    ///     terminates game server
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartWaitingForConnectionLost()
    {
        // Wait at least 5 seconds until first check
        yield return new WaitForSeconds(5);

        while (true)
        {
            yield return new WaitForSeconds(1);
            if (!Msf.Connection.IsConnected)
            {
                Logs.Error("Terminating game server, no connection");
                Application.Quit();
            }
        }
    }

    /// <summary>
    ///     Each time, after the amount of seconds provided passes, checks
    ///     if the server is empty, and if it is - terminates application
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    private IEnumerator StartEmptyIntervalsCheck(float timeout)
    {
        while (true)
        {
            yield return new WaitForSeconds(timeout);
            if (Room == null || Room.GetPlayers().Count <= 0)
            {
                Logs.Error("Terminating game server because it's empty at the time of an interval check.");
                Application.Quit();
            }
        }
    }

    /// <summary>
    ///     Waits a number of seconds, and checks if the game room was registered
    ///     If not - terminates the application
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartStartedTimeout(float timeout)
    {
        yield return new WaitForSeconds(timeout);
        if (Room == null || !Room.IsRoomRegistered)
            Application.Quit();
    }

    private IEnumerator StartFirstPlayerTimeout(float timeout)
    {
        yield return new WaitForSeconds(timeout);
        if (!_hasFirstPlayerShowedUp)
        {
            Logs.Error("Terminated game server because first player didn't show up");
            Application.Quit();
        }
    }
}
