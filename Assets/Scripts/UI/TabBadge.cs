using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Sekmenin kosesindeki bildirim rozeti. Panel kapaliyken oyuncunun
/// yeni bir seyin acildigini fark etmesini saglar.
/// </summary>
public class TabBadge : MonoBehaviour
{
    public enum Source
    {
        /// <summary>Kilidi acik + alinmamis + (yeni ya da parasi yeten) gelistirmeler.</summary>
        Upgrades,
        /// <summary>Hic alinmamis ama artik parasi yeten isciler.</summary>
        Workers,
        /// <summary>Reset atmaya degecek kadar Altin Masa birikti mi.</summary>
        Prestige,
        /// <summary>Alinmayi bekleyen bedava reklam hizlandirmasi (carpani yazar).</summary>
        AdBoost
    }

    [Tooltip("Sayiyi nereden alsin.")]
    public Source source = Source.Upgrades;

    [Tooltip("Prestij rozetinde sayi yerine bu yazar.")]
    public string prestigeLabel = "!";

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
        switch (source)
        {
            case Source.Upgrades:
                return UpgradeManager.Instance != null ? UpgradeManager.Instance.AlertCount() : 0;

            case Source.Workers:
                return WorkerManager.Instance != null ? WorkerManager.Instance.NewAffordableCount() : 0;

            case Source.AdBoost:
                // Odul HAZIRSA carpani goster ("3x"), boost zaten calisiyorsa gizle.
                // Uretim yokken gostermeyiz - hicbir seyin 3 kati yine hicbir sey,
                // oyuncuyu bos yere reklama yollamis oluruz.
                var g2 = GameManager.Instance;
                if (g2 == null || g2.productionPerSecond <= 0 || g2.BoostActive) return 0;
                return AdsManager.Instance != null ? (int)AdsManager.Instance.CurrentMultiplier : 0;

            case Source.Prestige:
                // Her zaman yanmasin: reset ancak simdiye kadar kazandiginin
                // en az %20'si kadar yeni puan getiriyorsa "degiyor" sayilir.
                var gm = GameManager.Instance;
                if (gm == null || gm.pendingPrestige <= 0) return 0;
                int earned = gm.prestigePoints + gm.prestigeSpent;
                double esik = System.Math.Max(1.0, earned * 0.20);
                return gm.pendingPrestige >= esik ? 1 : 0;
        }
        return 0;
    }

    void Apply()
    {
        int n = Count();
        if (n == lastCount) return;

        bool wasHidden = lastCount <= 0;
        lastCount = n;

        if (badgeRoot != null) badgeRoot.SetActive(n > 0);
        if (badgeText != null && n > 0)
        {
            if      (source == Source.Prestige) badgeText.text = prestigeLabel;
            else if (source == Source.AdBoost)  badgeText.text = "×" + n;
            else                                badgeText.text = n > 9 ? "9+" : n.ToString();
        }

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
