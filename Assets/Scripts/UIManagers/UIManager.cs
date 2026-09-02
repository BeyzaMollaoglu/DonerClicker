using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

// SINIF ADI DOSYA ADIYLA AYNI YAPILDI
public class UIManager : MonoBehaviour
{
    [Header("Ana Arayüz")]
    public TextMeshProUGUI txt_doner_count;
    public TextMeshProUGUI txt_doner_rate;
    public TextMeshProUGUI txt_prestige_chip;

    [Header("Offline Kazanc Penceresi")]
    public RectTransform    offline_panel;
    public TextMeshProUGUI  txt_offline_time;
    public TextMeshProUGUI  txt_offline_amount;
    public UnityEngine.UI.Button btn_offline_ok;
    public Button button_icon_doner;
    public GameObject floatingTextPrefab;
    public Transform mainCanvas;

    [Header("Prestige (Reset) Sistemi")]
    public Button btn_reset;
    public TextMeshProUGUI txt_prestige_info;

    public static string FormatNumber(double value)
    {
        if (value < 1000) return value.ToString("0.##");
        string[] suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };
        int suffixIndex = 0;
        while (value >= 1000 && suffixIndex < suffixes.Length - 1)
        {
            value /= 1000;
            suffixIndex++;
        }
        return value.ToString("0.##") + suffixes[suffixIndex];
    }

    private void Start()
    {
        if (button_icon_doner != null) button_icon_doner.onClick.AddListener(GameManager.Instance.OnDonerClicked);
        if (btn_reset != null) btn_reset.onClick.AddListener(GameManager.Instance.PrestigeAscension);
        if (btn_offline_ok != null) btn_offline_ok.onClick.AddListener(HideOfflineReward);
        if (offline_panel != null) offline_panel.gameObject.SetActive(false);
    }

    float rateTick;

    private void Update()
    {
        // Boost geri sayimi saniyede bir tazelensin
        var gm = GameManager.Instance;
        if (gm == null || !gm.BoostActive) return;
        rateTick += Time.unscaledDeltaTime;
        if (rateTick >= 1f) { rateTick = 0f; UpdateRateText(gm.productionPerSecond); }
    }

    public void UpdateTotalDonerText(double amount)
    {
        if (txt_doner_count != null) txt_doner_count.text = FormatNumber(System.Math.Floor(amount));
    }

    public void UpdateRateText(double perSecond)
    {
        if (txt_doner_rate == null) return;
        if (perSecond <= 0) { txt_doner_rate.text = ""; return; }

        string s = "+" + FormatNumber(perSecond) + "/sn";

        // Boost aktifse carpani ve kalan sureyi ayni satirda goster
        var gm = GameManager.Instance;
        if (gm != null && gm.BoostActive)
            s += "   <color=#E8622C>" + gm.boostMultiplier.ToString("0.#") + "x " + ShortTime(gm.BoostSecondsLeft) + "</color>";

        txt_doner_rate.text = s;
    }

    /// <summary>Saniyeyi "3sa 42dk" gibi kisa metne cevirir.</summary>
    public static string ShortTime(double seconds)
    {
        int s = Mathf.Max(0, Mathf.RoundToInt((float)seconds));
        int h = s / 3600, m = (s % 3600) / 60;
        if (h > 0) return h + "sa " + m + "dk";
        if (m > 0) return m + "dk";
        return s + "sn";
    }

    /// <summary>Saniyeyi "4 saat 12 dakika" gibi uzun metne cevirir.</summary>
    public static string LongTime(double seconds)
    {
        int s = Mathf.Max(0, Mathf.RoundToInt((float)seconds));
        int h = s / 3600, m = (s % 3600) / 60;
        if (h > 0 && m > 0) return h + " saat " + m + " dakika";
        if (h > 0)          return h + " saat";
        if (m > 0)          return m + " dakika";
        return s + " saniye";
    }

    /// <summary>Oyuna donuste "yokken sunu kazandin" penceresini acar.</summary>
    public void ShowOfflineReward(double amount, double seconds, bool capped)
    {
        if (offline_panel == null) return;

        if (txt_offline_time != null)
            txt_offline_time.text = capped
                ? LongTime(seconds) + " boyunca calisti\n<color=#A38A6E>(en fazla bu kadar birikir)</color>"
                : LongTime(seconds) + " boyunca calisti";

        if (txt_offline_amount != null)
            txt_offline_amount.text = "+" + FormatNumber(amount) + " dilim";

        offline_panel.gameObject.SetActive(true);
        offline_panel.localScale = Vector3.one * 0.85f;
        offline_panel.DOKill();
        offline_panel.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
    }

    public void HideOfflineReward()
    {
        if (offline_panel == null) return;
        offline_panel.DOKill();
        offline_panel.DOScale(Vector3.one * 0.85f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => offline_panel.gameObject.SetActive(false));
    }

    public void UpdatePrestigeUI(int currentGolden, int pendingGolden)
    {
        if (txt_prestige_chip != null)
            txt_prestige_chip.text = FormatNumber(currentGolden);

        if (txt_prestige_info != null)
            txt_prestige_info.text = $"Sahip Olunan: {FormatNumber(currentGolden)} Altın Döner\n<color=#F0B441>Reset Atarsan: +{FormatNumber(pendingGolden)} Kazanacaksın</color>";
    }
    
    public void PlayClickFeedback(double clickAmount)
    {
        if (button_icon_doner == null) return;
        button_icon_doner.transform.DOKill(true);
        button_icon_doner.transform.localScale = Vector3.one * 0.9f;
        button_icon_doner.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
        SpawnFloatingText(clickAmount);
    }

    private void SpawnFloatingText(double amount)
    {
        if (floatingTextPrefab == null || mainCanvas == null) return;
        Vector2 spawnPosition = Input.mousePosition;
        GameObject floatingObj = Instantiate(floatingTextPrefab, mainCanvas);
        floatingObj.transform.position = spawnPosition;
        
        TextMeshProUGUI floatText = floatingObj.GetComponent<TextMeshProUGUI>();
        floatText.text = "+" + FormatNumber(amount);
        
        float randomX = Random.Range(-50f, 50f);
        Vector3 targetPos = floatingObj.transform.position + new Vector3(randomX, 150f, 0f);
        
        floatingObj.transform.DOMove(targetPos, 0.8f).SetEase(Ease.OutQuad);
        floatText.DOFade(0, 0.8f).OnComplete(() => Destroy(floatingObj));
    }

    private void OnDestroy()
    {
        if (button_icon_doner != null) button_icon_doner.onClick.RemoveAllListeners();
        if (btn_reset != null) btn_reset.onClick.RemoveAllListeners();
        if (btn_offline_ok != null) btn_offline_ok.onClick.RemoveAllListeners();
    }
}