using UnityEngine;
using UnityEngine.Networking;

public class MiniCoin : NetworkBehaviour
{
    private readonly float _reapperDistance = 8f;
    public Transform Shape;

    // Use this for initialization
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        Shape.Rotate(Vector3.up, Time.deltaTime*180);
    }

    private void OnTriggerEnter(Collider collider)
    {
        // Ignore if this is not the server
        if (!isServer)
            return;

        var randX = -_reapperDistance + Random.value*2*_reapperDistance;
        var randZ = -_reapperDistance + Random.value*2*_reapperDistance;

        // Move to random position within a square
        transform.position = new Vector3(randX, transform.position.y, randZ);
    }
}