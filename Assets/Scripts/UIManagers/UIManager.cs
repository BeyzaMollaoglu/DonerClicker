using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

// SINIF ADI DOSYA ADIYLA AYNI YAPILDI
public class UIManager : MonoBehaviour
{
    [Header("Ana Arayüz")]
    public TextMeshProUGUI txt_doner_count;
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
    }

    public void UpdateTotalDonerText(double amount)
    {
        if (txt_doner_count != null) txt_doner_count.text = FormatNumber(System.Math.Floor(amount));
    }

    public void UpdatePrestigeUI(int currentGolden, int pendingGolden)
    {
        if (txt_prestige_info != null)
            txt_prestige_info.text = $"Sahip Olunan: {FormatNumber(currentGolden)} Altın Döner\n<color=#FFD700>Reset Atarsan: +{FormatNumber(pendingGolden)} Kazanacaksın</color>";
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
    }
}