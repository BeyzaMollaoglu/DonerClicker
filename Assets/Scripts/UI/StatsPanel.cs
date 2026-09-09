using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Istatistik ekrani.
///
/// Butun sayaclar GameManager'da zaten tutuluyor (basarim sistemi icin
/// eklenmisti), burasi sadece onlari okuyup gosteriyor. Satirlar kodda
/// uretiliyor - yeni bir istatistik eklemek icin sahneye dokunmak gerekmiyor.
///
/// Ayarlar / basarimlar panelleriyle ayni davranis: kendi butonu aciyor,
/// disariya dokununca kapaniyor.
/// </summary>
public class StatsPanel : MonoBehaviour
{
    [Header("Panel")]
    public RectTransform panel;
    public RectTransform content;
    public TextMeshProUGUI title;

    [Header("Butonlar")]
    public Button btn_open;
    public Button btn_close;
    public Button btn_blocker;
    [Tooltip("Bu panel acikken tiklanamaz olacak diger yan butonlar (cark, kupa, grafik).")]
    public Button[] sideButtons;

    [Header("Gorunum")]
    public TMP_FontAsset fontAsset;
    public Sprite rowSprite;

    public static bool IsOpen { get; private set; }

    readonly List<GameObject> rows = new List<GameObject>();

    static readonly Color GOLD    = new Color32(0xF0, 0xB4, 0x41, 0xFF);
    static readonly Color LABEL   = new Color32(0xA8, 0x94, 0x82, 0xFF);
    static readonly Color VALUE   = new Color32(0xFF, 0xE9, 0xC4, 0xFF);
    static readonly Color ROW     = new Color32(0x2E, 0x21, 0x19, 0xFF);
    static readonly Color HEADROW = new Color32(0x4A, 0x35, 0x20, 0xFF);

    void Start()
    {
        if (panel != null) panel.gameObject.SetActive(false);
        if (btn_open  != null) btn_open.onClick.AddListener(Open);
        if (btn_close != null) btn_close.onClick.AddListener(Close);
        LocalizationManager.OnLanguageChanged += OnLanguage;
    }

    void OnDestroy() { LocalizationManager.OnLanguageChanged -= OnLanguage; }
    void OnLanguage() { if (IsOpen) Rebuild(); }

    public void Open()
    {
        if (panel == null) return;
        IsOpen = true;
        ModalGuard.Enter(sideButtons);
        Rebuild();
        panel.gameObject.SetActive(true);
        if (btn_blocker != null)
        {
            btn_blocker.gameObject.SetActive(true);
            btn_blocker.onClick.AddListener(Close);
        }
        panel.localScale = Vector3.one * 0.85f;
        panel.DOKill();
        panel.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
    }

    public void Close()
    {
        if (panel == null) return;
        IsOpen = false;
        ModalGuard.Exit(sideButtons);
        if (btn_blocker != null)
        {
            btn_blocker.onClick.RemoveListener(Close);
            btn_blocker.gameObject.SetActive(false);
        }
        panel.DOKill();
        panel.DOScale(Vector3.one * 0.85f, 0.2f).SetEase(Ease.InBack)
             .OnComplete(() => panel.gameObject.SetActive(false));
    }

    string T(string k)
    {
        return LocalizationManager.Instance != null
             ? LocalizationManager.Instance.GetLocalizedValue(k) : k;
    }

    /// <summary>Saniyeyi "3g 4s 12dk" gibi okunur hale getirir.</summary>
    static string Duration(double seconds)
    {
        if (seconds < 60) return ((int)seconds) + "sn";
        long t = (long)seconds;
        long g = t / 86400; t %= 86400;
        long s = t / 3600;  t %= 3600;
        long dk = t / 60;
        if (g > 0) return g + "g " + s + "s";
        if (s > 0) return s + "s " + dk + "dk";
        return dk + "dk";
    }

