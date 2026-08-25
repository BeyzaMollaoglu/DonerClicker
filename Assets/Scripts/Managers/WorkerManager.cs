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

    [HideInInspector] public int level = 0; 
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
        if (saveData != null && saveData.workerLevels.Count == workerList.Count)
        {
            for (int i = 0; i < workerList.Count; i++)
            {
                workerList[i].level = saveData.workerLevels[i];
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
        worker.currentCost = worker.baseCost * Mathf.Pow(1.15f, worker.level); 

        if (GameManager.Instance.SpendDoner(worker.currentCost))
        {
            worker.level++;
            UpdateWorkerUI(worker);
            GameManager.Instance.RecalculateStats();
        }
    }

    public void UpdateWorkerUI(WorkerItem worker)
    {
        worker.currentCost = worker.baseCost * Mathf.Pow(1.15f, worker.level);

        // Sadece 1 işçinin ne kadar ürettiğini buluyoruz
        double upgradeMultiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetWorkerMultiplier(worker.workerId) : 1.0;
        double boostPerWorker = worker.productionBoost * upgradeMultiplier;

        // YENİ: Oyuncunun ŞU ANKİ seviyesiyle aldığı toplam üretim
        double currentTotalBoost = worker.level * boostPerWorker;

        // YENİ: Oyuncu 1 TANE DAHA ALIRSA üretimin ulaşacağı yeni değer
        double nextTotalBoost = (worker.level + 1) * boostPerWorker;

        // Unity'nin Rich Text (Renk) özelliğini kullanarak o harika ok işaretini ekliyoruz
        worker.buttonText.text = $"{worker.workerName}\nSeviye: {worker.level}\nÜretim: {UIManager.FormatNumber(currentTotalBoost)}/sn <color=#00FF00>-> {UIManager.FormatNumber(nextTotalBoost)}/sn</color>\nFiyat: {UIManager.FormatNumber(worker.currentCost)} TL";
    }

    public double GetTotalProduction()
    {
        double total = 0;
        foreach (var w in workerList)
        {
            double upgradeMultiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetWorkerMultiplier(w.workerId) : 1.0;
            total += (w.productionBoost * w.level) * upgradeMultiplier;
        }
        return total;
    }
}