using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Rehber kaplamasi. Iki modu var:
///
///  SPOTLIGHT - ekrani karartir ama hedefin uzerinde bir DELIK birakir.
///              Oyuncu sadece o hedefe dokunabilir, ok isareti oraya bakar.
///              Delik dort karartma parcasiyla yapiliyor (ust/alt/sol/sag);
///              shader gerekmiyor ve her dikdortgen hedefle calisiyor.
///
///  MODAL     - ekranin tamami kararir, ortada butonlu bir kart durur.
///              "Hos geldin" ve "prestij nedir" gibi anlatimlar icin.
///
/// Kaplamanin kendi Canvas'i var (sortingOrder yuksek), o yuzden her seyin
/// ustunde ciziliyor. Delige denk gelen yerde kendi grafigi olmadigi icin
/// dokunus alttaki asil Canvas'a gecip hedefe ulasiyor.
/// </summary>
public class TutorialOverlay : MonoBehaviour
{
    [Header("Kok")]
    public RectTransform root;

    [Header("Karartma parcalari")]
    public RectTransform dimTop, dimBottom, dimLeft, dimRight;

    [Tooltip("Delik ile karanlik arasindaki yumusak gecis (9-slice, ortasi seffaf).")]
    public RectTransform softHole;

    [Header("Isaret ve kart")]
    public RectTransform arrow;
    public RectTransform card;
    public TextMeshProUGUI title;
    public TextMeshProUGUI body;
    public Button button;
    public TextMeshProUGUI buttonText;

    [Header("Ayarlar")]
    [Tooltip("Hedefin cevresinde birakilacak bosluk.")]
    public float padding = 26f;
    [Tooltip("Ok ile hedef arasindaki mesafe.")]
    public float arrowGap = 22f;
    [Tooltip("Delik kenarindaki yumusama bandinin genisligi. ui_soft_hole'un 9-slice kenari 96px, esit olmali.")]
    public float feather = 96f;

    [Tooltip("Kart govde yazisinin punto boyutu. Kart yaziya gore uzuyor, yazi kucultulmuyor.")]
    public float bodyFontSize = 34f;
    [Tooltip("Ok ile kart arasinda birakilacak bosluk.")]
    public float cardGap = 20f;

    // Kart ic olculeri (sahnedeki tut_body / tut_btn yerlesimiyle ayni olmali)
    const float BODY_TOP  = 110f;   // kartin ustunden govde yazisinin ustune
    const float BODY_PAD  = 36f;    // govdenin altindan kartin altina (butonsuz)
    const float BTN_SPACE = 174f;   // buton (110) + alt bosluk (30) + ara (34)

    System.Action onButton;

    void Awake()
    {
        if (button != null) button.onClick.AddListener(() =>
        {
            var cb = onButton; onButton = null;
            if (cb != null) cb();
        });
        HideNow();
    }

    void HideNow()
    {
        if (root != null) root.gameObject.SetActive(false);
    }

    public bool IsVisible => root != null && root.gameObject.activeSelf;

