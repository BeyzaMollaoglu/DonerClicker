using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Odullu reklam ve verdigi boost.
///
/// Su an gercek bir reklam SDK'si bagli DEGIL - WatchAd() odulu dogrudan veriyor
/// ki sistem Editor'de test edilebilsin. AdMob eklendiginde sadece
/// WatchAd() icindeki isaretli yeri degistirmen yeterli, geri kalan her sey ayni kalir.
/// </summary>
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    [Header("Odul")]
    [Tooltip("Pasif uretim kac katina ciksin.")]
    public double boostMultiplier = 3.0;
    [Tooltip("Boost kac saat sursun.")]
    public float boostHours = 4f;

    [Header("Arayuz")]
    public Button           btn_watch_ad;
    public TextMeshProUGUI  txt_ad_offer;
    public TextMeshProUGUI  txt_ad_status;

    float tick;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (btn_watch_ad != null) btn_watch_ad.onClick.AddListener(WatchAd);
        RefreshOffer();
        Refresh();
    }

    /// <summary>Prestij magazasindaki "Reklam Anlasmasi" dahil guncel odul.</summary>
    public double CurrentMultiplier => boostMultiplier + PrestigeManager.AdBoostAdd();

    /// <summary>Teklif metnini tazeler - prestij kalemi alindiginda cagrilir.</summary>
    public void RefreshOffer()
    {
        if (txt_ad_offer != null)
            txt_ad_offer.text = $"{boostHours:0.#} saat boyunca\n<color=#F0B441>{CurrentMultiplier:0.#}x pasif uretim</color>";
    }

    private void Update()
    {
        tick += Time.unscaledDeltaTime;
        if (tick < 1f) return;
        tick = 0f;
        Refresh();
    }

    public void WatchAd()
    {
        // ================= GERCEK REKLAM BURAYA =================
        // AdMob ornegi:
        //   if (rewardedAd != null && rewardedAd.CanShowAd())
        //       rewardedAd.Show((Reward r) => GrantReward());
        //   else
        //       txt_ad_status.text = "Reklam hazir degil, birazdan tekrar dene.";
        //   return;
        // ========================================================

        GrantReward();   // SDK yokken odulu dogrudan ver (test)
    }

    private void GrantReward()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.GrantBoost(CurrentMultiplier, boostHours);
        Refresh();
    }

    private void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        bool active = gm.BoostActive;

        if (txt_ad_status != null)
        {
            txt_ad_status.text = active
                ? $"<color=#F0B441>{gm.boostMultiplier:0.#}x aktif</color>  -  {UIManager.ShortTime(gm.BoostSecondsLeft)} kaldi"
                : "<color=#A38A6E>Su an aktif boost yok</color>";
        }

        // Boost surerken tekrar izlemeye kapali: sure sifirlanmasin
        if (btn_watch_ad != null) btn_watch_ad.interactable = !active;
    }

    private void OnDestroy()
    {
        if (btn_watch_ad != null) btn_watch_ad.onClick.RemoveAllListeners();
    }
}
