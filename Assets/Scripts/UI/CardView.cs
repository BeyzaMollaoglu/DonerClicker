using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Liste kartinin gorunumu. Manager'lar metin yazmak yerine bu component'i cagirir.
/// Boylece kart duzeni degistiginde manager kodlarina dokunmak gerekmez.
/// </summary>
public class CardView : MonoBehaviour
{
    [Header("Gorseller")]
    public Image imgBorder;      // kok - cerceve rengi
    public Image imgFill;        // kart ic zemini
    public Image imgIcon;        // ikon (simdilik yer tutucu)
    public Image imgPriceBadge;  // fiyat rozeti zemini

    [Header("Metinler")]
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtSub;     // "Seviye 4" / "Guc: x2"
    public TextMeshProUGUI txtDetail;  // "0,4/sn > 0,5/sn"
    public TextMeshProUGUI txtPrice;

    static readonly Color BorderOn  = new Color(0.941f, 0.706f, 0.255f);   // #F0B441 altin
    static readonly Color BorderOff = new Color(0.306f, 0.200f, 0.125f);   // #4E3320 sonuk cerceve
    static readonly Color FillOn    = new Color(0.239f, 0.165f, 0.102f);   // #3D2A1A - basarim panelindeki acik satirla ayni
    static readonly Color FillOff   = new Color(0.180f, 0.129f, 0.098f);   // #2E2119 - basarim panelindeki kilitli satirla ayni
    static readonly Color IconOn    = new Color(0.851f, 0.380f, 0.169f);   // #D9612B ates
    static readonly Color IconOff   = new Color(0.298f, 0.204f, 0.141f);   // #4C3424
    static readonly Color TextOn    = new Color(1.000f, 0.914f, 0.769f);   // #FFE9C4 krem
    static readonly Color TextOff   = new Color(0.659f, 0.580f, 0.510f);   // #A89482
    static readonly Color SubColor  = new Color(0.639f, 0.541f, 0.431f);

    int lastState = -1;   // -1 hic ayarlanmadi, 0 alinamaz, 1 alinabilir

    /// <summary>
    /// Kartin ikonunu ayarlar. Renk SetAffordable tarafindan yonetiliyor,
    /// burada sadece sprite degisiyor.
    /// </summary>
    public void SetIcon(Sprite icon)
    {
        if (imgIcon == null) return;
        imgIcon.sprite = icon;
        imgIcon.enabled = icon != null;
    }

    public void Set(string title, string sub, string detail, string price)
    {
        if (txtName   != null) txtName.text   = title;
        if (txtSub    != null) { txtSub.text = sub; txtSub.color = SubColor; }
        if (txtDetail != null) txtDetail.text = detail;
        if (txtPrice  != null) txtPrice.text  = price;
    }

    /// <summary>Durum degismediyse hicbir sey yapmaz - her karede cagrilabilir.</summary>
    public void SetAffordable(bool ok)
    {
        int state = ok ? 1 : 0;
        if (state == lastState) return;

        // Sadece "alinamaz -> alinabilir" gecisinde yan. lastState -1 iken
        // (kart yeni dogdu) yanmamali, yoksa panel her acildiginda toplu yanip soner.
        bool justBecameAffordable = ok && lastState == 0;
        lastState = state;

        if (imgBorder != null) imgBorder.color = ok ? BorderOn : BorderOff;
        if (imgFill   != null) imgFill.color   = ok ? FillOn   : FillOff;
        if (imgIcon   != null) imgIcon.color   = ok ? IconOn   : IconOff;
        if (txtName   != null) txtName.color   = ok ? TextOn   : TextOff;
        if (txtDetail != null) txtDetail.color = ok ? TextOn   : TextOff;
        if (txtPrice  != null) txtPrice.color  = ok ? BorderOn : TextOff;

        if (justBecameAffordable) Pulse();
    }

    /// <summary>
    /// "Artik bunu alabilirsin" bildirimi. Layout Group scale'i yok saydigi icin
    /// (ChildScaleWidth/Height = 0) liste kaymaz, sadece kart hafifce zipla.
    /// </summary>
    void Pulse()
    {
        RectTransform rt = transform as RectTransform;
        if (rt == null) return;
        rt.DOKill();
        rt.localScale = Vector3.one;
        rt.DOPunchScale(Vector3.one * 0.05f, 0.45f, 7, 0.7f)
          .OnComplete(() => { if (rt != null) rt.localScale = Vector3.one; });
    }

    void OnDestroy()
    {
        RectTransform rt = transform as RectTransform;
        if (rt != null) rt.DOKill();
    }

    /// <summary>Satin alinmis geliştirme icin sonuk gorunum.</summary>
    public void SetPurchased()
    {
        lastState = -2;
        if (imgBorder != null) imgBorder.color = BorderOff;
        if (imgFill   != null) imgFill.color   = FillOff;
        if (imgIcon   != null) imgIcon.color   = IconOff;
        if (txtName   != null) txtName.color   = TextOff;
        if (txtDetail != null) txtDetail.color = TextOff;
        if (txtPrice  != null) txtPrice.color  = TextOff;
    }
}