    public void Hide()
    {
        if (root == null || !root.gameObject.activeSelf) return;
        if (card != null) card.DOKill();
        if (arrow != null) arrow.DOKill();
        root.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------ modal

    public void ShowModal(string t, string b, string btn, System.Action onClick)
    {
        if (root == null) return;
        root.gameObject.SetActive(true);
        onButton = onClick;

        // Tum ekrani karart: ust parca her seyi kaplasin, digerleri sifirlansin
        Vector2 full = root.rect.size;
        Set(dimTop,    Vector2.zero, full);
        Set(dimBottom, Vector2.zero, Vector2.zero);
        Set(dimLeft,   Vector2.zero, Vector2.zero);
        Set(dimRight,  Vector2.zero, Vector2.zero);
        if (softHole != null) softHole.gameObject.SetActive(false);

        if (arrow != null) arrow.gameObject.SetActive(false);
        if (button != null) button.gameObject.SetActive(true);
        if (buttonText != null) buttonText.text = btn;

        float halfH = root.rect.height * 0.5f;
        float h = BuildCard(t, b, 820f, true, root.rect.height - 160f);
        if (card != null)
        {
            card.pivot = new Vector2(0.5f, 0.5f);
            float y = Mathf.Clamp(40f, -halfH + h * 0.5f + 24f, halfH - h * 0.5f - 24f);
            card.anchoredPosition = new Vector2(0f, y);
        }
        Pop();
    }

    // -------------------------------------------------------- spotlight

    public void ShowSpotlight(RectTransform target, string t, string b)
    {
        ShowSpotlight(target, t, b, null, null);
    }

    /// <summary>btnLabel bos degilse karta bir "gectim" butonu koyar.</summary>
    public void ShowSpotlight(RectTransform target, string t, string b, string btnLabel, System.Action onClick)
    {
        if (root == null) return;
        if (target == null) { Hide(); return; }

        root.gameObject.SetActive(true);
        onButton = null;

        Rect r = LocalRectOf(target);
        r.xMin -= padding; r.xMax += padding;
        r.yMin -= padding; r.yMax += padding;

        Vector2 half = root.rect.size * 0.5f;

        // Yumusak gecis: deligin cevresinde "feather" kadar bir band birakiyoruz.
        // O bandi 9-slice bir sprite dolduruyor (ortasi seffaf, disa dogru
        // karariyor). Duz parcalar da bandin DISINDAN basliyor, boylece
        // sert kenar hic olusmuyor.
        Rect f = new Rect(r.xMin - feather, r.yMin - feather,
                          r.width + feather * 2f, r.height + feather * 2f);

        if (softHole != null)
        {
            softHole.gameObject.SetActive(true);
            softHole.anchoredPosition = f.center;
            softHole.sizeDelta = f.size;
        }

        // Duz parcalar delige 1 birim TASAR. Yuvarlama yuzunden aralarinda
        // sac teli kalinliginda aydinlik bir cizgi kalmasin diye.
        const float SEAM = 1f;
        float ft = f.yMax - SEAM, fb = f.yMin + SEAM;
        float fl = f.xMin + SEAM, fr = f.xMax - SEAM;

        Set(dimTop,    new Vector2(0f, (ft + half.y) * 0.5f),
                       new Vector2(half.x * 2f, Mathf.Max(0f, half.y - ft)));
        Set(dimBottom, new Vector2(0f, (fb - half.y) * 0.5f),
                       new Vector2(half.x * 2f, Mathf.Max(0f, fb + half.y)));
        Set(dimLeft,   new Vector2((fl - half.x) * 0.5f, f.center.y),
                       new Vector2(Mathf.Max(0f, fl + half.x), f.height));
        Set(dimRight,  new Vector2((fr + half.x) * 0.5f, f.center.y),
                       new Vector2(Mathf.Max(0f, half.x - fr), f.height));

        bool hasBtn = !string.IsNullOrEmpty(btnLabel);
        onButton = onClick;
        if (button != null) button.gameObject.SetActive(hasBtn);
        if (hasBtn && buttonText != null) buttonText.text = btnLabel;

        // Hedef ekranin alt yarisindaysa ok USTUNDE durup ASAGI bakar,
        // ust yarisindaysa ALTINDA durup YUKARI bakar.
        //
        // DIKKAT: icon_arrow sprite'i varsayilan halinde ASAGI bakiyor.
        // Yani "asagi baksin" = 0 derece, "yukari baksin" = 180 derece.
        // Pivot her zaman ortada; donunce yer degistirmesin diye okun
        // ucunu deligin kenarina getirmek icin yarim boy kadar kaydiriyoruz.
        bool targetLow = r.center.y < 0f;
        float ayTip = targetLow ? f.yMax + arrowGap : f.yMin - arrowGap;

        if (arrow != null)
        {
            arrow.gameObject.SetActive(true);
            arrow.pivot = new Vector2(0.5f, 0.5f);
            arrow.localEulerAngles = new Vector3(0f, 0f, targetLow ? 0f : 180f);

            float halfArrow = arrow.rect.height * 0.5f;
            float ay = targetLow ? ayTip + halfArrow : ayTip - halfArrow;

            arrow.anchoredPosition = new Vector2(Mathf.Clamp(r.center.x, -half.x + 80f, half.x - 80f), ay);
            arrow.DOKill();
            arrow.localScale = Vector3.one;

            // Nabiz gibi HEDEFE dogru gidip gelsin.
            float dir = targetLow ? -1f : 1f;
            arrow.DOAnchorPosY(ay + 22f * dir, 0.55f)
                 .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
        }

        // Kart okun DISINDA baslar; ok hicbir zaman kartin altinda kalmaz.
        float arrowH = arrow != null ? arrow.rect.height : 0f;
        float roomForCard = targetLow
            ? half.y - (ayTip + arrowH) - 24f
            : (ayTip - arrowH) + half.y - 24f;

        float h = BuildCard(t, b, 760f, hasBtn, Mathf.Max(roomForCard, 260f));

        if (card != null)
        {
            card.pivot = new Vector2(0.5f, targetLow ? 0f : 1f);   // hedefe bakan kenar sabit
            float cy = targetLow ? ayTip + arrowH + cardGap
                                 : ayTip - arrowH - cardGap;
            if (targetLow) cy = Mathf.Min(cy,  half.y - h - 24f);
            else           cy = Mathf.Max(cy, -half.y + h + 24f);
            card.anchoredPosition = new Vector2(0f, cy);
        }

        Pop();
    }

    // ----------------------------------------------------------- yardim

    /// <summary>Kart acikken sadece govde yazisini tazeler (canli sayac icin).</summary>
    public void SetBody(string b)
    {
        if (body != null) body.text = b;
    }

    void Fill(string t, string b)
    {
        if (title != null) { title.text = t; title.gameObject.SetActive(!string.IsNullOrEmpty(t)); }
        if (body  != null) body.text = b;
    }

    /// <summary>
    /// Karti YAZIYA GORE buyutur. Onceden kart sabit yukseklikteydi, uzun
    /// metinlerde TMP puntoyu kucultuyordu ve yazi okunmaz hale geliyordu.
    /// Artik punto sabit, kart uzuyor. Sadece ekrana sigmayan uc durumda
    /// (maxHeight) son care olarak otomatik kucultme devreye giriyor.
    /// Kartin son yuksekligini dondurur.
    /// </summary>
    float BuildCard(string t, string b, float width, bool hasBtn, float maxHeight)
    {
        Fill(t, b);

        float chrome = BODY_TOP + (hasBtn ? BTN_SPACE : BODY_PAD);
        float bodyH  = 90f;

        if (body != null)
        {
            body.enableAutoSizing = false;
            body.fontSize = bodyFontSize;

            Vector2 pref = body.GetPreferredValues(b ?? string.Empty, width - 72f, 0f);
            bodyH = Mathf.Max(80f, pref.y + 12f);

            float room = maxHeight - chrome;
            if (room > 80f && bodyH > room)
            {
                bodyH = room;
                body.enableAutoSizing = true;
                body.fontSizeMin = 22f;
                body.fontSizeMax = bodyFontSize;
            }

            body.rectTransform.sizeDelta = new Vector2(-72f, bodyH);
        }

        float h = chrome + bodyH;
        if (card != null) card.sizeDelta = new Vector2(width, h);
        return h;
    }

    void Pop()
    {
        if (card == null) return;
        card.DOKill();
        card.localScale = Vector3.one * 0.88f;
        card.DOScale(Vector3.one, 0.32f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    void Set(RectTransform rt, Vector2 pos, Vector2 size)
    {
        if (rt == null) return;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    /// <summary>Hedefin dikdortgenini kaplamanin yerel koordinatlarina cevirir.</summary>
    Rect LocalRectOf(RectTransform target)
    {
        Vector3[] c = new Vector3[4];
        target.GetWorldCorners(c);
        Vector2 mn = root.InverseTransformPoint(c[0]);
        Vector2 mx = mn;
        for (int i = 1; i < 4; i++)
        {
            Vector2 p = root.InverseTransformPoint(c[i]);
            mn = Vector2.Min(mn, p);
            mx = Vector2.Max(mx, p);
        }
        return new Rect(mn.x, mn.y, mx.x - mn.x, mx.y - mn.y);
    }

    void OnDestroy()
    {
        if (button != null) button.onClick.RemoveAllListeners();
        if (card  != null) card.DOKill();
        if (arrow != null) arrow.DOKill();
    }
}
