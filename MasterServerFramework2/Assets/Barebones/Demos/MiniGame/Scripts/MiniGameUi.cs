using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MiniGameUi : MonoBehaviour
{
    public Text Coins;

    public static MiniGameUi Instance;

    public Image AutoWalkBg;

    public bool IsAutoWalk { get; private set; }
    private Color _defaultWalkBtnColor;

    void Awake()
    {
        Instance = this;
    }

    public void ToggleAutoWalk()
    {
        // Save defualt color
        if (!IsAutoWalk)
            _defaultWalkBtnColor = AutoWalkBg.color;

        IsAutoWalk = !IsAutoWalk;

        AutoWalkBg.color = IsAutoWalk ? Color.red : _defaultWalkBtnColor;
    }

    public void OnPlayerSpawned(MiniPlayerController player)
    {
        player.CoinsChanged += () =>
        {
            Coins.text = player.Coins.ToString();
        };
    }
}
