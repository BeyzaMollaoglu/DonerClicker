using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum UpgradeType
{
    ClickPowerAdd,
    ClickPowerMultiplier,
    PassiveProductionAdd,
    PassiveProductionMultiplier,
    SpecificWorkerMultiplier, 
    SpecificWorkerXPAdd       
}

[System.Serializable]
public class UpgradeItem
{
    public string upgradeName;
    public UpgradeType type;
    public double cost; 
    public double effectAmount;
    
    public int targetWorkerIndex = -1; 
    
    [HideInInspector] public bool isPurchased = false; 
    
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
    public static UpgradeManager Instance;

    [Header("Geliştirmeler (Upgrades)")]
    public GameObject upgradeButtonPrefab;
    public Transform upgradeContent;
    
    [HideInInspector] public List<UpgradeItem> upgradeList;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        LoadUpgradesFromJSON();
    }

    void Start()
    {
        GameSaveData saveData = SaveManager.Instance.LoadGame();
        if (saveData != null && saveData.upgradePurchased.Count == upgradeList.Count)
        {
            for (int i = 0; i < upgradeList.Count; i++)
            {
                upgradeList[i].isPurchased = saveData.upgradePurchased[i];
                
                if (upgradeList[i].isPurchased && upgradeList[i].type != UpgradeType.SpecificWorkerXPAdd)
                {
                    ApplyUpgradeEffect(upgradeList[i]);
                }
            }
        }

        for (int i = 0; i < upgradeList.Count; i++)
        {
            int index = i;
            UpgradeItem item = upgradeList[i];

            GameObject newBtnObj = Instantiate(upgradeButtonPrefab, upgradeContent);
            item.buttonText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            item.buttonComponent = newBtnObj.GetComponent<Button>();

            item.buttonComponent.onClick.AddListener(() => BuyUpgrade(index));
            
            UpdateUpgradeUI(item);
        }
        
        SortPurchasedUpgradesToBottom();
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

    private void UpdateUpgradeUI(UpgradeItem item)
    {
        if (item.isPurchased)
        {
            item.buttonText.text = $"<color=#808080><s>{item.upgradeName}</s>\n(ALINDI)</color>";
            item.buttonComponent.interactable = false;
        }
        else
        {
            string effectStr = item.type == UpgradeType.SpecificWorkerXPAdd ? $"+{item.effectAmount} XP" : $"Güç: {item.effectAmount}";
            item.buttonText.text = $"{item.upgradeName}\n{effectStr}\nMaliyet: {item.cost.ToString("F0")} TL";
            item.buttonComponent.interactable = true; 
        }
    }

    private void BuyUpgrade(int index)
    {
        UpgradeItem item = upgradeList[index];

        // İŞÇİ KONTROLÜ: Eğer bu yetenek spesifik bir işçiye aitse
        if (item.targetWorkerIndex != -1)
        {
            // İlgili işçinin satın alma adedi 0 ise işlemi durdur ve uyarı ver
            if (WorkerManager.Instance.workerList[item.targetWorkerIndex].purchaseCount == 0)
            {
                StartCoroutine(ShowWarningRoutine(item));
                return; 
            }
        }

        if (GameManager.Instance.SpendDoner(item.cost))
        {
            item.isPurchased = true; 
            
            if (item.type == UpgradeType.SpecificWorkerXPAdd)
            {
                WorkerManager.Instance.AddXP(item.targetWorkerIndex, (int)item.effectAmount);
            }
            else
            {
                ApplyUpgradeEffect(item);
            }
            
            UpdateUpgradeUI(item);
            item.buttonComponent.transform.SetAsLastSibling();
            GameManager.Instance.RecalculateStats();
            
            if (WorkerManager.Instance != null)
            {
                foreach (var w in WorkerManager.Instance.workerList) 
                    WorkerManager.Instance.UpdateWorkerUI(w);
            }
        }
    }

    // UYARI SİSTEMİ
    private IEnumerator ShowWarningRoutine(UpgradeItem item)
    {
        // Butonu geçici olarak tıklanamaz yapıp kırmızı uyarıyı yaz
        item.buttonComponent.interactable = false;
        item.buttonText.text = "<color=red>ÖNCE İŞÇİYİ SATIN AL!</color>";
        
        yield return new WaitForSeconds(1.5f);
        
        // Süre bitince arayüzü eski haline döndür
        UpdateUpgradeUI(item); 
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

    public double GetWorkerMultiplier(int workerIndex)
    {
        double multiplier = 1.0;
        foreach (var upg in upgradeList)
        {
            if (upg.isPurchased && upg.type == UpgradeType.SpecificWorkerMultiplier && upg.targetWorkerIndex == workerIndex)
            {
                multiplier *= upg.effectAmount;
            }
        }
        return multiplier;
    }

    private void SortPurchasedUpgradesToBottom()
    {
        foreach (var item in upgradeList)
        {
            if (item.isPurchased)
            {
                item.buttonComponent.transform.SetAsLastSibling();
            }
        }
    }
}