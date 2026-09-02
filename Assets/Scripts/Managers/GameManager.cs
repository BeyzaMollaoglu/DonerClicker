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
    public float  productionInterval = 1f;

    [Header("Prestige Sistemi (Melek Yatirimci)")]
    public double lifetimeDoner = 0;
    public int    goldenDoner = 0;
    public int    pendingGoldenDoner = 0;
    public float  prestigeBonus = 0.02f;

    [Header("Offline Kazanc")]
    [Tooltip("Oyuncu yokken en fazla kac saat uretim islesin.")]
    public float offlineCapHours = 8f;

    [Header("Reklam Boostu")]
    [Tooltip("Aktif boost carpani. 1 = boost yok.")]
    public double boostMultiplier = 1.0;
    [Tooltip("Boostun bitis zamani (Unix saniye, UTC).")]
    public long   boostEndsAtUnix = 0;

    [HideInInspector] public double baseClickPower = 1;
    [HideInInspector] public double clickMultiplier = 1;
    [HideInInspector] public double basePassiveProduction = 0;
    [HideInInspector] public double passiveMultiplier = 1;

    public UIManager uiManager;

    bool lastBoostState = false;

    public static long NowUnix() => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public bool   BoostActive      => boostMultiplier > 1.0 && NowUnix() < boostEndsAtUnix;
    public double ActiveBoost      => BoostActive ? boostMultiplier : 1.0;
    public int    BoostSecondsLeft => BoostActive ? (int)(boostEndsAtUnix - NowUnix()) : 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        GameSaveData saveData = SaveManager.Instance.LoadGame();
        long lastTicks = 0;
        if (saveData != null)
        {
            totalDoner      = saveData.totalDoner;
            lifetimeDoner   = saveData.lifetimeDoner;
            goldenDoner     = saveData.goldenDoner;
            boostMultiplier = saveData.boostMultiplier > 1.0 ? saveData.boostMultiplier : 1.0;
            boostEndsAtUnix = saveData.boostEndsAtUnix;
            lastTicks       = saveData.lastSaveTicks;
        }

        UpdatePrestigeCalculations();
        RecalculateStats();
        uiManager.UpdateTotalDonerText(totalDoner);
        lastBoostState = BoostActive;

        StartCoroutine(OfflineRoutine(lastTicks));
        StartCoroutine(AutoProductionLoop());
    }

    // Isci ve gelistirme Start'lari bittikten SONRA calismali, yoksa
    // uretim hizi 0 hesaplanir. Bir kare beklemek bunu garanti eder.
    private IEnumerator OfflineRoutine(long lastTicks)
    {
        yield return null;
        RecalculateStats();

        if (lastTicks <= 0) yield break;

        System.DateTime last = new System.DateTime(lastTicks, System.DateTimeKind.Utc);
        double away = (System.DateTime.UtcNow - last).TotalSeconds;
        if (away < 60) yield break;                       // 1 dakikadan azsa rahatsiz etme

        double capSeconds = offlineCapHours * 3600.0;
        bool   capped     = away > capSeconds;
        double counted    = System.Math.Min(away, capSeconds);
        double earned     = productionPerSecond * counted;   // tam hizda calisir

        if (earned <= 0) yield break;

        AddDoner(earned);
        if (uiManager != null) uiManager.ShowOfflineReward(earned, counted, capped);
    }

    public void AddDoner(double amount)
    {
        totalDoner    += amount;
        lifetimeDoner += amount;

        UpdatePrestigeCalculations();
        uiManager.UpdateTotalDonerText(totalDoner);
        RefreshShopUI();
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

            // Boost bittiyse uretimi yeniden hesapla
            bool now = BoostActive;
            if (now != lastBoostState) { lastBoostState = now; RecalculateStats(); }

            if (productionPerSecond > 0) AddDoner(productionPerSecond * productionInterval);
        }
    }

    private void UpdatePrestigeCalculations()
    {
        double totalGoldenEarnedEver = System.Math.Floor(System.Math.Sqrt(lifetimeDoner / 1000000000f));
        pendingGoldenDoner = (int)totalGoldenEarnedEver - goldenDoner;
        if (pendingGoldenDoner < 0) pendingGoldenDoner = 0;

        if (uiManager != null) uiManager.UpdatePrestigeUI(goldenDoner, pendingGoldenDoner);
    }

    public void RecalculateStats()
    {
        double workerProduction = 0;
        if (WorkerManager.Instance != null) workerProduction = WorkerManager.Instance.GetTotalProduction();

        double globalPrestigeMultiplier = 1.0 + (goldenDoner * prestigeBonus);

        clickPower = baseClickPower * clickMultiplier * globalPrestigeMultiplier;
        productionPerSecond = (basePassiveProduction + workerProduction)
                            * passiveMultiplier * globalPrestigeMultiplier * ActiveBoost;

        if (uiManager != null) uiManager.UpdateRateText(productionPerSecond);
    }

    /// <summary>Reklam odulu: belirli sure boyunca pasif uretimi carpar.</summary>
    public void GrantBoost(double multiplier, float hours)
    {
        boostMultiplier = multiplier;
        boostEndsAtUnix = NowUnix() + (long)(hours * 3600f);
        lastBoostState  = true;
        RecalculateStats();
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    private void RefreshShopUI()
    {
        if (WorkerManager.Instance  != null) WorkerManager.Instance.RefreshAffordability();
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.RefreshAffordability();
    }

    public bool SpendDoner(double amount)
    {
        if (totalDoner >= amount)
        {
            totalDoner -= amount;
            uiManager.UpdateTotalDonerText(totalDoner);
            RefreshShopUI();
            return true;
        }
        return false;
    }

    public void PrestigeAscension()
    {
        if (pendingGoldenDoner > 0)
        {
            goldenDoner += pendingGoldenDoner;
            SaveManager.Instance.PrestigeSave(goldenDoner, lifetimeDoner);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            Debug.LogWarning("Reset atmak icin henuz yeterli puanin yok! En az 1 Milyar kazanmalisin.");
        }
    }

    public void HardResetGame()
    {
        SaveManager.Instance.ClearSave();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