    void Rebuild()
    {
        foreach (var r in rows) if (r != null) Destroy(r);
        rows.Clear();

        var gm = GameManager.Instance;
        var wm = WorkerManager.Instance;
        var um = UpgradeManager.Instance;
        var am = AchievementManager.Instance;
        if (gm == null || content == null) return;

        if (title != null) title.text = T("stat_title");

        Head(T("stat_head_production"));
        Row(T("stat_lifetime"),   UIManager.FormatNumber(gm.lifetimeDoner));
        Row(T("stat_current"),    UIManager.FormatNumber(gm.totalDoner));
        Row(T("stat_run"),        UIManager.FormatNumber(gm.RunDoner));
        Row(T("stat_rate"),       UIManager.FormatNumber(gm.productionPerSecond) + T("stat_per_sec"));
        Row(T("stat_clickpower"), UIManager.FormatNumber(gm.clickPower));

        Head(T("stat_head_effort"));
        Row(T("stat_clicks"),   gm.totalClicks.ToString("N0"));
        Row(T("stat_playtime"), Duration(gm.playSeconds));
        Row(T("stat_offline"),  UIManager.FormatNumber(gm.offlineEarnedTotal));
        Row(T("stat_golden"),   gm.goldenCaught.ToString("N0"));

        Head(T("stat_head_shop"));
        int lv = wm != null ? wm.TotalLevels() : 0;
        Row(T("stat_worker_levels"), lv.ToString("N0"));
        Row(T("stat_upgrades"), Bought(um) + " / " + Total(um));

        Head(T("stat_head_prestige"));
        Row(T("stat_prestige_count"), gm.prestigeCount.ToString("N0"));
        Row(T("stat_coins_total"),    gm.PrestigeEarnedTotal.ToString("N0"));
        Row(T("stat_coins_purse"),    gm.prestigePoints.ToString("N0"));
        Row(T("stat_coins_spent"),    gm.prestigeSpent.ToString("N0"));

        if (am != null)
        {
            Head(T("stat_head_ach"));
            Row(T("stat_ach"),  am.UnlockedCount() + " / " + am.Total());
            Row(T("stat_fame"), "+%" + AchievementManager.FamePercent().ToString("0.#"));
        }
    }

    static int Bought(UpgradeManager um)
    {
        if (um == null || um.upgradeList == null) return 0;
        int n = 0; foreach (var u in um.upgradeList) if (u.isPurchased) n++;
        return n;
    }
    static int Total(UpgradeManager um)
    {
        return (um == null || um.upgradeList == null) ? 0 : um.upgradeList.Count;
    }

    // ------------------------------------------------------------------ cizim

    void Head(string text)
    {
        var go = Shell(74f, HEADROW);
        var t = Label(go.transform, 32f, GOLD, TextAlignmentOptions.Left);
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(30f, 0f); rt.offsetMax = new Vector2(-30f, 0f);
        t.text = text;
    }

    void Row(string label, string value)
    {
        var go = Shell(84f, ROW);

        var l = Label(go.transform, 36f, LABEL, TextAlignmentOptions.Left);
        var lrt = l.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = new Vector2(0.58f, 1f);
        lrt.offsetMin = new Vector2(30f, 0f); lrt.offsetMax = new Vector2(0f, 0f);
        l.text = label;

        var v = Label(go.transform, 40f, VALUE, TextAlignmentOptions.Right);
        var vrt = v.rectTransform;
        vrt.anchorMin = new Vector2(0.42f, 0f); vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(0f, 0f); vrt.offsetMax = new Vector2(-30f, 0f);
        v.text = value;
    }

    GameObject Shell(float h, Color fill)
    {
        var go = new GameObject("stat_row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(content, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(0f, h);
        var img = go.GetComponent<Image>();
        img.color = fill; img.raycastTarget = false;
        if (rowSprite != null) { img.sprite = rowSprite; img.type = Image.Type.Sliced; }
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = h; le.preferredHeight = h;
        rows.Add(go);
        return go;
    }

    TextMeshProUGUI Label(Transform parent, float size, Color col, TextAlignmentOptions align)
    {
        var go = new GameObject("txt", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) t.font = fontAsset;
        t.fontSize = size; t.color = col; t.alignment = align; t.raycastTarget = false;
        return t;
    }
}
