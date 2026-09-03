using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Oyuncunun ilerlemesini arka plandaki doner yagmurunun YOGUNLUGU ile gosterir.
/// Basta tek tuk ve agir agir duser; ilerledikce siklasir ve hizlanir.
///
/// Doneri buyutmeyi denedik, olmadi: siluet zaten ekranin ortasinda ve
/// birkac yuzde buyumesi fark edilmiyor, cok buyuyunce de duzeni bozuyor.
/// Yagmur ise cevresel - fark etmesen bile "ortam hizlandi" hissi veriyor.
/// </summary>
public class DonerProgression : MonoBehaviour
{
    [Header("Bagli efekt")]
    public UIRainEffect rain;

    [Header("Uretim araligi (log olcekli)")]
    [Tooltip("Bu uretimde yogunluk 0 (en seyrek).")]
    public double startProduction = 10.0;
    [Tooltip("Bu uretimde yogunluk 1 (en yogun).")]
    public double fullProduction  = 1e18;
    [Tooltip("Egri. 1 = dogrusal, buyuk = baslarda daha uzun sure seyrek kalir.")]
    public float curve = 1.8f;

    [Header("Etin rengi (hafif kizarma)")]
    public Image meatImage;
    public Color meatFullColor = new Color(1f, 0.88f, 0.76f);

    float tick;
    float age;
    float shown = -1f;

    void Start()
    {
        Apply(Target(), true);
    }

    // GameManager uretimi ilk KAREDEN SONRA hesapliyor (OfflineRoutine'de
    // "yield return null" var). Start'ta okursak uretim 0 gorunuyor ve
    // yumusak gecis 0'dan basladigi icin dogru yogunluga ulasmasi ~45 saniye
    // suruyordu. Ilk 2 saniye anlik oturt, sonrasinda yumusak gec.
    void Update()
    {
        age  += Time.deltaTime;
        tick += Time.deltaTime;
        if (tick < 0.5f) return;
        tick = 0f;
        Apply(Target(), age < 2f);
    }

    /// <summary>
    /// Uretimin logaritmasini 0..1 arasina esler. Uretim usel buyudugu icin
    /// dogrusal olcek ise yaramaz - log ile butun oyun boyunca duzgun ilerler.
    /// </summary>
    float Target()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.productionPerSecond <= 0) return 0f;

        double lo = System.Math.Log10(startProduction);
        double hi = System.Math.Log10(fullProduction);
        double v  = System.Math.Log10(gm.productionPerSecond);
        float linear = Mathf.Clamp01((float)((v - lo) / (hi - lo)));
        return Mathf.Pow(linear, curve);   // baslarda yavas artsin
    }

    void Apply(float t, bool instant)
    {
        // Ani ziplamasin, yumusak gecsin
        shown = (instant || shown < 0f) ? t : Mathf.MoveTowards(shown, t, 0.008f);

        if (rain != null) rain.SetIntensity(shown);

        if (meatImage != null)
            meatImage.color = Color.Lerp(Color.white, meatFullColor, shown);
    }
}
