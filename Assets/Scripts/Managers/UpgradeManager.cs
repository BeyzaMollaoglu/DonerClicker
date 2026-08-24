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
    public double cost;
    public double effectAmount;

    [HideInInspector] public bool isPurchased = false;
    [HideInInspector] public Button buttonComponent;
    [HideInInspector] public TextMeshProUGUI buttonText;
}

// JSON'daki 'upgrades' dizisini tutacak sarmalayıcı
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
        InitializeUpgrades();
    }

    private void LoadUpgradesFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("upgrades");
        if (jsonFile != null)
        {
            UpgradeDataWrapper wrapper = JsonUtility.FromJson<UpgradeDataWrapper>(jsonFile.text);
            upgradeList = wrapper.upgrades;
            Debug.Log("Geliştirme verileri JSON'dan başarıyla yüklendi.");
        }
        else
        {
            Debug.LogError("upgrades.json dosyası Resources klasöründe bulunamadı!");
        }
    }

    private void InitializeUpgrades()
    {
        for (int i = 0; i < upgradeList.Count; i++)
        {
            int index = i;
            UpgradeItem item = upgradeList[i];

            if (item.isPurchased) continue;

            GameObject newBtnObj = Instantiate(upgradeButtonPrefab, upgradeContent);

            item.buttonText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            item.buttonComponent = newBtnObj.GetComponent<Button>();

            item.buttonComponent.onClick.AddListener(() => BuyUpgrade(index));

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

        if (!item.isPurchased && GameManager.Instance.SpendDoner(item.cost))
        {
            switch (item.type)
            {
                case UpgradeType.ClickPowerAdd:
                    GameManager.Instance.baseClickPower += item.effectAmount;
                    break;
                case UpgradeType.ClickPowerMultiplier:
                    GameManager.Instance.clickMultiplier *= item.effectAmount;
                    break;
                case UpgradeType.PassiveProductionAdd:
                    GameManager.Instance.basePassiveProduction += item.effectAmount;
                    break;
                case UpgradeType.PassiveProductionMultiplier:
                    GameManager.Instance.passiveMultiplier *= item.effectAmount;
                    break;
            }

            item.isPurchased = true;
            Destroy(item.buttonComponent.gameObject);
            GameManager.Instance.RecalculateStats();
        }
    }
}