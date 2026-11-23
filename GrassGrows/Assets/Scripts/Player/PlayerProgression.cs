using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }

    [Header("XP")]
    public Slider xpSlider;
    public int baseXpToLevel = 30;
    public int xpGrowthPerLevel = 15;
    public UnityEvent<int> onLevelChanged;   // hook up a level text if you want one

    [Header("Upgrades")]
    public Upgrade[] upgradePool;
    public UpgradeSelectionUI selectionUI;
    public Animator cardUIAnimator;

    public int Level { get; private set; } = 1;
    public int XP { get; private set; }
    public int XpToNext => baseXpToLevel + (Level - 1) * xpGrowthPerLevel;

    private PlayerController player;
    private readonly Dictionary<Upgrade, int> stacks = new Dictionary<Upgrade, int>();
    private int pendingLevelUps;
    private bool choosing;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;   // in case the scene was reloaded mid-selection
    }

    private void Start()
    {
        player = GameManager.instance.player;
        if (xpSlider) { xpSlider.minValue = 0f; xpSlider.maxValue = 1f; }
        Refresh();
        onLevelChanged?.Invoke(Level);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            AddXP(XpToNext);
        }
    }

    public void AddXP(int amount)
    {
        if (player.IsDead) return;

        XP += amount;
        while (XP >= XpToNext)
        {
            XP -= XpToNext;
            Level++;
            pendingLevelUps++;
            onLevelChanged?.Invoke(Level);
        }
        Refresh();

        if (pendingLevelUps > 0 && !choosing)
        {
            ShowChoices();
            cardUIAnimator.SetTrigger("Open");
        }
    }

    private void Refresh()
    {
        if (xpSlider) xpSlider.value = (float)XP / XpToNext;
    }

    private int Stacks(Upgrade u) => stacks.TryGetValue(u, out var s) ? s : 0;

    private void ShowChoices()
    {
        // Everything not yet maxed out, then pick up to 3 distinct at random
        var options = new List<Upgrade>();
        foreach (var u in upgradePool)
            if (u && Stacks(u) < u.maxStacks) options.Add(u);

        var picks = new List<Upgrade>();
        while (picks.Count < 3 && options.Count > 0)
        {
            int i = Random.Range(0, options.Count);
            picks.Add(options[i]);
            options.RemoveAt(i);
        }

        if (picks.Count == 0)   // everything maxed
        {
            pendingLevelUps = 0;
            Time.timeScale = 1f;
            return;
        }

        choosing = true;
        Time.timeScale = 0f;
        selectionUI.Show(picks, Choose);
    }

    public int CostFromFraction(float fraction) => Mathf.CeilToInt(XpToNext * fraction);

    public bool TrySpendXP(int amount)
    {
        if (XP < amount) return false;
        XP -= amount;
        Refresh();
        return true;
    }

    private void Choose(Upgrade u)
    {
        int stack = Stacks(u) + 1;
        stacks[u] = stack;
        u.Apply(player, stack);

        pendingLevelUps--;
        choosing = false;

        if (pendingLevelUps > 0)
        {
            ShowChoices();   // leveled up more than once
        }
        else
        {
            StartCoroutine(CloseCards());
        }
    }

    private IEnumerator CloseCards()
    {
        cardUIAnimator.SetTrigger("Close");

        yield return new WaitForSecondsRealtime(0.5f);

        selectionUI.Hide();
        Time.timeScale = 1f;


    }
}