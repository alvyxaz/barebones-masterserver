using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleAccessHandler : MonoBehaviour
{
    public HelpBox _header = new HelpBox("This script waits for game server access to be received, " +
                                         "and then loads the appropriate scene");

    [Tooltip("If true, and if access contains the scene name - this script will automatically load that scene")]
    public bool LoadSceneIfFound = true;

    void Awake()
    {
        Msf.Client.Rooms.AccessReceived += OnAccessReceived;
    }

    private void OnAccessReceived(RoomAccessPacket access)
    {
        // Set the access
        UnetRoomConnector.RoomAccess = access;

        if (LoadSceneIfFound && access.Properties.ContainsKey(MsfDictKeys.SceneName))
        {
            var sceneName = access.Properties[MsfDictKeys.SceneName];
            if (sceneName != SceneManager.GetActiveScene().name)
            {
                // If we're at the wrong scene
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                // If we're already at the correct scene
                FindObjectOfType<UnetRoomConnector>().ConnectToGame(access);
            }
        }
    }

    void OnDestroy()
    {
        Msf.Client.Rooms.AccessReceived -= OnAccessReceived;
    }
}
