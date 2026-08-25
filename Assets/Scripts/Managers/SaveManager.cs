using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public double totalDoner;
    public List<int> workerLevels = new List<int>();
    // YENİ: Artık geliştirmelerin bool (alındı) durumunu değil, int (seviye) değerini tutuyoruz
    public List<int> upgradeLevels = new List<int>(); 
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
                data.workerLevels.Add(worker.level);
            }
        }

        UpgradeManager upgradeMgr = FindAnyObjectByType<UpgradeManager>();
        if (upgradeMgr != null)
        {
            // YENİ: Geliştirmelerin seviyelerini kaydediyoruz
            foreach (var upgrade in upgradeMgr.upgradeList)
            {
                data.upgradeLevels.Add(upgrade.level);
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
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log("Kayıt Bulundu ve Yüklendi!");
            return data;
        }
        
        Debug.Log("Daha önce kayıt yapılmamış, yeni oyun başlıyor.");
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