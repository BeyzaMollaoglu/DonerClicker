using UnityEngine;

/// <summary>
/// Bu script'i Canvas'in ALTINDAKI bir container'a takarsin (Canvas'in kendisine DEGIL -
/// Canvas'in RectTransform'u Canvas component'i tarafindan surulur, elle degistirilemez).
/// Container'i cihazin guvenli alanina oturtur: centik, durum cubugu, jest cubugu.
/// Banner reklam eklediginde bannerHeightDp'yi doldur, icerik o kadar yukari itilir.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("Reklam Alani")]
    [Tooltip("AdMob banner yuksekligi (dp). Banner yokken 0 birak. Adaptive banner genelde 50-90 dp.")]
    public float bannerHeightDp = 0f;

    [Header("Ek Bosluk (dp)")]
    public float extraTopDp = 0f;
    public float extraBottomDp = 0f;

    RectTransform rt;
    Rect lastSafeArea;
    Vector2Int lastResolution;
    ScreenOrientation lastOrientation;
    float lastBanner = -1f;

    void Awake()  { rt = GetComponent<RectTransform>(); Apply(); }
    void OnEnable() { Apply(); }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea
            || Screen.width != lastResolution.x || Screen.height != lastResolution.y
            || Screen.orientation != lastOrientation
            || !Mathf.Approximately(bannerHeightDp, lastBanner))
            Apply();
    }

    void Apply()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (rt == null || Screen.width == 0 || Screen.height == 0) return;

        lastSafeArea    = Screen.safeArea;
        lastResolution  = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;
        lastBanner      = bannerHeightDp;

        // dp -> piksel. Screen.dpi bazi cihazlarda 0 doner, o zaman 3x varsayalim.
        float pxPerDp = Screen.dpi > 1f ? Screen.dpi / 160f : 3f;

        Rect area = lastSafeArea;
        area.yMin += (bannerHeightDp + extraBottomDp) * pxPerDp;
        area.yMax -= extraTopDp * pxPerDp;

        if (area.width <= 0f || area.height <= 0f) return;

        Vector2 min = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        Vector2 max = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);

        // bozuk veriye karsi koruma - ekrani yok etmesin
        if (min.x < 0f || min.y < 0f || max.x > 1f || max.y > 1f || min.x >= max.x || min.y >= max.y)
            return;

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
