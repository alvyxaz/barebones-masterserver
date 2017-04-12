using UnityEngine;
using System.Collections;
using Barebones.MasterServer;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ZonePortal : NetworkBehaviour
{
    public string Title = "Teleport";
    public string ZoneId = "";

    public GameObject PortalNamePrefab;

    protected GameObject NameObject;

    [Tooltip("Position, to which player will be teleported in the next zone")]
    public Vector3 NewPosition;

    // Use this for initialization
    void Start ()
    {
        StartCoroutine(DisplayName());
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public IEnumerator DisplayName()
    {
        if (Msf.Args.DestroyUi)
            yield break;

        // Create a player name
        NameObject = Instantiate(PortalNamePrefab);
        NameObject.GetComponentInChildren<Text>().text = Title ?? "Portal";
        NameObject.transform.SetParent(FindObjectOfType<Canvas>().transform);

        while (true)
        {
            // While we're still "online"
            NameObject.transform.position = RectTransformUtility
                                                .WorldToScreenPoint(Camera.main, transform.position) + Vector2.up * 30;
            yield return 0;
        }
    }

    public void OnTeleportClick()
    {
        
    }

    public void OnTriggerEnter(Collider collider)
    {
        // Ignore if it's not the server who received the event
        if (!isServer) return;

        var playerCharacter = collider.GetComponent<MiniPlayerController>();

        // Ignore if collider is not a player
        if (playerCharacter == null) return;

        var server = FindObjectOfType<WorldDemoZoneRoom>();

        var player = server.GetPlayer(playerCharacter.Name);

        server.TeleportPlayerToAnotherZone(player, ZoneId, NewPosition);
    }
}
