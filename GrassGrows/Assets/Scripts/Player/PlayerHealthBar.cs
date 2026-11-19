using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    public Image[] pips;                          // one per hit, left to right
    public Color fullColor = Color.white;
    public Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);

    private void Start()
    {
        var player = GameManager.instance.player;
        player.onHealthChanged.AddListener(SetHealth);
        SetHealth(player.Health, player.maxHealth);
    }

    public void SetHealth(int current, int max)
    {
        for (int i = 0; i < pips.Length; i++)
        {
            pips[i].gameObject.SetActive(i < max);
            pips[i].color = i < current ? fullColor : emptyColor;
        }
    }
}