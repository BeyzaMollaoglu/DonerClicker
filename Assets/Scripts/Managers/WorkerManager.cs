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

    const double GROWTH = 1.15;

    /// <summary>Kac tane birden alinacak: 1, 10 veya 100.</summary>
    [HideInInspector] public int buyAmount = 1;

    /// <summary>Mevcut seviyeden itibaren N seviyenin TOPLAM maliyeti (geometrik seri).</summary>
    public static double CostFor(WorkerItem w, int count)
    {
        double first = w.baseCost * System.Math.Pow(GROWTH, w.level) * PrestigeManager.WorkerCost();
        if (count <= 1) return first;
        return first * (System.Math.Pow(GROWTH, count) - 1) / (GROWTH - 1);
    }

    /// <summary>Tum iscilerin seviye toplami - toplam-seviye kilometre taslari icin.</summary>
    public int TotalLevels()
    {
        if (workerList == null) return 0;
        int s = 0;
        foreach (var w in workerList) s += w.level;
        return s;
    }

    /// <summary>
    /// Hic alinmamis (seviye 0) ama artik parasi yeten isci sayisi.
    /// "Yeni bir isci acildi" bildirimi icin - her seviye icin degil,
    /// sadece ilk kez alinabilir hale gelenler, yoksa rozet surekli yanar.
    /// </summary>
    public int NewAffordableCount()
    {
        if (workerList == null || GameManager.Instance == null) return 0;
        double money = GameManager.Instance.totalDoner;
        int n = 0;
        foreach (var w in workerList)
            if (w.level == 0 && money >= CostFor(w, 1)) n++;
        return n;
    }

    public void SetBuyAmount(int amount)
    {
        buyAmount = Mathf.Max(1, amount);
        if (workerList == null) return;
        foreach (var w in workerList) UpdateWorkerUI(w);
        RefreshAffordability();
    }

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
        else
        {
            // Yeni tur (ilk acilis ya da prestij sonrasi):
            // "Miras Kalan Tezgah" alinmissa ilk isciler bedava seviyeyle baslar.
            int free = PrestigeManager.StartLevels();
            if (free > 0)
            {
                int n = Mathf.Min(PrestigeManager.START_WORKER_COUNT, workerList.Count);
                for (int i = 0; i < n; i++) workerList[i].level = free;
            }
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

        // Kayittan gelen seviyeler upgrade kilitlerini acmis olabilir
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.RefreshUnlocks();
    }

    private void BuyWorker(int index)
    {
        WorkerItem worker = workerList[index];
        int n = Mathf.Max(1, buyAmount);
        worker.currentCost = CostFor(worker, n);

        if (GameManager.Instance.SpendDoner(worker.currentCost))
        {
            worker.level += n;
            UpdateWorkerUI(worker);
            GameManager.Instance.RecalculateStats();

            // Yeni seviye yeni upgrade kilidi acmis olabilir
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.RefreshUnlocks();
        }
    }

    public void UpdateWorkerUI(WorkerItem worker)
    {
        int n = Mathf.Max(1, buyAmount);
        worker.currentCost = CostFor(worker, n);

        double upgradeMultiplier = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetWorkerMultiplier(worker.workerId) : 1.0;
        double boostPerWorker    = worker.productionBoost * upgradeMultiplier;
        double currentTotalBoost = worker.level * boostPerWorker;
        double nextTotalBoost    = (worker.level + n) * boostPerWorker;

        if (worker.card != null)
        {
            string sub = n == 1
                ? $"Seviye {worker.level}"
                : $"Seviye {worker.level} <color=#9CB84A>+{n}</color>";

            worker.card.Set(
                worker.workerName,
                sub,
                $"{UIManager.FormatNumber(currentTotalBoost)}/sn   <color=#9CB84A>> {UIManager.FormatNumber(nextTotalBoost)}/sn</color>",
                $"{UIManager.FormatNumber(worker.currentCost)} dilim");
        }
        else if (worker.buttonText != null)
        {
            worker.buttonText.text = $"{worker.workerName}\nSeviye: {worker.level}\n" +
                $"Üretim: {UIManager.FormatNumber(currentTotalBoost)}/sn\n" +
                $"Fiyat: {UIManager.FormatNumber(worker.currentCost)} dilim";
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
