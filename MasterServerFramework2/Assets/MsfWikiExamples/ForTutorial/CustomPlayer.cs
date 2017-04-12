using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CustomPlayer : NetworkBehaviour
{
    public TextMesh UsernameMesh;

    void Awake()
    {
        UsernameMesh = UsernameMesh ?? GetComponentInChildren<TextMesh>();
    }

    void Update()
    {
        Logs.Error(isLocalPlayer);

        if (!isLocalPlayer)
            return;


        var horizontal = Input.GetAxis("Horizontal") * Time.deltaTime * 10;
        var vertical = Input.GetAxis("Vertical") * Time.deltaTime * 10;

        transform.Translate(horizontal, 0, vertical);

    }
}
