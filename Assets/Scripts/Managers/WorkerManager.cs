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

    [HideInInspector] public bool isUnlocked = false; 
    [HideInInspector] public int tier = 1;          
    [HideInInspector] public int currentXP = 0;     
    
    [HideInInspector] public Button buttonComponent; 
    [HideInInspector] public TextMeshProUGUI buttonText;
    [HideInInspector] public Button detailsButtonComponent; 
}

[System.Serializable]
public class WorkerDataWrapper
{
    public List<WorkerItem> workers;
}

public class WorkerManager : MonoBehaviour
{
    public static WorkerManager Instance;

    [Header("İşçi Sistemi (Workers)")]
    public GameObject workerButtonPrefab;
    public Transform workerContent;
    
    [HideInInspector] public List<WorkerItem> workerList;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        LoadWorkersFromJSON(); 
    }

    void Start()
    {
        GameSaveData saveData = SaveManager.Instance.LoadGame();
        if (saveData != null && saveData.workerUnlocked.Count == workerList.Count)
        {
            for (int i = 0; i < workerList.Count; i++)
            {
                workerList[i].isUnlocked = saveData.workerUnlocked[i];
                workerList[i].tier = saveData.workerTiers[i];
                workerList[i].currentXP = saveData.workerXPs[i];
            }
        }

        InitializeWorkers();
        GameManager.Instance.RecalculateStats();
    }

    private void LoadWorkersFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("workers");
        if (jsonFile != null)
        {
            WorkerDataWrapper wrapper = JsonUtility.FromJson<WorkerDataWrapper>(jsonFile.text);
            workerList = wrapper.workers;
        }
    }

    private void InitializeWorkers()
    {
        for (int i = 0; i < workerList.Count; i++)
        {
            int index = i;
            WorkerItem worker = workerList[i];

            GameObject newBtnObj = Instantiate(workerButtonPrefab, workerContent);
            
            worker.buttonComponent = newBtnObj.GetComponent<Button>();
            worker.buttonText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // ÇOK ÖNEMLİ: Kod, prefabın hemen altındaki "Btn_Details" isimli objeyi arar.
            Transform detailsTransform = newBtnObj.transform.Find("Btn_Details");
            if (detailsTransform != null)
            {
                worker.detailsButtonComponent = detailsTransform.GetComponent<Button>();
                worker.detailsButtonComponent.onClick.AddListener(() => 
                {
                    WorkerUpgradeManager.Instance.OpenWorkerDetails(index);
                });
            }

            worker.buttonComponent.onClick.AddListener(() => OnWorkerButtonClicked(index));

            UpdateWorkerUI(worker);
        }
    }

    private void OnWorkerButtonClicked(int index)
    {
        WorkerItem worker = workerList[index];

        if (!worker.isUnlocked) 
        {
            if (GameManager.Instance.SpendDoner(worker.baseCost))
            {
                worker.isUnlocked = true;
                UpdateWorkerUI(worker);
                
                // SATIN ALINDIĞI AN BUFF UYGULANIR: 
                // RecalculateStats() çağrıldığında aşağıdaki GetTotalProduction() çalışır
                // ve işçinin üretimi toplam (global) üretime eklenir.
                GameManager.Instance.RecalculateStats();
                
                WorkerUpgradeManager.Instance.OpenWorkerDetails(index);
            }
        }
        else
        {
            // Zaten açıksa ana butona basılsa da detayları açar
            WorkerUpgradeManager.Instance.OpenWorkerDetails(index);
        }
    }

    // ARAYÜZ YÖNETİMİ: TAM İSTEDİĞİN GİBİ SADELEŞTİRİLDİ
    public void UpdateWorkerUI(WorkerItem worker)
    {
        if (!worker.isUnlocked)
        {
            // 1. Kilitliyken: Adı, Fiyatı ve Vereceği Buff Yazar
            worker.buttonText.text = $"{worker.workerName}\nÜretim: +{worker.productionBoost.ToString("F1")}/sn\nFiyat: {worker.baseCost.ToString("F0")} TL";
            
            // 2. Detay Butonu GİZLENİR
            if (worker.detailsButtonComponent != null) worker.detailsButtonComponent.gameObject.SetActive(false);
        }
        else
        {
            // 1. Kilidi açılınca (Satın alınınca): Bütün diğer yazılar silinir, SADECE ADI YAZAR
            worker.buttonText.text = worker.workerName;
            
            // 2. Detay Butonu GÖRÜNÜR OLUR
            if (worker.detailsButtonComponent != null) worker.detailsButtonComponent.gameObject.SetActive(true);
        }
    }

    // İŞÇİ ÜRETİMİ HESAPLAMASI
    public double GetTotalProduction()
    {
        double total = 0;
        foreach (var w in workerList)
        {
            if (w.isUnlocked) // İşçi satın alındıysa (kilidi açıksa) üretimi toplam güce eklenir
            {
                double tierMultiplier = Mathf.Pow((float)w.levelUpProductionBonus, w.tier - 1);
                double buffMultiplier = WorkerUpgradeManager.Instance != null ? WorkerUpgradeManager.Instance.GetWorkerBuffMultiplier(w.workerId) : 1.0;
                
                total += w.productionBoost * tierMultiplier * buffMultiplier;
            }
        }
        return total;
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
            else
            {
                break;
            }
        }

        UpdateWorkerUI(w);
        GameManager.Instance.RecalculateStats();
    }
}