using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    static readonly Color BorderOn  = new Color(0.960f, 0.772f, 0.258f);
    static readonly Color BorderOff = new Color(0.313f, 0.235f, 0.188f);
    static readonly Color FillOn    = new Color(0.290f, 0.180f, 0.109f);
    static readonly Color FillOff   = new Color(0.203f, 0.141f, 0.109f);
    static readonly Color IconOn    = new Color(0.909f, 0.384f, 0.172f);
    static readonly Color IconOff   = new Color(0.352f, 0.258f, 0.196f);
    static readonly Color TextOn    = new Color(1.000f, 0.952f, 0.878f);
    static readonly Color TextOff   = new Color(0.588f, 0.486f, 0.411f);
    static readonly Color SubColor  = new Color(0.588f, 0.486f, 0.411f);

    int lastState = -1;   // -1 hic ayarlanmadi, 0 alinamaz, 1 alinabilir

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
        lastState = state;

        if (imgBorder != null) imgBorder.color = ok ? BorderOn : BorderOff;
        if (imgFill   != null) imgFill.color   = ok ? FillOn   : FillOff;
        if (imgIcon   != null) imgIcon.color   = ok ? IconOn   : IconOff;
        if (txtName   != null) txtName.color   = ok ? TextOn   : TextOff;
        if (txtDetail != null) txtDetail.color = ok ? TextOn   : TextOff;
        if (txtPrice  != null) txtPrice.color  = ok ? BorderOn : TextOff;
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
