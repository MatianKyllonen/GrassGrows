using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UpgradeSelectionUI : MonoBehaviour
{
    [Serializable]
    public class Card
    {
        public Button button;
        public TMP_Text title;
        public TMP_Text description;
        public Image icon;
    }

    public GameObject panel;
    public Card[] cards;   // 3 cards

    private List<Upgrade> current = new List<Upgrade>();
    private Action<Upgrade> onPick;

    private void Awake() => panel.SetActive(false);

    public void Show(List<Upgrade> options, Action<Upgrade> pick)
    {
        current = options;
        onPick = pick;
        panel.SetActive(true);

        for (int i = 0; i < cards.Length; i++)
        {
            bool has = i < options.Count;
            cards[i].button.gameObject.SetActive(has);
            if (!has) continue;

            var u = options[i];
            cards[i].title.text = u.title;
            cards[i].description.text = u.description;
            if (cards[i].icon) { cards[i].icon.sprite = u.icon; cards[i].icon.enabled = u.icon != null; }

            int index = i;
            cards[i].button.onClick.RemoveAllListeners();
            cards[i].button.onClick.AddListener(() => Pick(index));
        }

        if (EventSystem.current)   // so gamepad / keyboard navigation works
            EventSystem.current.SetSelectedGameObject(cards[0].button.gameObject);
    }

    public void Hide() => panel.SetActive(false);

    private void Update()
    {
        if (!panel.activeSelf) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) Pick(0);
        else if (kb.digit2Key.wasPressedThisFrame) Pick(1);
        else if (kb.digit3Key.wasPressedThisFrame) Pick(2);
    }

    private void Pick(int i)
    {
        if (i < current.Count) onPick?.Invoke(current[i]);
    }
}