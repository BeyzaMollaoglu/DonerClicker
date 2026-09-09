using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Basarim listesi paneli.
///
/// Satirlar sahnede degil, KODDA uretiliyor. 66 basarimin hepsini sahneye
/// koymak gereksiz agir olurdu ve her yeni basarimda sahneye dokunmak
/// gerekirdi; boylece achievements.json'a satir eklemek yetiyor.
///
/// Ayarlar paneliyle ayni davranis: cark yerine kupa butonu aciyor,
/// disariya dokununca kapaniyor.
/// </summary>
public class AchievementPanel : MonoBehaviour
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

    [Header("Sekme rozeti")]
    [Tooltip("Kupa butonunun uzerindeki sayac - yeni acilan basarim varsa gorunur.")]
    public GameObject badgeRoot;
    public TextMeshProUGUI badgeText;

    [Header("Gorunum")]
    public TMP_FontAsset fontAsset;
    [Tooltip("Satir zemini icin 9-slice yuvarlak kart sprite'i.")]
    public Sprite rowSprite;
    [Tooltip("Kategori ikonlari - sirasiyla: doner, alev, dokunus, asci, gelistirme, prestij, yildiz, ay, gizli.")]
    public Sprite[] categoryIcons;
    [Tooltip("Acilmis basarimin yanindaki tik. FONTTA TIK GLIFI YOK, sprite kullanmak zorundayiz.")]
    public Sprite checkIcon;

    /// <summary>Rehber kaplamasi acik panelin uzerine cizilmesin diye.</summary>
    public static bool IsOpen { get; private set; }

    readonly List<GameObject> rows = new List<GameObject>();
    int newSinceSeen;
    int lastCount = -1;

    // RENKLER
    // Eski hallerinde panel zemini ile satir arasindaki kontrast 1.02:1 idi
    // (yani satirlar gorunmuyordu) ve kilitli baslik kendi zeminine karsi
    // 1.70:1 kaliyordu. Asagidaki degerler olculerek secildi:
    // kilitli baslik 3.8:1, acik baslik 4.6:1, satir/panel ayrimi ~2:1.
    static readonly Color GOLD      = new Color32(0xF0, 0xB4, 0x41, 0xFF);
    static readonly Color GOLD_SOFT = new Color32(0xFF, 0xE9, 0xC4, 0xFF);   // acik satir basligi
    static readonly Color TXT_LOCK  = new Color32(0xA8, 0x94, 0x82, 0xFF);   // kilitli baslik
    static readonly Color DESC_ON   = new Color32(0xC4, 0xAE, 0x96, 0xFF);
    static readonly Color DESC_OFF  = new Color32(0x7D, 0x6E, 0x5E, 0xFF);
    static readonly Color ROW_ON    = new Color32(0x3D, 0x2A, 0x1A, 0xFF);
    static readonly Color ROW_OFF   = new Color32(0x2E, 0x21, 0x19, 0xFF);
    static readonly Color SUMMARY   = new Color32(0x4A, 0x35, 0x20, 0xFF);

    /// <summary>Her kategorinin kendi rengi - oyunun tek tonluluguna nefes actiriyor.</summary>
    static readonly Color32[] CAT_COL = {
        new Color32(0xF0,0xB4,0x41,0xFF),  // 0 doner   - altin
        new Color32(0xE4,0x64,0x3A,0xFF),  // 1 alev    - pul biber
        new Color32(0xEB,0xD3,0xA8,0xFF),  // 2 dokunus - krem
        new Color32(0x9F,0xC2,0x4B,0xFF),  // 3 asci    - yesil
        new Color32(0x6F,0xA8,0xD0,0xFF),  // 4 gelistirme - mavi
        new Color32(0xB9,0x8C,0xE0,0xFF),  // 5 prestij - mor
        new Color32(0xFF,0xD6,0x59,0xFF),  // 6 yildiz  - sari
        new Color32(0x7F,0xB0,0xC4,0xFF),  // 7 ay      - camgobegi
        new Color32(0x8A,0x7A,0x68,0xFF),  // 8 gizli   - sonuk
    };

    /// <summary>kind -> ikon/renk dizini.</summary>
    static int CatOf(int kind)
    {
        switch (kind)
        {
            case 0: case 1:  return 0;
            case 2:          return 1;
            case 3:          return 2;
            case 4: case 5: case 12: return 3;
            case 6:          return 4;
            case 7: case 8:  return 5;
            case 9:          return 6;
            case 10: case 11:return 7;
        }
        return 8;
    }

    void Start()
    {
        if (panel != null) panel.gameObject.SetActive(false);
        if (btn_open  != null) btn_open.onClick.AddListener(Open);
        if (btn_close != null) btn_close.onClick.AddListener(Close);

        AchievementManager.OnChanged += OnAchievementsChanged;
        LocalizationManager.OnLanguageChanged += OnLanguage;

        // Acilista listeyi KURMUYORUZ: 66 satir + yazilari ~200 obje eder ve
        // panel kapaliyken bunun hicbir faydasi yok. Ilk acilista kuruluyor.
        if (AchievementManager.Instance != null)
            lastCount = AchievementManager.Instance.UnlockedCount();
        RefreshBadge();
    }

    void OnDestroy()
    {
        AchievementManager.OnChanged -= OnAchievementsChanged;
        LocalizationManager.OnLanguageChanged -= OnLanguage;
    }

    /// <summary>Dil degistiyse sadece ACIK panel yeniden kurulur.</summary>
    void OnLanguage() { if (IsOpen) Rebuild(); }

    void OnAchievementsChanged()
    {
        var am = AchievementManager.Instance;
        if (am == null) return;
        int n = am.UnlockedCount();
        if (lastCount >= 0 && n > lastCount && !IsOpen) newSinceSeen += n - lastCount;
        lastCount = n;

        if (IsOpen) Rebuild(); else RefreshBadge();
    }

    // ------------------------------------------------------------- ac / kapat

    public void Open()
    {
        if (panel == null) return;
        IsOpen = true;
        ModalGuard.Enter(sideButtons);
        newSinceSeen = 0;
        RefreshBadge();
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

    void RefreshBadge()
    {
        if (badgeRoot != null) badgeRoot.SetActive(newSinceSeen > 0);
        if (badgeText != null) badgeText.text = newSinceSeen > 99 ? "99+" : newSinceSeen.ToString();
    }

    string T(string k)
    {
        return LocalizationManager.Instance != null
             ? LocalizationManager.Instance.GetLocalizedValue(k) : k;
    }

    // ------------------------------------------------------------------ liste

    void Rebuild()
    {
        var am = AchievementManager.Instance;
        if (am == null || content == null) return;

        foreach (var r in rows) if (r != null) Destroy(r);
        rows.Clear();

        if (title != null)
            title.text = string.Format(T("ach_title"), am.UnlockedCount(), am.Total());

        AddSummary(am);

        foreach (var a in am.list)
        {
            // Gizli basarimlar acilana kadar "???" durur - kesfedilmesi keyif.
            bool masked = a.hidden == 1 && !a.unlocked;
            AddRow(a,
                   masked ? "???" : am.Name(a),
                   masked ? T("ach_hidden_desc") : am.Desc(a),
                   masked);
        }
    }

    void AddSummary(AchievementManager am)
    {
        var go = Row(136f, SUMMARY);
        var t = Label(go.transform, 38f, GOLD, TextAlignmentOptions.Center);
        Stretch(t.rectTransform, 16f);
        t.text = string.Format(T("ach_fame"),
                               AchievementManager.FamePercent().ToString("0.#"),
                               am.ScoringCount());
    }

    void AddRow(AchievementItem a, string name, string desc, bool masked)
    {
        bool unlocked = a.unlocked;
        int  cat = masked ? 8 : CatOf(a.kind);

        var go = Row(152f, unlocked ? ROW_ON : ROW_OFF);

        // Acik olanlarda sol kenarda kategori renginde bir seri:
        // listede goz gezdirirken neyi kazandigin bir bakista belli olsun.
        if (unlocked)
        {
            var bar = new GameObject("bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.transform.SetParent(go.transform, false);
            var brt = (RectTransform)bar.transform;
            brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 0.5f);
            brt.sizeDelta = new Vector2(10f, -28f);
            brt.anchoredPosition = new Vector2(10f, 0f);
            bar.GetComponent<Image>().color = CAT_COL[cat];
            bar.GetComponent<Image>().raycastTarget = false;
        }

        // Kategori ikonu
        if (categoryIcons != null && cat < categoryIcons.Length && categoryIcons[cat] != null)
        {
            var ico = new GameObject("icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ico.transform.SetParent(go.transform, false);
            var irt = (RectTransform)ico.transform;
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.sizeDelta = new Vector2(84f, 84f);
            irt.anchoredPosition = new Vector2(34f, 0f);
            var img = ico.GetComponent<Image>();
            img.sprite = categoryIcons[cat];
            img.raycastTarget = false;
            // Kilitliyken ikon sonuk: satir okunur kalir ama kazanilmadigi bellidir.
            Color c = CAT_COL[cat];
            img.color = unlocked ? c : new Color(c.r * 0.42f, c.g * 0.42f, c.b * 0.42f, 0.85f);
        }

        float left = 140f;

        var nm = Label(go.transform, 46f, unlocked ? GOLD_SOFT : TXT_LOCK, TextAlignmentOptions.Left);
        var nrt = nm.rectTransform;
        nrt.anchorMin = new Vector2(0f, 1f); nrt.anchorMax = new Vector2(1f, 1f);
        nrt.pivot = new Vector2(0.5f, 1f);
        nrt.offsetMin = new Vector2(left, 0f); nrt.offsetMax = new Vector2(-96f, 0f);
        nrt.sizeDelta = new Vector2(nrt.sizeDelta.x, 52f);
        nrt.anchoredPosition = new Vector2(nrt.anchoredPosition.x, -16f);
        nm.text = name;

        var ds = Label(go.transform, 32f, unlocked ? DESC_ON : DESC_OFF, TextAlignmentOptions.TopLeft);
        var drt = ds.rectTransform;
        drt.anchorMin = new Vector2(0f, 0f); drt.anchorMax = new Vector2(1f, 1f);
        drt.pivot = new Vector2(0.5f, 0.5f);
        drt.offsetMin = new Vector2(left, 12f); drt.offsetMax = new Vector2(-96f, -72f);
        ds.text = desc;

        // Tik: yazi degil SPRITE. Fredoka'da tik glifi yok, yazi olarak
        // koyarsak eksik-karakter kutusu cikiyor.
        if (unlocked && checkIcon != null)
        {
            var ok = new GameObject("check", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ok.transform.SetParent(go.transform, false);
            var ort = (RectTransform)ok.transform;
            ort.anchorMin = new Vector2(1f, 0.5f); ort.anchorMax = new Vector2(1f, 0.5f);
            ort.pivot = new Vector2(1f, 0.5f);
            ort.sizeDelta = new Vector2(56f, 56f);
            ort.anchoredPosition = new Vector2(-28f, 0f);
            var oi = ok.GetComponent<Image>();
            oi.sprite = checkIcon; oi.color = GOLD; oi.raycastTarget = false;
        }
    }

    GameObject Row(float h, Color fill)
    {
        var go = new GameObject("ach_row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(content, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(0f, h);

        var img = go.GetComponent<Image>();
        img.color = fill;
        img.raycastTarget = false;
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
        t.fontSize = size;
        t.color = col;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt, float pad)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
    }
}
