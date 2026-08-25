using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class WorkerItem
{
    public int workerId;
    public string workerName;
    public double baseCost;
    public double productionBoost;
    public int basePointsToLevelUp;
    public float levelUpPointMultiplier;
    public double levelUpProductionBonus;

    [HideInInspector] public int purchaseCount = 0; 
    [HideInInspector] public int tier = 1;          
    [HideInInspector] public int currentXP = 0;     
    [HideInInspector] public double currentCost;
    
    [HideInInspector] public Button buttonComponent; 
    [HideInInspector] public TextMeshProUGUI buttonText;
}

[System.Serializable]
public class WorkerDataWrapper { public List<WorkerItem> workers; }

public class WorkerManager : MonoBehaviour
{
    public static WorkerManager Instance;

    public GameObject workerButtonPrefab;
    public Transform workerContent;
    [HideInInspector] public List<WorkerItem> workerList;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        TextAsset jsonFile = Resources.Load<TextAsset>("workers");
        if (jsonFile != null) workerList = JsonUtility.FromJson<WorkerDataWrapper>(jsonFile.text).workers;
    }

    void Start()
    {
        GameSaveData saveData = SaveManager.Instance.LoadGame();
        if (saveData != null && saveData.workerCounts.Count == workerList.Count)
        {
            for (int i = 0; i < workerList.Count; i++)
            {
                workerList[i].purchaseCount = saveData.workerCounts[i];
                workerList[i].tier = saveData.workerTiers[i];
                workerList[i].currentXP = saveData.workerXPs[i];
            }
        }

        for (int i = 0; i < workerList.Count; i++)
        {
            int index = i;
            WorkerItem worker = workerList[i];
            
            GameObject newBtnObj = Instantiate(workerButtonPrefab, workerContent);
            worker.buttonComponent = newBtnObj.GetComponent<Button>();
            worker.buttonText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            worker.buttonComponent.onClick.AddListener(() => BuyWorker(index));
            UpdateWorkerUI(worker);
        }
        GameManager.Instance.RecalculateStats();
    }

    private void BuyWorker(int index)
    {
        WorkerItem worker = workerList[index];
        worker.currentCost = worker.baseCost * Mathf.Pow(1.15f, worker.purchaseCount); 

        if (GameManager.Instance.SpendDoner(worker.currentCost))
        {
            worker.purchaseCount++;
            AddXP(index, 1); // HER SATIN ALIMDA 1 XP KAZANIR
        }
    }

    public void AddXP(int workerIndex, int xpAmount)
    {
        WorkerItem w = workerList[workerIndex];
        w.currentXP += xpAmount;

        while (true)
        {
            int requiredXP = Mathf.FloorToInt(w.basePointsToLevelUp * Mathf.Pow(w.levelUpPointMultiplier, w.tier - 1));
            if (w.currentXP >= requiredXP)
            {
                w.currentXP -= requiredXP;
                w.tier++;
            }
            else break;
        }

        UpdateWorkerUI(w);
        GameManager.Instance.RecalculateStats();
    }

    public void UpdateWorkerUI(WorkerItem worker)
    {
        worker.currentCost = worker.baseCost * Mathf.Pow(1.15f, worker.purchaseCount);
        int requiredXP = Mathf.FloorToInt(worker.basePointsToLevelUp * Mathf.Pow(worker.levelUpPointMultiplier, worker.tier - 1));
        
        double tierMultiplier = Mathf.Pow((float)worker.levelUpProductionBonus, worker.tier - 1);
        double upgradeMultiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetWorkerMultiplier(worker.workerId) : 1.0;
        double actualBoost = worker.productionBoost * tierMultiplier * upgradeMultiplier;

        worker.buttonText.text = $"{worker.workerName} (Adet: {worker.purchaseCount})\nSeviye: {worker.tier} (XP: {worker.currentXP}/{requiredXP})\nÜretim: +{actualBoost.ToString("F1")}/sn\nFiyat: {worker.currentCost.ToString("F0")} TL";
    }

    public double GetTotalProduction()
    {
        double total = 0;
        foreach (var w in workerList)
        {
            double tierMultiplier = Mathf.Pow((float)w.levelUpProductionBonus, w.tier - 1);
            double upgradeMultiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetWorkerMultiplier(w.workerId) : 1.0;
            total += (w.productionBoost * tierMultiplier * upgradeMultiplier) * w.purchaseCount;
        }
        return total;
    }
}