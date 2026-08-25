using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public double totalDoner;
    public List<bool> workerUnlocked = new List<bool>(); // YENİ: Sadece kilit durumunu tutuyoruz
    public List<int> workerTiers = new List<int>();  
    public List<int> workerXPs = new List<int>();    
    
    public List<int> upgradeLevels = new List<int>();       
    public List<int> workerUpgradeLevels = new List<int>(); 
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnApplicationQuit() 
    {
        SaveGame();
    }

    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();
        data.totalDoner = GameManager.Instance.totalDoner;

        WorkerManager workerMgr = FindAnyObjectByType<WorkerManager>();
        if (workerMgr != null)
        {
            foreach (var worker in workerMgr.workerList)
            {
                data.workerUnlocked.Add(worker.isUnlocked);
                data.workerTiers.Add(worker.tier);
                data.workerXPs.Add(worker.currentXP);
            }
        }

        UpgradeManager upgradeMgr = FindAnyObjectByType<UpgradeManager>();
        if (upgradeMgr != null)
        {
            foreach (var upgrade in upgradeMgr.upgradeList)
            {
                data.upgradeLevels.Add(upgrade.level);
            }
        }
        
        WorkerUpgradeManager workerUpgMgr = FindAnyObjectByType<WorkerUpgradeManager>();
        if (workerUpgMgr != null)
        {
            foreach (var upg in workerUpgMgr.upgradeList)
            {
                data.workerUpgradeLevels.Add(upg.level);
            }
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("DonerSave", json);
        PlayerPrefs.Save();
        
        Debug.Log("Oyun Kaydedildi: " + json);
    }

    public GameSaveData LoadGame()
    {
        if (PlayerPrefs.HasKey("DonerSave"))
        {
            string json = PlayerPrefs.GetString("DonerSave");
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        return null; 
    }

    public void ClearSave()
    {
        if (PlayerPrefs.HasKey("DonerSave"))
        {
            PlayerPrefs.DeleteKey("DonerSave");
            PlayerPrefs.Save();
            Debug.Log("Kayıt tamamen silindi!");
        }
    }
}