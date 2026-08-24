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

    [Header("Ekstra Butonlar")]
    public Button btn_reset;

    [Header("Blocker (Arka Plan Kapatıcı)")]
    public Button btn_blocker; // <-- YENİ EKLENDİ

    private RectTransform activePanel; // <-- O an ekranda hangi panelin açık olduğunu tutacak

    private void Start()
    {
        // Ana döner tıklaması
        button_icon_doner.onClick.AddListener(GameManager.Instance.OnDonerClicked);

        if (btn_reset != null)
        {
            btn_reset.onClick.AddListener(GameManager.Instance.HardResetGame);
        }

        // Panel butonları
        if (btn_open_upgrades != null) btn_open_upgrades.onClick.AddListener(() => OpenPanel(panel_upgrades));
        if (btn_close_upgrades != null) btn_close_upgrades.onClick.AddListener(() => ClosePanel(panel_upgrades));

        if (btn_open_workers != null) btn_open_workers.onClick.AddListener(() => OpenPanel(panel_workers));
        if (btn_close_workers != null) btn_close_workers.onClick.AddListener(() => ClosePanel(panel_workers));

        // Blocker (Arka plan) butonu ayarı
        if (btn_blocker != null)
        {
            btn_blocker.onClick.AddListener(CloseActivePanel);
            btn_blocker.gameObject.SetActive(false); // Oyun başlarken görünmez olsun
        }
    }

    public void UpdateTotalDonerText(double amount)
    {
        txt_doner_count.text = amount.ToString("F0");
    }

    // Tıklama animasyonu (Juice)
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
        floatText.text = "+" + amount.ToString("F0");

        float randomX = Random.Range(-50f, 50f);
        Vector3 targetPos = floatingObj.transform.position + new Vector3(randomX, 150f, 0f);

        floatingObj.transform.DOMove(targetPos, 0.8f).SetEase(Ease.OutQuad);
        floatText.DOFade(0, 0.8f).OnComplete(() => Destroy(floatingObj));
    }

    private void OpenPanel(RectTransform panel)
    {
        activePanel = panel; // Hangi panelin açıldığını kaydet
        if (btn_blocker != null) btn_blocker.gameObject.SetActive(true); // Blocker'ı görünür yap

        panel.gameObject.SetActive(true);
        panel.anchoredPosition = new Vector2(0, -2500);
        panel.DOAnchorPos(Vector2.zero, 0.6f).SetEase(Ease.OutBack);
    }

    private void ClosePanel(RectTransform panel)
    {
        if (btn_blocker != null) btn_blocker.gameObject.SetActive(false); // Blocker'ı kapat

        panel.DOAnchorPos(new Vector2(0, -2500), 0.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            panel.gameObject.SetActive(false);
            if (activePanel == panel) activePanel = null; // Kapanan panel buysa hafızayı temizle
        });
    }

    // Blocker'a (arka plana) tıklandığında çalışacak olan YENİ FONKSİYON
    private void CloseActivePanel()
    {
        if (activePanel != null)
        {
            ClosePanel(activePanel);
        }
    }

    private void OnDestroy()
    {
        button_icon_doner.onClick.RemoveAllListeners();
        if (btn_open_upgrades != null) btn_open_upgrades.onClick.RemoveAllListeners();
        if (btn_close_upgrades != null) btn_close_upgrades.onClick.RemoveAllListeners();
        if (btn_open_workers != null) btn_open_workers.onClick.RemoveAllListeners();
        if (btn_close_workers != null) btn_close_workers.onClick.RemoveAllListeners();
        if (btn_blocker != null) btn_blocker.onClick.RemoveAllListeners();
    }
}