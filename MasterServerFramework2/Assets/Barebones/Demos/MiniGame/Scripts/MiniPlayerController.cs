using System;
using System.Collections;
using System.Linq;
using Barebones.MasterServer;
using Barebones.Utils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MiniPlayerController : NetworkBehaviour
{
    private CharacterController _characterController;

    private float _fallVelocity;
    private readonly float _fallVelocityMax = 20f;
    private readonly float _forwardMaxSpeed = 5f;

    private float _forwardSpeed;

    private readonly float _yDeathValue = -20f;
    private readonly float _rotationMaxVelocity = 270;

    //private Vector3 _rotationVelocity;
    private float _rotationVelocity;

    [SyncVar(hook = "OnCoinsChange")]
    public int Coins;

    public Color CurrentPlayerColor;
    public SpriteRenderer Direction;

    [SyncVar]
    public string Name;

    [SyncVar(hook = "OnWeaponChange")]
    public string WeaponSpriteName;

    [SyncVar(hook= "OnFlagChange")]
    public string FlagColor = "";

    private Text _nameObject;
    public Text NamePrefab;

    public Transform NameTransform;
    public GameObject Shape;

    public SpriteRenderer Weapon;

    public event Action CoinsChanged;

    public GameObject Flag;

    // Use this for initialization
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _characterController.detectCollisions = false;

        StartCoroutine(DisplayName());
    }

    public void Setup(string username)
    {
        Name = username;
    }

    public override void OnStartClient()
    {
        SetWeapon(WeaponSpriteName);
        SetFlagColor(FlagColor);
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        // Change colors
        var color = CurrentPlayerColor;
        Direction.color = new Color(color.r, color.g, color.b, 0.5f);
        Shape.GetComponent<MeshRenderer>().material.color = color;

        // Notify UI
        if (MiniGameUi.Instance != null)
        {
            MiniGameUi.Instance.OnPlayerSpawned(this);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        // Ignore input from other players
        if (!isLocalPlayer)
            return;

        UpdateMovement();

        // Input
        if (MiniGameUi.Instance != null && MiniGameUi.Instance.IsAutoWalk)
        {
            _forwardSpeed = 0.7f * _forwardMaxSpeed;
            _rotationVelocity = 0.3f * _rotationMaxVelocity;
        }
        else
        {
            _forwardSpeed = Input.GetAxis("Vertical") * _forwardMaxSpeed;
            _rotationVelocity = Input.GetAxis("Horizontal") * _rotationMaxVelocity;
        }
    }

    private void UpdateMovement()
    {
        var moveDirection = transform.forward*_forwardSpeed*Time.deltaTime;

        // Reset fall velocity if grounded
        if (_characterController.isGrounded)
            _fallVelocity = 0;

        // Gravity application
        _fallVelocity += _fallVelocityMax*Time.deltaTime;
        _fallVelocity = Mathf.Min(_fallVelocity, _fallVelocityMax);
        moveDirection.y -= _fallVelocity*Time.deltaTime;

        // Movement update
        _characterController.Move(moveDirection);
        transform.Rotate(Vector3.up*_rotationVelocity*Time.deltaTime);

        // Death and "respawn"
        if (transform.position.y < _yDeathValue)
            MoveToRandomSpawnPoint();
    }

    public void MoveToRandomSpawnPoint()
    {
        var spawns = FindObjectsOfType<NetworkStartPosition>();
        var spawn = spawns[Random.Range(0, spawns.Length)];
        transform.position = spawn.transform.position;
    }

    public void OnWeaponChange(string name)
    {
        WeaponSpriteName = name;

        SetWeapon(name);
    }

    public void OnFlagChange(string color)
    {
        FlagColor = color;

        SetFlagColor(FlagColor);
    }

    public void SetWeapon(string name)
    {
        if (isServer)
        {
            WeaponSpriteName = name;
        }

        // Load the weapon (inefficient, use a lookup table in production)
        Weapon.sprite = Resources.LoadAll<Sprite>("Textures/tut_game")
            .FirstOrDefault(s => s.name == name);
    }

    public void SetFlagColor(string color)
    {
        if (string.IsNullOrEmpty(color))
        {
            // Hide the flag
            Flag.transform.parent.gameObject.SetActive(false);
            return;
        }

        // Display the flag
        Flag.transform.parent.gameObject.SetActive(true);

        // Inefficient, but works for the demo
        Flag.GetComponent<MeshRenderer>().material.color = BmHelper.HexToColor(color);
    }

    public void OnCoinsChange(int coins)
    {
        if (!isLocalPlayer)
            return;

        // Update value on client
        Coins = coins;

        if (CoinsChanged != null)
            CoinsChanged.Invoke();
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (!isServer)
            return;

        var coin = collider.GetComponent<MiniCoin>();
        if (coin != null)
        {
            Coins++;

            // Invoke event on server
            if (CoinsChanged != null)
                CoinsChanged.Invoke();
        }
        
    }

    private void OnDestroy()
    {
        // Cleanup the name object
        if (_nameObject != null)
            Destroy(_nameObject);
    }

    public IEnumerator DisplayName()
    {
        // Create a player name
        _nameObject = Instantiate(NamePrefab).GetComponent<Text>();
        _nameObject.text = Name ?? ".";
        _nameObject.transform.SetParent(FindObjectOfType<Canvas>().transform);

        while (true)
        {
            if ((_nameObject.text != Name) && (Name != null))
                _nameObject.text = Name;

            // While we're still "online"
            _nameObject.transform.position = RectTransformUtility
                                                .WorldToScreenPoint(Camera.main, NameTransform.position) + Vector2.up*30;

            yield return 0;
        }
    }
}