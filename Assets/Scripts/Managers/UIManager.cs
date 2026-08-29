using System.Collections; 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class TabItem
{
    public string tabName; 
    public Button tabButton; 
    public RectTransform buttonRect; 
    public Image backgroundImage; 
    public TextMeshProUGUI tabText; 
    public RectTransform linkedPanel; 
}

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

    [Header("Blocker (Arka Plan Kapatıcı)")]
    public Button btn_blocker;

    [Header("Prestige (Reset) Sistemi")]
    public Button btn_reset;
    public TextMeshProUGUI txt_prestige_info;

    private RectTransform activePanel;

    [Header("Sekme Sistemi (Tab Bar)")]
    public List<TabItem> tabs;
    public int defaultTabIndex = 2; 
    
    public float activeYOffset = 10f; 
    public float activeScale = 1.15f; 
    public float inactiveScale = 0.95f; 
    public float animationDuration = 0.35f; 
    
    public Color activeColor = new Color(0.2f, 0.7f, 0.1f);
    public Color inactiveColor = new Color(0.1f, 0.4f, 0.8f);

    private int currentTabIndex = -1;
    private float originalYPos; 

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

    private IEnumerator Start()
    {
        if (button_icon_doner != null) button_icon_doner.onClick.AddListener(GameManager.Instance.OnDonerClicked);
        if (btn_reset != null) btn_reset.onClick.AddListener(GameManager.Instance.PrestigeAscension);

        if (btn_blocker != null)
        {
            btn_blocker.onClick.AddListener(CloseActivePanel);
            btn_blocker.gameObject.SetActive(false);
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i; 
            if (tabs[i].tabButton != null)
                tabs[i].tabButton.onClick.AddListener(() => SelectTab(index));
        }

        // Unity'nin UI Layout Group'u hesaplayıp kutuları milimetrik dizmesi için 1 frame bekle.
        yield return null; 

        // Şimdi kutular yerleştiğine göre, doğru Y pozisyonunu güvenle hafızaya alabiliriz.
        if(tabs.Count > 0 && tabs[0].buttonRect != null)
        {
            originalYPos = tabs[0].buttonRect.anchoredPosition.y;
        }
        
        ForceInitialState();
    }

    private void ForceInitialState()
    {
        currentTabIndex = defaultTabIndex;

        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == defaultTabIndex);
            TabItem tab = tabs[i];

            if (tab.buttonRect != null)
            {
                tab.buttonRect.anchoredPosition = new Vector2(tab.buttonRect.anchoredPosition.x, isActive ? originalYPos + activeYOffset : originalYPos);
                tab.buttonRect.localScale = Vector3.one * (isActive ? activeScale : inactiveScale);
            }
            
            if (tab.backgroundImage != null) tab.backgroundImage.color = isActive ? activeColor : inactiveColor;
            
            if (tab.tabText != null)
            {
                tab.tabText.alpha = isActive ? 1f : 0f;
                tab.tabText.gameObject.SetActive(isActive);
            }

            if (isActive && tab.linkedPanel != null) OpenPanel(tab.linkedPanel);
        }
    }

    public void UpdateTotalDonerText(double amount) { if (txt_doner_count != null) txt_doner_count.text = FormatNumber(System.Math.Floor(amount)); }
    public void UpdatePrestigeUI(int currentGolden, int pendingGolden) { if (txt_prestige_info != null) txt_prestige_info.text = $"Sahip Olunan: {FormatNumber(currentGolden)} Altın Döner\n<color=#FFD700>Reset Atarsan: +{FormatNumber(pendingGolden)} Kazanacaksın</color>"; }
    
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
        if (currentTabIndex != defaultTabIndex) SelectTabVisualsOnly(defaultTabIndex);
    }

    public void SelectTab(int index)
    {
        if (index == currentTabIndex) return; 
        if (activePanel != null) ClosePanel(activePanel);
        currentTabIndex = index;

        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == index);
            TabItem tab = tabs[i];

            if (isActive && tab.linkedPanel != null) OpenPanel(tab.linkedPanel);

            if (isActive)
            {
                if (tab.buttonRect != null)
                {
                    tab.buttonRect.DOAnchorPosY(originalYPos + activeYOffset, animationDuration).SetEase(Ease.OutBack);
                    tab.buttonRect.DOScale(activeScale, animationDuration).SetEase(Ease.OutBack);
                }
                if (tab.backgroundImage != null) tab.backgroundImage.DOColor(activeColor, animationDuration);
                
                if (tab.tabText != null)
                {
                    tab.tabText.gameObject.SetActive(true);
                    tab.tabText.DOFade(1f, animationDuration);
                }
            }
            else
            {
                if (tab.buttonRect != null)
                {
                    tab.buttonRect.DOAnchorPosY(originalYPos, animationDuration).SetEase(Ease.OutQuad);
                    tab.buttonRect.DOScale(inactiveScale, animationDuration).SetEase(Ease.OutQuad);
                }
                if (tab.backgroundImage != null) tab.backgroundImage.DOColor(inactiveColor, animationDuration);
                
                if (tab.tabText != null)
                {
                    tab.tabText.DOFade(0f, animationDuration).OnComplete(() => {
                        if (currentTabIndex != tabs.IndexOf(tab)) tab.tabText.gameObject.SetActive(false);
                    });
                }
            }
        }
    }

    private void SelectTabVisualsOnly(int index)
    {
        currentTabIndex = index;
        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == index);
            TabItem tab = tabs[i];

            if (isActive)
            {
                if (tab.buttonRect != null) { tab.buttonRect.DOAnchorPosY(originalYPos + activeYOffset, animationDuration).SetEase(Ease.OutBack); tab.buttonRect.DOScale(activeScale, animationDuration).SetEase(Ease.OutBack); }
                if (tab.backgroundImage != null) tab.backgroundImage.DOColor(activeColor, animationDuration);
                if (tab.tabText != null) { tab.tabText.gameObject.SetActive(true); tab.tabText.DOFade(1f, animationDuration); }
            }
            else
            {
                if (tab.buttonRect != null) { tab.buttonRect.DOAnchorPosY(originalYPos, animationDuration).SetEase(Ease.OutQuad); tab.buttonRect.DOScale(inactiveScale, animationDuration).SetEase(Ease.OutQuad); }
                if (tab.backgroundImage != null) tab.backgroundImage.DOColor(inactiveColor, animationDuration);
                if (tab.tabText != null) { tab.tabText.DOFade(0f, animationDuration).OnComplete(() => { if (currentTabIndex != tabs.IndexOf(tab)) tab.tabText.gameObject.SetActive(false); }); }
            }
        }
    }

    private void OnDestroy()
    {
        if (button_icon_doner != null) button_icon_doner.onClick.RemoveAllListeners();
        if (btn_blocker != null) btn_blocker.onClick.RemoveAllListeners();
        if (btn_reset != null) btn_reset.onClick.RemoveAllListeners();
        
        foreach (var tab in tabs)
        {
            if (tab.tabButton != null) tab.tabButton.onClick.RemoveAllListeners();
        }
    }
}