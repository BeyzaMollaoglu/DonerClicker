using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Isciler panelindeki x1 / x10 / x100 secicisi.
/// Secim WorkerManager.buyAmount'a yazilir, kartlar aninda tazelenir.
/// </summary>
public class BuyAmountSelector : MonoBehaviour
{
    [Tooltip("Butonlar - sirasiyla amounts dizisine karsilik gelir.")]
    public Button[] buttons;
    [Tooltip("Butonlarin arka plan Image'lari (renk degisecek).")]
    public Image[] backgrounds;
    [Tooltip("Buton yazilari (renk degisecek).")]
    public TextMeshProUGUI[] labels;
    public int[] amounts = { 1, 10, 100 };

    static readonly Color BgOn    = new Color(0.941f, 0.706f, 0.255f);
    static readonly Color BgOff   = new Color(0.165f, 0.102f, 0.071f);
    static readonly Color TextOn  = new Color(0.090f, 0.051f, 0.031f);
    static readonly Color TextOff = new Color(0.639f, 0.541f, 0.431f);

    int selected = 0;

    void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            if (buttons[i] != null) buttons[i].onClick.AddListener(() => Select(idx));
        }
        Select(0);
    }

    public void Select(int index)
    {
        if (index < 0 || index >= amounts.Length) return;
        selected = index;

        if (WorkerManager.Instance != null)
            WorkerManager.Instance.SetBuyAmount(amounts[index]);

        for (int i = 0; i < amounts.Length; i++)
        {
            bool on = (i == selected);
            if (backgrounds != null && i < backgrounds.Length && backgrounds[i] != null)
                backgrounds[i].color = on ? BgOn : BgOff;
            if (labels != null && i < labels.Length && labels[i] != null)
                labels[i].color = on ? TextOn : TextOff;
        }
    }

    void OnDestroy()
    {
        foreach (var b in buttons) if (b != null) b.onClick.RemoveAllListeners();
    }
}
