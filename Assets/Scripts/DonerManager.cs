using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

// UNITY'DE AÇILIR MENÜ OLUŞTURACAK LİSTEMİZ
public enum UpgradeType
{
    ClickPowerAdd,              // Tıklama gücüne DİREKT EKLER (Örn: +5)
    ClickPowerMultiplier,       // Tıklama gücünü ÇARPAR (Örn: x2 veya %50 için x1.5)
    PassiveProductionAdd,       // Pasif üretime DİREKT EKLER (Örn: +10)
    PassiveProductionMultiplier // Pasif üretimi ÇARPAR (Örn: x3)
}

[System.Serializable]
public class UpgradeItem
{
    public string upgradeName;
    public UpgradeType type;
    public double cost;
    public double effectAmount;       // Artış veya Çarpan miktarı

    [HideInInspector] public bool isPurchased = false;
    [HideInInspector] public Button buttonComponent;
    [HideInInspector] public TextMeshProUGUI buttonText;
}

public class DonerManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI txt_doner_count;
    public Button button_icon_doner;
    public GameObject floatingTextPrefab;
    public Transform mainCanvas;

    [Header("UI Panels")]
    public RectTransform panel_upgrades;

    [Header("Upgrade System")]
    public GameObject upgradeButtonPrefab;
    public Transform upgradeContent;
    public List<UpgradeItem> upgradeList;

    [Header("Game Data")]
    public double totalDoner = 0;

    // OYUNCUYA YANSIYAN GERÇEK SAYILAR (Formülle hesaplanan sonuçlar)
    public double clickPower = 1;
    public double productionPerSecond = 0;
    public float productionInterval = 1f;

    // ARKA PLANDA TUTACAĞIMIZ TEMEL DEĞERLER (Hesaplamalar için)
    private double baseClickPower = 1;
    private double clickMultiplier = 1; // Çarpanlar her zaman 1'den başlar

    private double basePassiveProduction = 0;
    private double passiveMultiplier = 1;

    void Start()
    {
        button_icon_doner.onClick.AddListener(OnDonerClicked);

        // Oyun başlarken temel değerleri formülden geçir
        RecalculateStats();
        UpdateUI();

        StartCoroutine(AutoProductionLoop());
        InitializeUpgrades();
    }

    private void InitializeUpgrades()
    {
        for (int i = 0; i < upgradeList.Count; i++)
        {
            int index = i;
            UpgradeItem item = upgradeList[i];

            // Eğer daha önceden alınmışsa hiç buton üretme
            if (item.isPurchased) continue;

            GameObject newBtnObj = Instantiate(upgradeButtonPrefab, upgradeContent);

            item.buttonText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            item.buttonComponent = newBtnObj.GetComponent<Button>();

            item.buttonComponent.onClick.AddListener(() => BuyUpgrade(index));

            // Tipe göre metin ve sembol (+ veya x) ayarlama
            string typeText = "";
            string symbol = "";

            if (item.type == UpgradeType.ClickPowerAdd) { typeText = "Tıklama"; symbol = "+"; }
            else if (item.type == UpgradeType.ClickPowerMultiplier) { typeText = "Tıklama"; symbol = "x"; }
            else if (item.type == UpgradeType.PassiveProductionAdd) { typeText = "Saniye/Üretim"; symbol = "+"; }
            else if (item.type == UpgradeType.PassiveProductionMultiplier) { typeText = "Saniye/Üretim"; symbol = "x"; }

            item.buttonText.text = $"{item.upgradeName}\n{typeText}: {symbol}{item.effectAmount}\nMaliyet: {item.cost} TL";
        }
    }

    private void BuyUpgrade(int index)
    {
        UpgradeItem item = upgradeList[index];

        if (!item.isPurchased && totalDoner >= item.cost)
        {
            totalDoner -= item.cost;

            // 1. DEĞERLERİ ARKA PLANDAKİ TEMEL LİSTEYE EKLE
            switch (item.type)
            {
                case UpgradeType.ClickPowerAdd:
                    baseClickPower += item.effectAmount;
                    break;
                case UpgradeType.ClickPowerMultiplier:
                    clickMultiplier *= item.effectAmount;
                    break;
                case UpgradeType.PassiveProductionAdd:
                    basePassiveProduction += item.effectAmount;
                    break;
                case UpgradeType.PassiveProductionMultiplier:
                    passiveMultiplier *= item.effectAmount;
                    break;
            }

            item.isPurchased = true;

            // Butonu tamamen sil, alttakiler yukarı kaysın
            Destroy(item.buttonComponent.gameObject);

            // 2. MATEMATİĞİ SIFIRDAN HESAPLA (Alma sırası hatasını çözen formül)
            RecalculateStats();

            UpdateUI();
        }
    }

    // OYUNUN KALBİ OLAN FORMÜL
    private void RecalculateStats()
    {
        clickPower = baseClickPower * clickMultiplier;
        productionPerSecond = basePassiveProduction * passiveMultiplier;
    }

    private void OnDonerClicked()
    {
        totalDoner += clickPower;
        UpdateUI();

        button_icon_doner.transform.DOKill(true);
        button_icon_doner.transform.localScale = Vector3.one * 0.9f;
        button_icon_doner.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);

        SpawnFloatingText();
    }

    private void SpawnFloatingText()
    {
        Vector2 spawnPosition = Input.mousePosition;
        GameObject floatingObj = Instantiate(floatingTextPrefab, mainCanvas);
        floatingObj.transform.position = spawnPosition;

        TextMeshProUGUI floatText = floatingObj.GetComponent<TextMeshProUGUI>();
        floatText.text = "+" + clickPower.ToString("F0");

        float randomX = Random.Range(-50f, 50f);
        Vector3 targetPos = floatingObj.transform.position + new Vector3(randomX, 150f, 0f);

        floatingObj.transform.DOMove(targetPos, 0.8f).SetEase(Ease.OutQuad);
        floatText.DOFade(0, 0.8f).OnComplete(() =>
        {
            Destroy(floatingObj);
        });
    }

    private IEnumerator AutoProductionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(productionInterval);

            if (productionPerSecond > 0)
            {
                totalDoner += productionPerSecond;
                UpdateUI();
            }
        }
    }

    private void UpdateUI()
    {
        txt_doner_count.text = totalDoner.ToString("F0");
    }

    public void OpenUpgradesPanel()
    {
        panel_upgrades.gameObject.SetActive(true);
        panel_upgrades.anchoredPosition = new Vector2(0, -2500);
        panel_upgrades.DOAnchorPos(Vector2.zero, 0.8f).SetEase(Ease.OutBack);
    }

    public void CloseUpgradesPanel()
    {
        panel_upgrades.DOAnchorPos(new Vector2(0, -2500), 0.7f).SetEase(Ease.InBack).OnComplete(() =>
        {
            panel_upgrades.gameObject.SetActive(false);
        });
    }

    private void OnDestroy()
    {
        if (button_icon_doner != null)
        {
            button_icon_doner.onClick.RemoveAllListeners();
        }
    }
}