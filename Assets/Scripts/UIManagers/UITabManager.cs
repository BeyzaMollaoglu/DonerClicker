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

public class UITabManager : MonoBehaviour 
{
    [Header("Paneller & Arka Plan Kapatıcı")]
    public Button btn_blocker;
    private RectTransform activePanel;

    [Header("Sekme Sistemi (Tab Bar)")]
    public List<TabItem> tabs;
    public int defaultTabIndex = 2; 
    
    [Tooltip("Seçili sekme yukarı doğru kaç piksel UZAYACAK?")]
    public float activeHeightExtension = 35f; 
    public float animationDuration = 0.35f; 
    
    public Color activeColor = new Color(0.2f, 0.7f, 0.1f);
    public Color inactiveColor = new Color(0.1f, 0.4f, 0.8f);

    private int currentTabIndex = -1;
    private float originalHeight; 

    private IEnumerator Start()
    {
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

            // Panel basligindaki X butonunu bul ve bagla
            if (tabs[i].linkedPanel != null)
            {
                foreach (var b in tabs[i].linkedPanel.GetComponentsInChildren<Button>(true))
                    if (b.name == "btn_panel_close") b.onClick.AddListener(CloseActivePanel);
            }
        }

        yield return null; 

        // rect.height yerine sizeDelta.y kullanmak Layout Group'larla daha uyumludur
        if(tabs.Count > 0 && tabs[0].buttonRect != null)
        {
            originalHeight = tabs[0].buttonRect.rect.height;
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
                tab.buttonRect.sizeDelta = new Vector2(tab.buttonRect.sizeDelta.x, isActive ? originalHeight + activeHeightExtension : originalHeight);
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

    private void OpenPanel(RectTransform panel)
    {
        activePanel = panel;
        if (btn_blocker != null) btn_blocker.gameObject.SetActive(true);
        panel.gameObject.SetActive(true);

        // Panel her acildiginda liste en basa donsun
        StartCoroutine(ResetScrollRoutine(panel));

        // Gelistirmeler paneli acildi: sadece sekme rozetini sifirla.
        // "YENI" etiketleri panel kapanana kadar dursun.
        if (IsUpgradePanel(panel)) UpgradeManager.Instance.MarkAllSeen();

        panel.DOKill(); // Panel animasyonlarını çakışmaya karşı korur
        panel.anchoredPosition = new Vector2(0, -2500);
        panel.DOAnchorPos(Vector2.zero, 0.6f).SetEase(Ease.OutBack);
    }

    // Panelin icindeki ScrollRect'i en uste alir.
    // Iki asamali: once hemen, sonra layout oturduktan sonra tekrar -
    // cunku panel yeni aktif edildiginde Content henuz olculmemis oluyor.
    private IEnumerator ResetScrollRoutine(RectTransform panel)
    {
        ScrollRect sr = panel.GetComponentInChildren<ScrollRect>(true);
        if (sr == null) yield break;

        sr.StopMovement();
        sr.verticalNormalizedPosition = 1f;

        yield return null;

        if (sr != null)
        {
            sr.StopMovement();
            sr.verticalNormalizedPosition = 1f;
        }
    }

    private bool IsUpgradePanel(RectTransform panel)
    {
        var um = UpgradeManager.Instance;
        return um != null && um.upgradeContent != null && um.upgradeContent.IsChildOf(panel);
    }

    private void ClosePanel(RectTransform panel)
    {
        // Panel kapandi - oyuncu kartlari gordu, "YENI" etiketleri kalkabilir
        if (IsUpgradePanel(panel)) UpgradeManager.Instance.ClearNewLabels();

        if (btn_blocker != null) btn_blocker.gameObject.SetActive(false);
        
        panel.DOKill(); // Panel animasyonlarını çakışmaya karşı korur
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
                    tab.buttonRect.DOKill(); // YENİ: Eski animasyonu öldür
                    tab.buttonRect.DOSizeDelta(new Vector2(tab.buttonRect.sizeDelta.x, originalHeight + activeHeightExtension), animationDuration).SetEase(Ease.OutBack); 
                }
                if (tab.backgroundImage != null) 
                { 
                    tab.backgroundImage.DOKill(); 
                    tab.backgroundImage.DOColor(activeColor, animationDuration); 
                }
                if (tab.tabText != null) 
                { 
                    tab.tabText.DOKill();
                    tab.tabText.gameObject.SetActive(true); 
                    tab.tabText.DOFade(1f, animationDuration); 
                }
            }
            else
            {
                if (tab.buttonRect != null) 
                { 
                    tab.buttonRect.DOKill(); // YENİ: Eski animasyonu öldür
                    tab.buttonRect.DOSizeDelta(new Vector2(tab.buttonRect.sizeDelta.x, originalHeight), animationDuration).SetEase(Ease.OutQuad); 
                }
                if (tab.backgroundImage != null) 
                { 
                    tab.backgroundImage.DOKill();
                    tab.backgroundImage.DOColor(inactiveColor, animationDuration); 
                }
                if (tab.tabText != null) 
                { 
                    tab.tabText.DOKill();
                    tab.tabText.DOFade(0f, animationDuration).OnComplete(() => { if (currentTabIndex != tabs.IndexOf(tab)) tab.tabText.gameObject.SetActive(false); }); 
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
                if (tab.buttonRect != null) { tab.buttonRect.DOKill(); tab.buttonRect.DOSizeDelta(new Vector2(tab.buttonRect.sizeDelta.x, originalHeight + activeHeightExtension), animationDuration).SetEase(Ease.OutBack); }
                if (tab.backgroundImage != null) { tab.backgroundImage.DOKill(); tab.backgroundImage.DOColor(activeColor, animationDuration); }
                if (tab.tabText != null) { tab.tabText.DOKill(); tab.tabText.gameObject.SetActive(true); tab.tabText.DOFade(1f, animationDuration); }
            }
            else
            {
                if (tab.buttonRect != null) { tab.buttonRect.DOKill(); tab.buttonRect.DOSizeDelta(new Vector2(tab.buttonRect.sizeDelta.x, originalHeight), animationDuration).SetEase(Ease.OutQuad); }
                if (tab.backgroundImage != null) { tab.backgroundImage.DOKill(); tab.backgroundImage.DOColor(inactiveColor, animationDuration); }
                if (tab.tabText != null) { tab.tabText.DOKill(); tab.tabText.DOFade(0f, animationDuration).OnComplete(() => { if (currentTabIndex != tabs.IndexOf(tab)) tab.tabText.gameObject.SetActive(false); }); }
            }
        }
    }

    private void OnDestroy()
    {
        if (btn_blocker != null) btn_blocker.onClick.RemoveAllListeners();
        foreach (var tab in tabs) if (tab.tabButton != null) tab.tabButton.onClick.RemoveAllListeners();
    }
}