using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum UpgradeType
{
    ClickPowerAdd,
    ClickPowerMultiplier,
    PassiveProductionAdd,
    PassiveProductionMultiplier
}

[System.Serializable]
public class UpgradeItem
{
    public string upgradeName;
    public UpgradeType type;
    public double baseCost;          
    public float costMultiplier;     
    public double effectAmount;

    public int requiredUpgradeIndex; 
    public int requiredUpgradeLevel; 

    [HideInInspector] public int level = 0; 
    [HideInInspector] public double currentCost;
    [HideInInspector] public bool isUnlocked = false;
    [HideInInspector] public Button buttonComponent;
    [HideInInspector] public TextMeshProUGUI buttonText;
}

[System.Serializable]
public class UpgradeDataWrapper
{
    public List<UpgradeItem> upgrades;
}

public class UpgradeManager : MonoBehaviour
{
    [Header("Geliştirmeler (Upgrades)")]
    public GameObject upgradeButtonPrefab;
    public Transform upgradeContent;
    
    [HideInInspector] public List<UpgradeItem> upgradeList;

    void Start()
    {
        LoadUpgradesFromJSON();

        GameSaveData saveData = SaveManager.Instance.LoadGame();
        if (saveData != null && saveData.upgradeLevels.Count == upgradeList.Count)
        {
            for (int i = 0; i < upgradeList.Count; i++)
            {
                upgradeList[i].level = saveData.upgradeLevels[i];
                
                for (int j = 0; j < upgradeList[i].level; j++)
                {
                    ApplyUpgradeEffect(upgradeList[i]);
                }
            }
        }

        InitializeUpgrades();
        CheckUnlocks(); // Başlangıçta kilitleri kontrol et ve tıklanabilirlikleri aç
        GameManager.Instance.RecalculateStats();
    }

    private void LoadUpgradesFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("upgrades");
        if (jsonFile != null)
        {
            UpgradeDataWrapper wrapper = JsonUtility.FromJson<UpgradeDataWrapper>(jsonFile.text);
            upgradeList = wrapper.upgrades;
        }
    }

    private void InitializeUpgrades()
    {
        for (int i = 0; i < upgradeList.Count; i++)
        {
            int index = i;
            UpgradeItem item = upgradeList[i];

            item.currentCost = item.baseCost * Mathf.Pow(item.costMultiplier, item.level);

            GameObject newBtnObj = Instantiate(upgradeButtonPrefab, upgradeContent);
            item.buttonText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            item.buttonComponent = newBtnObj.GetComponent<Button>();

            item.buttonComponent.onClick.AddListener(() => BuyUpgrade(index));
            
            // Başlangıçta butonu tıklanamaz (soluk) yap
            item.buttonComponent.interactable = false; 

            UpdateUpgradeUI(item);
        }
    }

    // Arayüzü her zaman normal fiyat ve statlarla günceller
    private void UpdateUpgradeUI(UpgradeItem item)
    {
        string typeText = item.type == UpgradeType.ClickPowerAdd || item.type == UpgradeType.ClickPowerMultiplier ? "Tıklama" : "Üretim";
        string symbol = item.type == UpgradeType.ClickPowerAdd || item.type == UpgradeType.PassiveProductionAdd ? "+" : "x";

        item.buttonText.text = $"{item.upgradeName} (Lvl {item.level})\n{typeText}: {symbol}{item.effectAmount}\nMaliyet: {item.currentCost.ToString("F0")} TL";
    }

    public void CheckUnlocks()
    {
        foreach (var item in upgradeList)
        {
            // Şart yoksa (-1) VEYA gereken upgrade yeterli seviyedeyse tıklanabilirliği aç
            if (item.requiredUpgradeIndex == -1 || 
               (item.requiredUpgradeIndex < upgradeList.Count && upgradeList[item.requiredUpgradeIndex].level >= item.requiredUpgradeLevel))
            {
                if (!item.isUnlocked)
                {
                    item.isUnlocked = true;
                    if (item.buttonComponent != null) 
                    {
                        item.buttonComponent.interactable = true; // Butonu aktif hale getir
                    }
                }
            }
        }
    }

    private void BuyUpgrade(int index)
    {
        UpgradeItem item = upgradeList[index];

        if (GameManager.Instance.SpendDoner(item.currentCost))
        {
            item.level++;
            item.currentCost = item.baseCost * Mathf.Pow(item.costMultiplier, item.level);
            
            ApplyUpgradeEffect(item);
            UpdateUpgradeUI(item);
            
            GameManager.Instance.RecalculateStats();
            CheckUnlocks(); // Alım yapıldıktan sonra alttaki açıldı mı diye kontrol et
        }
    }

    private void ApplyUpgradeEffect(UpgradeItem item)
    {
        switch (item.type)
        {
            case UpgradeType.ClickPowerAdd: GameManager.Instance.baseClickPower += item.effectAmount; break;
            case UpgradeType.ClickPowerMultiplier: GameManager.Instance.clickMultiplier *= item.effectAmount; break;
            case UpgradeType.PassiveProductionAdd: GameManager.Instance.basePassiveProduction += item.effectAmount; break;
            case UpgradeType.PassiveProductionMultiplier: GameManager.Instance.passiveMultiplier *= item.effectAmount; break;
        }
    }
}