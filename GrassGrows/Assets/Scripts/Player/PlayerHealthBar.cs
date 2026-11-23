
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    public Image[] pips;                          // Initial hearts, left to right
    public Color fullColor = Color.white;
    public Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);

    private List<Image> allPips = new List<Image>();

    private void Awake()
    {
        // Copy the Inspector-assigned hearts into our expandable list.
        foreach (Image pip in pips)
        {
            if (pip != null)
                allPips.Add(pip);
        }
    }

    private void Start()
    {
        var player = GameManager.instance.player;

        player.onHealthChanged.AddListener(SetHealth);

        SetHealth(player.Health, player.maxHealth);
    }

    public void SetHealth(int current, int max)
    {
        // Create additional hearts if maximum health has increased.
        while (allPips.Count < max)
        {
            if (allPips.Count == 0)
            {
                Debug.LogWarning("PlayerHealthBar needs at least one heart assigned.");
                return;
            }

            Image template = allPips[0];

            GameObject newHeart = Instantiate(
                template.gameObject,
                template.transform.parent,
                false
            );

            Image newPip = newHeart.GetComponent<Image>();

            allPips.Add(newPip);
        }

        // Update visibility and color of every heart.
        for (int i = 0; i < allPips.Count; i++)
        {
            allPips[i].gameObject.SetActive(i < max);

            if (i < max)
            {
                allPips[i].color = i < current
                    ? fullColor
                    : emptyColor;
            }
        }
    }
}
