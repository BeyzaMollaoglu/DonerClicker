using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public double totalDoner;
    public List<int> workerCounts = new List<int>(); 
    public List<int> workerTiers = new List<int>();  
    public List<int> workerXPs = new List<int>();    
    
    public List<bool> upgradePurchased = new List<bool>(); // YENİ: Upgradeler tek seferlik oldu
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnApplicationQuit() => SaveGame();

    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();
        data.totalDoner = GameManager.Instance.totalDoner;

        WorkerManager workerMgr = FindAnyObjectByType<WorkerManager>();
        if (workerMgr != null)
        {
            foreach (var worker in workerMgr.workerList)
            {
                data.workerCounts.Add(worker.purchaseCount);
                data.workerTiers.Add(worker.tier);
                data.workerXPs.Add(worker.currentXP);
            }
        }

        UpgradeManager upgradeMgr = FindAnyObjectByType<UpgradeManager>();
        if (upgradeMgr != null)
        {
            foreach (var upgrade in upgradeMgr.upgradeList)
            {
                data.upgradePurchased.Add(upgrade.isPurchased);
            }
        }
        
        PlayerPrefs.SetString("DonerSave", JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public GameSaveData LoadGame()
    {
        if (PlayerPrefs.HasKey("DonerSave"))
        {
            return JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString("DonerSave"));
        }
        return null; 
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey("DonerSave");
    }
}