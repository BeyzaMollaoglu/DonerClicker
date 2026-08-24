using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class WorkerItem
{
    public string workerName;
    public double baseCost;
    public double productionBoost; 

    [HideInInspector] public int level = 0;
    [HideInInspector] public double currentCost;
    [HideInInspector] public Button buttonComponent;
    [HideInInspector] public TextMeshProUGUI buttonText;
}

// JSON'daki 'workers' dizisini tutacak sarmalayıcı
[System.Serializable]
public class WorkerDataWrapper
{
    public List<WorkerItem> workers;
}

public class WorkerManager : MonoBehaviour
{
    [Header("İşçi Sistemi (Workers)")]
    public GameObject workerButtonPrefab;
    public Transform workerContent;
    
    // Artık inspector'dan gizleyebiliriz çünkü JSON'dan dolacak
    [HideInInspector] public List<WorkerItem> workerList;

    void Start()
    {
        LoadWorkersFromJSON();
        InitializeWorkers();
    }

    private void LoadWorkersFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("workers");
        if (jsonFile != null)
        {
            WorkerDataWrapper wrapper = JsonUtility.FromJson<WorkerDataWrapper>(jsonFile.text);
            workerList = wrapper.workers;
            Debug.Log("İşçi verileri JSON'dan başarıyla yüklendi.");
        }
        else
        {
            Debug.LogError("workers.json dosyası Resources klasöründe bulunamadı!");
        }
    }

    private void InitializeWorkers()
    {
        for (int i = 0; i < workerList.Count; i++)
        {
            int index = i;
            WorkerItem worker = workerList[i];
            worker.currentCost = worker.baseCost; 

            GameObject newBtnObj = Instantiate(workerButtonPrefab, workerContent);
            worker.buttonText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            worker.buttonComponent = newBtnObj.GetComponent<Button>();

            worker.buttonComponent.onClick.AddListener(() => BuyWorker(index));

            UpdateWorkerUI(worker);
        }
    }

    private void UpdateWorkerUI(WorkerItem worker)
    {
        worker.buttonText.text = $"{worker.workerName} (Lvl {worker.level})\nÜretim: +{worker.productionBoost}/sn\nMaliyet: {worker.currentCost.ToString("F0")} TL";
    }

    private void BuyWorker(int index)
    {
        WorkerItem worker = workerList[index];

        if (GameManager.Instance.SpendDoner(worker.currentCost))
        {
            worker.level++;
            GameManager.Instance.basePassiveProduction += worker.productionBoost;
            worker.currentCost = worker.baseCost * Mathf.Pow(1.15f, worker.level);

            UpdateWorkerUI(worker);
            GameManager.Instance.RecalculateStats();
        }
    }
}