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
    [HideInInspector] public CardView card;
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
                workerList[i].level = saveData.workerLevels[i];
        }

        for (int i = 0; i < workerList.Count; i++)
        {
            int index = i;
            WorkerItem worker = workerList[i];

            GameObject newBtnObj = Instantiate(workerButtonPrefab, workerContent);
            worker.buttonComponent = newBtnObj.GetComponent<Button>();
            worker.card            = newBtnObj.GetComponent<CardView>();
            worker.buttonText      = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            worker.buttonComponent.onClick.AddListener(() => BuyWorker(index));
            UpdateWorkerUI(worker);
        }
        GameManager.Instance.RecalculateStats();
        RefreshAffordability();
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

        double upgradeMultiplier = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetWorkerMultiplier(worker.workerId) : 1.0;
        double boostPerWorker    = worker.productionBoost * upgradeMultiplier;
        double currentTotalBoost = worker.level * boostPerWorker;
        double nextTotalBoost    = (worker.level + 1) * boostPerWorker;

        if (worker.card != null)
        {
            worker.card.Set(
                worker.workerName,
                $"Seviye {worker.level}",
                $"{UIManager.FormatNumber(currentTotalBoost)}/sn   <color=#9CB84A>> {UIManager.FormatNumber(nextTotalBoost)}/sn</color>",
                $"{UIManager.FormatNumber(worker.currentCost)} TL");
        }
        else if (worker.buttonText != null)
        {
            worker.buttonText.text = $"{worker.workerName}\nSeviye: {worker.level}\n" +
                $"Üretim: {UIManager.FormatNumber(currentTotalBoost)}/sn\n" +
                $"Fiyat: {UIManager.FormatNumber(worker.currentCost)} TL";
        }
    }

    /// <summary>Para degistikce cagrilir. CardView durum degismediyse hicbir sey yapmaz.</summary>
    public void RefreshAffordability()
    {
        if (workerList == null || GameManager.Instance == null) return;
        double money = GameManager.Instance.totalDoner;
        foreach (var w in workerList)
            if (w.card != null) w.card.SetAffordable(money >= w.currentCost);
    }

    public double GetTotalProduction()
    {
        double total = 0;
        foreach (var w in workerList)
        {
            double upgradeMultiplier = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetWorkerMultiplier(w.workerId) : 1.0;
            total += (w.productionBoost * w.level) * upgradeMultiplier;
        }
        return total;
    }
}
