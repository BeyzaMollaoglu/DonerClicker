using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("Ana Arayüz")]
    public TextMeshProUGUI txt_doner_count;
    public Button button_icon_doner;
    public GameObject floatingTextPrefab;
    public Transform mainCanvas;

    [Header("Paneller")]
    public RectTransform panel_upgrades;
    public RectTransform panel_workers;

    [Header("Panel Aç/Kapat Butonları")]
    public Button btn_open_upgrades;
    public Button btn_close_upgrades;
    public Button btn_open_workers;
    public Button btn_close_workers;

    [Header("Blocker (Arka Plan Kapatıcı)")]
    public Button btn_blocker;

    [Header("Prestige (Reset) Sistemi")]
    public Button btn_reset;
    public TextMeshProUGUI txt_prestige_info;

    private RectTransform activePanel;


    public static string FormatNumber(double value)
    {
        // Math.Floor(value) kısmını sildik! 
        // Yerine ToString("0.##") yazdık ki 0.1, 0.5 gibi sayılar ekranda virgülden sonraki haliyle düzgünce görünsün.
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
        button_icon_doner.onClick.AddListener(GameManager.Instance.OnDonerClicked);

        // Prestige (Melek) Reset butonu
        if (btn_reset != null) btn_reset.onClick.AddListener(GameManager.Instance.PrestigeAscension);

        if (btn_open_upgrades != null) btn_open_upgrades.onClick.AddListener(() => OpenPanel(panel_upgrades));
        if (btn_close_upgrades != null) btn_close_upgrades.onClick.AddListener(() => ClosePanel(panel_upgrades));

        if (btn_open_workers != null) btn_open_workers.onClick.AddListener(() => OpenPanel(panel_workers));
        if (btn_close_workers != null) btn_close_workers.onClick.AddListener(() => ClosePanel(panel_workers));

        if (btn_blocker != null)
        {
            btn_blocker.onClick.AddListener(CloseActivePanel);
            btn_blocker.gameObject.SetActive(false);
        }
    }

    public void UpdateTotalDonerText(double amount)
    {
        txt_doner_count.text = FormatNumber(System.Math.Floor(amount));
    }

    public void UpdatePrestigeUI(int currentGolden, int pendingGolden)
    {
        if (txt_prestige_info != null)
        {
            txt_prestige_info.text = $"Sahip Olunan: {FormatNumber(currentGolden)} Altın Döner\n<color=#FFD700>Reset Atarsan: +{FormatNumber(pendingGolden)} Kazanacaksın</color>";
        }
    }

    public void PlayClickFeedback(double clickAmount)
    {
        button_icon_doner.transform.DOKill(true);
        button_icon_doner.transform.localScale = Vector3.one * 0.9f;
        button_icon_doner.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);

        SpawnFloatingText(clickAmount);
    }

    private void SpawnFloatingText(double amount)
    {
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

    private void OpenPanel(RectTransform panel)
    {
        activePanel = panel;
        if (btn_blocker != null) btn_blocker.gameObject.SetActive(true);

        panel.gameObject.SetActive(true);
        panel.anchoredPosition = new Vector2(0, -2500);
        panel.DOAnchorPos(Vector2.zero, 0.6f).SetEase(Ease.OutBack);
    }

    private void ClosePanel(RectTransform panel)
    {
        if (btn_blocker != null) btn_blocker.gameObject.SetActive(false);

        panel.DOAnchorPos(new Vector2(0, -2500), 0.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            panel.gameObject.SetActive(false);
            if (activePanel == panel) activePanel = null;
        });
    }

    private void CloseActivePanel()
    {
        if (activePanel != null) ClosePanel(activePanel);
    }

    private void OnDestroy()
    {
        button_icon_doner.onClick.RemoveAllListeners();
        if (btn_open_upgrades != null) btn_open_upgrades.onClick.RemoveAllListeners();
        if (btn_close_upgrades != null) btn_close_upgrades.onClick.RemoveAllListeners();
        if (btn_open_workers != null) btn_open_workers.onClick.RemoveAllListeners();
        if (btn_close_workers != null) btn_close_workers.onClick.RemoveAllListeners();
        if (btn_blocker != null) btn_blocker.onClick.RemoveAllListeners();
        if (btn_reset != null) btn_reset.onClick.RemoveAllListeners();
    }
}