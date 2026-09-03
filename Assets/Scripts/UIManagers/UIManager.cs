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

    float  rateTick;
    double targetDoner;      // gercek deger
    double shownDoner;       // ekranda yazan deger - hedefe dogru yumusakca kayar
    bool   counterReady;

    // Sayac metnini her KAREDE yazmak pahali: her atamada yeni string uretilir
    // ve TMP metni bastan olcer. Saniyede 30 kez yazmak gozle ayni gorunuyor
    // ama maliyeti yariya indiriyor. Ayni metin tekrar yazilmaz.
    float  countTick;
    string lastCountText;

    void SetCountText(string s)
    {
        if (txt_doner_count == null || s == lastCountText) return;
        lastCountText = s;
        txt_doner_count.text = s;
    }

    private void Update()
    {
        var gm = GameManager.Instance;

        // Sayac: aninda ziplamak yerine hedefe dogru kaysin.
        // Hesap her karede (akici olsun), ekrana yazma saniyede 30 kez.
        if (counterReady && txt_doner_count != null && shownDoner != targetDoner)
        {
            double diff = targetDoner - shownDoner;
            shownDoner += diff * Mathf.Clamp01(Time.unscaledDeltaTime * 14f);
            if (System.Math.Abs(targetDoner - shownDoner) < 0.5) shownDoner = targetDoner;

            countTick += Time.unscaledDeltaTime;
            if (countTick >= 0.033f)
            {
                countTick = 0f;
                SetCountText(FormatNumber(System.Math.Floor(shownDoner)));
            }
        }

        // Boost geri sayimi saniyede bir tazelensin
        if (gm == null || !gm.BoostActive) return;
        rateTick += Time.unscaledDeltaTime;
        if (rateTick >= 1f) { rateTick = 0f; UpdateRateText(gm.productionPerSecond); }
    }

    public void UpdateTotalDonerText(double amount)
    {
        targetDoner = amount;

        // Ilk deger ve buyuk sicramalar (offline odulu, prestij) aninda yazilsin
        if (!counterReady || System.Math.Abs(amount - shownDoner) > System.Math.Max(5000, amount * 0.30))
        {
            counterReady = true;
            shownDoner = amount;
            SetCountText(FormatNumber(System.Math.Floor(amount)));
        }
    }

    public void UpdateRateText(double perSecond)
    {
        if (txt_doner_rate == null) return;
        if (perSecond <= 0) { txt_doner_rate.text = ""; return; }

        var gm = GameManager.Instance;
        bool boosted = gm != null && (gm.BoostActive || gm.EventActive);

        string rate = "+" + FormatNumber(perSecond) + "/sn";

        if (!boosted) { txt_doner_rate.text = rate; return; }

        // ONEMLI: perSecond'a carpanlar ZATEN uygulanmis geliyor.
        // Carpani sayinin yanina esit agirlikta yazarsak "193 x 3 mu olacak"
        // diye okunuyordu. Cozum: sayiyi ALTIN yapip carpani sessiz bir not
        // olarak arkasina koymak - altin renk "bu hiz suanda artirilmis"
        // sinyalini veriyor, not da neden/ne kadar sureyle oldugunu soyluyor.
        double mult = gm.ActiveBoost;

        // Iki boost ayni anda aktifse ikisini de yazmak satiri sisiriyor.
        // Toplam carpani ve ILK BITECEK olanin suresini gostermek hem kisa
        // hem dogru: "su an x21'sin, 45 saniye sonra bu degisecek".
        int left = int.MaxValue;
        if (gm.EventActive) left = System.Math.Min(left, gm.EventSecondsLeft);
        if (gm.BoostActive) left = System.Math.Min(left, gm.BoostSecondsLeft);

        txt_doner_rate.text =
            $"<color=#F0B441>{rate}</color>" +
            $"   <size=85%><color=#A38A6E>×{mult:0.#}  ·  {ShortTime(left)}</color></size>";
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

    /// <summary>Altin Doner odulu / kademe atlama gibi anlik bildirimler icin buyuk yazi.</summary>
    public void ShowEventToast(string message, Vector2 anchoredPos)
    {
        if (floatingTextPrefab == null || mainCanvas == null) return;

        GameObject obj = Instantiate(floatingTextPrefab, mainCanvas);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(700f, 220f);

        TextMeshProUGUI txt = obj.GetComponent<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.text = message;
            txt.enableAutoSizing = false;
            txt.fontSize = 62f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.DOFade(0f, 1.1f).SetDelay(0.7f);
        }

        rt.localScale = Vector3.one * 0.6f;
        rt.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        rt.DOAnchorPosY(anchoredPos.y + 230f, 1.8f).SetEase(Ease.OutQuad)
          .OnComplete(() => { if (obj != null) Destroy(obj); });
    }

    public void HideOfflineReward()
    {
        if (offline_panel == null) return;
        offline_panel.DOKill();
        offline_panel.DOScale(Vector3.one * 0.85f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => offline_panel.gameObject.SetActive(false));
    }

    public void UpdatePrestigeUI(int currentGolden, int pendingGolden)   // Altin Masa
    {
        if (txt_prestige_chip != null)
            txt_prestige_chip.text = FormatNumber(currentGolden);

        // Puan yoksa buton sonuk dursun - bosuna basip "neden olmadi" dedirtmesin
        if (btn_reset != null) btn_reset.interactable = pendingGolden > 0;

        if (txt_prestige_info != null)
            txt_prestige_info.text = pendingGolden > 0
                ? $"Prestij yaparsan <color=#F0B441>+{FormatNumber(pendingGolden)} Altın Maşa</color> kazanırsın.\n<color=#A38A6E>Dilimler, işçiler ve geliştirmeler sıfırlanır;\naşağıdaki kalıcı yükseltmeler kalır.</color>"
                : $"<color=#A38A6E>Prestij için henüz yeterli üretim yok.\nToplam ürettiğin dilim arttıkça Altın Maşa kazanırsın.</color>";
    }
    
    public void PlayClickFeedback(double clickAmount)
    {
        if (button_icon_doner == null) return;
        button_icon_doner.transform.DOKill(true);
        button_icon_doner.transform.localScale = Vector3.one * 0.9f;
        button_icon_doner.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
        SpawnFloatingText(clickAmount);

        if (SliceFx.Instance != null) SliceFx.Instance.Burst(Input.mousePosition);
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