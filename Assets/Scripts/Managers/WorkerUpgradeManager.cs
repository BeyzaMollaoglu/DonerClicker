using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class WorkerUpgradeItem
{
    public string upgradeName;
    public int targetWorkerIndex;
    public double baseCost;
    public float costMultiplier;
    public double effectAmount;
    public int workerPointsGranted;

    [HideInInspector] public int level = 0;
    [HideInInspector] public double currentCost;
}

[System.Serializable]
public class WorkerUpgradeDataWrapper
{
    public List<WorkerUpgradeItem> workerUpgrades;
}

public class WorkerUpgradeManager : MonoBehaviour
{
    public static WorkerUpgradeManager Instance;

    [Header("Detay Paneli (UI) Ana Referanslar")]
    public GameObject worker_details_panel; 
    public Button worker_details_panel_close; 
    
    [Header("Yazılar (Texts)")]
    public TextMeshProUGUI txt_worker_levels; 
    public TextMeshProUGUI txt_worker_buffs;  
    
    [Header("Prefab ve Buton Dizilimi")]
    public GameObject prefab_worker_updates_button; 
    public Transform upgradesContainer; 

    [HideInInspector] public List<WorkerUpgradeItem> upgradeList;
    
    [HideInInspector] public int activeWorkerIndex = -1; 
    private List<int> currentWorkerUpgradeIndexes = new List<int>(); 
    
    private List<GameObject> spawnedButtons = new List<GameObject>(); 
    private List<TextMeshProUGUI> spawnedTexts = new List<TextMeshProUGUI>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        LoadFromJSON();
    }

    void Start()
    {
        GameSaveData saveData = SaveManager.Instance.LoadGame();
        if (saveData != null && saveData.workerUpgradeLevels.Count == upgradeList.Count)
        {
            for (int i = 0; i < upgradeList.Count; i++)
            {
                upgradeList[i].level = saveData.workerUpgradeLevels[i];
                upgradeList[i].currentCost = upgradeList[i].baseCost * Mathf.Pow(upgradeList[i].costMultiplier, upgradeList[i].level);
            }
        }
        else 
        {
            foreach(var upg in upgradeList) upg.currentCost = upg.baseCost;
        }

        if (worker_details_panel_close != null)
        {
            worker_details_panel_close.onClick.AddListener(CloseWorkerDetails);
        }
        
        worker_details_panel.SetActive(false); 
    }

    private void LoadFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("worker_upgrades");
        if (jsonFile != null)
        {
            WorkerUpgradeDataWrapper wrapper = JsonUtility.FromJson<WorkerUpgradeDataWrapper>(jsonFile.text);
            upgradeList = wrapper.workerUpgrades;
        }
    }

    public void OpenWorkerDetails(int workerIndex)
    {
        activeWorkerIndex = workerIndex;
        
        foreach (var btnObj in spawnedButtons)
        {
            Destroy(btnObj);
        }
        spawnedButtons.Clear();
        spawnedTexts.Clear();
        currentWorkerUpgradeIndexes.Clear();

        for (int i = 0; i < upgradeList.Count; i++)
        {
            if (upgradeList[i].targetWorkerIndex == workerIndex)
            {
                currentWorkerUpgradeIndexes.Add(i);
                int upgIndex = i; 

                GameObject newBtnObj = Instantiate(prefab_worker_updates_button, upgradesContainer);
                Button btnComp = newBtnObj.GetComponent<Button>();
                TextMeshProUGUI txtComp = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();

                btnComp.onClick.AddListener(() => BuyUpgrade(upgIndex));

                spawnedButtons.Add(newBtnObj);
                spawnedTexts.Add(txtComp);
            }
        }

        worker_details_panel.SetActive(true);
        RefreshPanelUI();
    }

    public void CloseWorkerDetails()
    {
        worker_details_panel.SetActive(false);
        activeWorkerIndex = -1;
    }

    public void RefreshPanelUI()
    {
        if (activeWorkerIndex == -1) return;

        WorkerItem worker = WorkerManager.Instance.workerList[activeWorkerIndex];
        
        int requiredXP = Mathf.FloorToInt(worker.basePointsToLevelUp * Mathf.Pow(worker.levelUpPointMultiplier, worker.tier - 1));
        
        if (txt_worker_levels != null)
        {
            txt_worker_levels.text = $"<color=#FFD700>{worker.workerName}</color>\nSeviye: {worker.tier}  |  XP: {worker.currentXP}/{requiredXP}";
        }
        
        if (txt_worker_buffs != null)
        {
            double currentBuff = GetWorkerBuffMultiplier(activeWorkerIndex);
            // Adet yazısını buradan kaldırdık, sadece buff'ı gösteriyoruz
            txt_worker_buffs.text = $"Güncel Buff Çarpanı: x{currentBuff.ToString("F2")}";
        }

        for (int i = 0; i < currentWorkerUpgradeIndexes.Count; i++)
        {
            WorkerUpgradeItem upg = upgradeList[currentWorkerUpgradeIndexes[i]];
            spawnedTexts[i].text = $"{upg.upgradeName} (Lvl {upg.level})\n+XP: {upg.workerPointsGranted} | Buff: x{upg.effectAmount}\nMaliyet: {upg.currentCost.ToString("F0")} TL";
        }
    }

    private void BuyUpgrade(int index)
    {
        WorkerUpgradeItem item = upgradeList[index];

        if (GameManager.Instance.SpendDoner(item.currentCost))
        {
            item.level++;
            item.currentCost = item.baseCost * Mathf.Pow(item.costMultiplier, item.level);

            WorkerManager.Instance.AddXP(item.targetWorkerIndex, item.workerPointsGranted);
            
            RefreshPanelUI(); 
            GameManager.Instance.RecalculateStats();
        }
    }
    
    public double GetWorkerBuffMultiplier(int workerIndex)
    {
        double multiplier = 1.0;
        foreach (var upg in upgradeList)
        {
            if (upg.targetWorkerIndex == workerIndex && upg.level > 0)
            {
                multiplier *= Mathf.Pow((float)upg.effectAmount, upg.level);
            }
        }
        return multiplier;
    }
}