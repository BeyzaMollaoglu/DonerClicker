using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Sekmenin kosesindeki bildirim rozeti. Panel kapaliyken oyuncunun
/// yeni bir seyin acildigini fark etmesini saglar.
/// </summary>
public class TabBadge : MonoBehaviour
{
    public enum Source { Upgrades }

    [Tooltip("Sayiyi nereden alsin.")]
    public Source source = Source.Upgrades;

    [Tooltip("Rozetin kok objesi - sayi 0 iken kapatilir.")]
    public GameObject badgeRoot;
    public TextMeshProUGUI badgeText;
    [Tooltip("Belirdiginde sicrayacak obje (genelde badgeRoot'un RectTransform'u).")]
    public RectTransform pulseTarget;

    float tick;
    int   lastCount = -1;

    void Start()
    {
        if (badgeRoot != null) badgeRoot.SetActive(false);
        Apply();
    }

    void Update()
    {
        tick += Time.unscaledDeltaTime;
        if (tick < 0.3f) return;
        tick = 0f;
        Apply();
    }

    int Count()
    {
        if (source == Source.Upgrades && UpgradeManager.Instance != null)
            return UpgradeManager.Instance.NewCount();
        return 0;
    }

    void Apply()
    {
        int n = Count();
        if (n == lastCount) return;

        bool wasHidden = lastCount <= 0;
        lastCount = n;

        if (badgeRoot != null) badgeRoot.SetActive(n > 0);
        if (badgeText != null && n > 0) badgeText.text = n > 9 ? "9+" : n.ToString();

        // Sifirdan gorunur hale geldiyse dikkat cek
        if (n > 0 && wasHidden && pulseTarget != null)
        {
            pulseTarget.DOKill();
            pulseTarget.localScale = Vector3.one * 0.35f;
            pulseTarget.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
        }
    }

    void OnDestroy()
    {
        if (pulseTarget != null) pulseTarget.DOKill();
    }
}
