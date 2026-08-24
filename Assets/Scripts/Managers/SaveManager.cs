using UnityEngine;
using System.Collections.Generic;

// Kaydedilecek verilerin paketleneceği şablon sınıf
[System.Serializable]
public class GameSaveData
{
    public double totalDoner;
    public List<int> workerLevels = new List<int>();
    public List<bool> upgradePurchased = new List<bool>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // OYUNDAN ÇIKARKEN ÇALIŞIR
    private void OnApplicationQuit() 
    {
        SaveGame();
    }

    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();
        
        // 1. Ana Parayı Kaydet
        data.totalDoner = GameManager.Instance.totalDoner;

        // 2. İşçi Seviyelerini Kaydet
        WorkerManager workerMgr = FindAnyObjectByType<WorkerManager>();
        if (workerMgr != null)
        {
            foreach (var worker in workerMgr.workerList)
            {
                data.workerLevels.Add(worker.level);
            }
        }

        // 3. Geliştirme Durumlarını Kaydet
        UpgradeManager upgradeMgr = FindAnyObjectByType<UpgradeManager>();
        if (upgradeMgr != null)
        {
            foreach (var upgrade in upgradeMgr.upgradeList)
            {
                data.upgradePurchased.Add(upgrade.isPurchased);
            }
        }

        // Veriyi JSON formatına çevirip cihaza kaydet
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("DonerSave", json);
        PlayerPrefs.Save();
        
        Debug.Log("Oyun Kaydedildi: " + json);
    }

    // DİĞER MANAGER'LAR VERİLERİ OKURKEN BU FONKSİYONU ÇAĞIRACAK
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
        return null; // Kayıt yoksa boş dön
    }
}