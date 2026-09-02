using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public double totalDoner;
    public double lifetimeDoner;
    public int goldenDoner;

    public List<int> workerLevels = new List<int>();
    public List<bool> upgradePurchased = new List<bool>();

    // Cikis zamani - offline kazanc icin (UTC tick)
    public long lastSaveTicks;

    // Reklam boostu - uygulama kapansa da devam etsin
    public double boostMultiplier;
    public long   boostEndsAtUnix;

    // Ekonomi degistiginde eski kayit gecersiz olsun
    public int version;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    /// <summary>workers.json / upgrades.json degistiginde artir - eski kayitlar temizlenir.</summary>
    public const int SAVE_VERSION = 2;

    [Tooltip("Kac saniyede bir otomatik kayit alinsin.")]
    public float autoSaveInterval = 20f;

    private float autoSaveTimer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        autoSaveTimer += Time.unscaledDeltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            SaveGame();
        }
    }

    // Mobilde asil guvenilir olan bu ikisi.
    // OnApplicationQuit Android'de cogu zaman hic calismaz.
    private void OnApplicationPause(bool paused) { if (paused) SaveGame(); }
    private void OnApplicationFocus(bool focused) { if (!focused) SaveGame(); }
    private void OnApplicationQuit() { SaveGame(); }

    public void SaveGame()
    {
        if (GameManager.Instance == null) return;   // sahne daha kurulmadiysa kaydetme

        GameSaveData data = new GameSaveData();

        data.totalDoner    = GameManager.Instance.totalDoner;
        data.lifetimeDoner = GameManager.Instance.lifetimeDoner;
        data.goldenDoner   = GameManager.Instance.goldenDoner;
        data.lastSaveTicks   = System.DateTime.UtcNow.Ticks;
        data.boostMultiplier = GameManager.Instance.boostMultiplier;
        data.boostEndsAtUnix = GameManager.Instance.boostEndsAtUnix;
        data.version         = SAVE_VERSION;

        if (WorkerManager.Instance != null && WorkerManager.Instance.workerList != null)
            foreach (var worker in WorkerManager.Instance.workerList)
                data.workerLevels.Add(worker.level);

        if (UpgradeManager.Instance != null && UpgradeManager.Instance.upgradeList != null)
            foreach (var upgrade in UpgradeManager.Instance.upgradeList)
                data.upgradePurchased.Add(upgrade.isPurchased);

        PlayerPrefs.SetString("DonerSave", JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public GameSaveData LoadGame()
    {
        if (!PlayerPrefs.HasKey("DonerSave")) return null;

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString("DonerSave"));
        if (data == null) return null;

        if (data.version != SAVE_VERSION)
        {
            Debug.LogWarning($"Eski kayit surumu ({data.version}), ekonomi degistigi icin sifirlaniyor.");
            ClearSave();
            return null;
        }
        return data;
    }

    public void PrestigeSave(int currentGoldenDoner, double currentLifetimeDoner)
    {
        GameSaveData data = new GameSaveData();
        data.lifetimeDoner = currentLifetimeDoner;
        data.goldenDoner   = currentGoldenDoner;
        data.totalDoner    = 0;
        data.lastSaveTicks = System.DateTime.UtcNow.Ticks;
        data.version       = SAVE_VERSION;

        PlayerPrefs.SetString("DonerSave", JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        Debug.Log("Prestige atildi, yeni kayit olusturuldu.");
    }

    public void ClearSave()
    {
        if (PlayerPrefs.HasKey("DonerSave"))
        {
            PlayerPrefs.DeleteKey("DonerSave");
            PlayerPrefs.Save();
            Debug.Log("Kayit tamamen silindi.");
        }
    }
}
