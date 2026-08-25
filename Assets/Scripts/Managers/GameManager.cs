using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Oyun Verileri")]
    public double totalDoner = 0;
    public double clickPower = 1;
    public double productionPerSecond = 0;
    public float productionInterval = 1f;

    [Header("Prestige Sistemi (Melek Yatırımcı)")]
    public double lifetimeDoner = 0;
    public int goldenDoner = 0;         
    public int pendingGoldenDoner = 0;  
    public float prestigeBonus = 0.02f; // %2 AdCap mantığı (Her 1 puan = +%2 Genel Üretim)

    [HideInInspector] public double baseClickPower = 1;
    [HideInInspector] public double clickMultiplier = 1; 
    [HideInInspector] public double basePassiveProduction = 0;
    [HideInInspector] public double passiveMultiplier = 1;

    public UIManager uiManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        GameSaveData saveData = SaveManager.Instance.LoadGame();
        if (saveData != null)
        {
            totalDoner = saveData.totalDoner;
            lifetimeDoner = saveData.lifetimeDoner;
            goldenDoner = saveData.goldenDoner;
        }

        UpdatePrestigeCalculations();
        RecalculateStats();
        uiManager.UpdateTotalDonerText(totalDoner);
        StartCoroutine(AutoProductionLoop());
    }

    // YENİ: Para kazanma işlemlerini tek bir merkeze topladık
    public void AddDoner(double amount)
    {
        totalDoner += amount;
        lifetimeDoner += amount; 
        
        UpdatePrestigeCalculations();
        uiManager.UpdateTotalDonerText(totalDoner);
    }

    public void OnDonerClicked()
    {
        AddDoner(clickPower);
        uiManager.PlayClickFeedback(clickPower);
    }

    private IEnumerator AutoProductionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(productionInterval);
            if (productionPerSecond > 0)
            {
                AddDoner(productionPerSecond);
            }
        }
    }

    // ADCAP PRESTIGE FORMÜLÜ 
    private void UpdatePrestigeCalculations()
    {
        // 1 Milyar = 1 Altın Döner, 4 Milyar = 2 Altın Döner, 9 Milyar = 3 Altın Döner (Karekök Sistemi)
        double totalGoldenEarnedEver = System.Math.Floor(System.Math.Sqrt(lifetimeDoner / 1000000000f));
        
        pendingGoldenDoner = (int)totalGoldenEarnedEver - goldenDoner;
        if (pendingGoldenDoner < 0) pendingGoldenDoner = 0;

        // Arayüzdeki Melek miktarını canlı olarak güncelle
        if (uiManager != null) 
        {
            uiManager.UpdatePrestigeUI(goldenDoner, pendingGoldenDoner);
        }
    }

    public void RecalculateStats()
    {
        double workerProduction = 0;
        if (WorkerManager.Instance != null) 
        {
            workerProduction = WorkerManager.Instance.GetTotalProduction();
        }
        
        // Sahip olunan Altın Döner'in verdiği global çarpanı hesapla
        double globalPrestigeMultiplier = 1.0 + (goldenDoner * prestigeBonus);

        // Hem tıklama gücünü hem pasif üretimi bu devasa çarpanla genişlet
        clickPower = baseClickPower * clickMultiplier * globalPrestigeMultiplier;
        productionPerSecond = (basePassiveProduction + workerProduction) * passiveMultiplier * globalPrestigeMultiplier;
    }

    public bool SpendDoner(double amount)
    {
        if (totalDoner >= amount)
        {
            totalDoner -= amount;
            uiManager.UpdateTotalDonerText(totalDoner);
            return true; 
        }
        return false; 
    }

    // YENİ: OYUNCU RESET BUTONUNA BASTIĞINDA ÇALIŞIR
    public void PrestigeAscension()
    {
        if (pendingGoldenDoner > 0)
        {
            goldenDoner += pendingGoldenDoner; // Yeni puanları cebe at
            
            // İşçileri ve upgradeleri sıfırlayan özel kaydı çağır
            SaveManager.Instance.PrestigeSave(goldenDoner, lifetimeDoner);
            
            // Sahneyi baştan yükle
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            Debug.LogWarning("Reset atmak için henüz yeterli puanın yok! En az 1 Milyar kazanmalısın.");
        }
    }

    public void HardResetGame()
    {
        SaveManager.Instance.ClearSave();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}