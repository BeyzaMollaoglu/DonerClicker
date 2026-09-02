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

    [Header("Prestij: ALTIN MASA (kalici para)")]
    public double lifetimeDoner = 0;
    [Tooltip("Harcanabilir Altin Masa. Prestij magazasinda harcanir.")]
    public int    prestigePoints = 0;
    [Tooltip("Magazada harcanmis toplam Altin Masa - bekleyen puan hesabi icin.")]
    public int    prestigeSpent = 0;
    public int    pendingPrestige = 0;
    [Tooltip("Altin Masa = kupkok(lifetime / bu deger)")]
    public double prestigeDivisor = 1e12;

    [Header("Offline Kazanc")]
    [Tooltip("Oyuncu yokken en fazla kac saat uretim islesin.")]
    public float offlineCapHours = 8f;

    [Header("Reklam Boostu")]
    [Tooltip("Aktif boost carpani. 1 = boost yok.")]
    public double boostMultiplier = 1.0;
    [Tooltip("Boostun bitis zamani (Unix saniye, UTC).")]
    public long   boostEndsAtUnix = 0;

    [Header("Altin Doner Olayi (ekranda beliren, gecici odul)")]
    [Tooltip("Altin Donerden gelen gecici carpan. Reklam boostuyla CARPILIR.")]
    public double eventMultiplier = 1.0;
    public long   eventEndsAtUnix = 0;

    [HideInInspector] public double baseClickPower = 1;
    [HideInInspector] public double clickMultiplier = 1;
    [HideInInspector] public double basePassiveProduction = 0;
    [HideInInspector] public double passiveMultiplier = 1;
    [HideInInspector] public double clickPercentOfProduction = 0;

    public UIManager uiManager;

    bool  lastBoostState = false;
    float uiTick;

    public static long NowUnix() => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public bool   BoostActive      => boostMultiplier > 1.0 && NowUnix() < boostEndsAtUnix;
    public int    BoostSecondsLeft => BoostActive ? (int)(boostEndsAtUnix - NowUnix()) : 0;

    public bool   EventActive      => eventMultiplier > 1.0 && NowUnix() < eventEndsAtUnix;
    public int    EventSecondsLeft => EventActive ? (int)(eventEndsAtUnix - NowUnix()) : 0;

    /// <summary>Reklam boostu ve Altin Doner olayi carpilarak uygulanir.</summary>
    public double ActiveBoost => (BoostActive ? boostMultiplier : 1.0)
                               * (EventActive ? eventMultiplier : 1.0);

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
            prestigePoints     = saveData.prestigePoints;
            prestigeSpent     = saveData.prestigeSpent;
            boostMultiplier = saveData.boostMultiplier > 1.0 ? saveData.boostMultiplier : 1.0;
            boostEndsAtUnix = saveData.boostEndsAtUnix;
            lastTicks       = saveData.lastSaveTicks;
        }

        UpdatePrestigeCalculations();
        RecalculateStats();
        uiManager.UpdateTotalDonerText(totalDoner);
        lastBoostState = BoostActive;

        StartCoroutine(OfflineRoutine(lastTicks));
    }

    // Pasif uretim HER KAREDE birikir. Saniyede bir eklenirse sayac
    // saniyede bir ziplayip duruyor gibi gorunuyor; kareye yayinca akici oluyor.
    private void Update()
    {
        if (productionPerSecond > 0)
        {
            double gain = productionPerSecond * Time.deltaTime;
            totalDoner    += gain;
            lifetimeDoner += gain;
            if (uiManager != null) uiManager.UpdateTotalDonerText(totalDoner);
        }

        // Pahali isler her karede degil, saniyede ~5 kez
        uiTick += Time.deltaTime;
        if (uiTick < 0.2f) return;
        uiTick = 0f;

        bool boosted = BoostActive || EventActive;
        if (boosted != lastBoostState) { lastBoostState = boosted; RecalculateStats(); }

        UpdatePrestigeCalculations();
        RefreshShopUI();
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

        double capSeconds = (offlineCapHours + PrestigeManager.OfflineHours()) * 3600.0;
        bool   capped     = away > capSeconds;
        double counted    = System.Math.Min(away, capSeconds);
        double earned     = productionPerSecond * counted * PrestigeManager.OfflineMult();   // tam hizda calisir

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


    /// <summary>Prestij magazasi ve HUD sayaclarini tazeler.</summary>
    public void RefreshPrestigeUI()
    {
        if (uiManager != null) uiManager.UpdatePrestigeUI(prestigePoints, pendingPrestige);
        if (PrestigeManager.Instance != null) PrestigeManager.Instance.RefreshAffordability();
    }

    private void UpdatePrestigeCalculations()
    {
        // Kup kok: karekokten daha yavas buyur, boylece prestij gec oyunda da anlamli kalir
        double totalPrestigeEver = lifetimeDoner <= 0 ? 0
            : System.Math.Floor(System.Math.Pow(lifetimeDoner / prestigeDivisor, 1.0 / 3.0));
        // Kesedeki + harcanmis = bugune kadar kazanilan. Harcamak yeni puan kazandirmaz.
        pendingPrestige = (int)totalPrestigeEver - (prestigePoints + prestigeSpent);
        if (pendingPrestige < 0) pendingPrestige = 0;

        RefreshPrestigeUI();
    }

    public void RecalculateStats()
    {
        double workerProduction = 0;
        if (WorkerManager.Instance != null) workerProduction = WorkerManager.Instance.GetTotalProduction();

        // Prestij bonusu artik puan sayisindan degil, magazada ALINANLARDAN geliyor.
        double globalPrestigeMultiplier = PrestigeManager.GlobalMult();

        productionPerSecond = (basePassiveProduction + workerProduction)
                            * passiveMultiplier * globalPrestigeMultiplier * ActiveBoost;

        // Tiklama gucu = sabit taban (carpanlarla buyur) + uretimin bir yuzdesi.
        // YUZDE TERIMI clickMultiplier ILE CARPILMAZ - ikisi carpilirsa tek tiklama
        // saniyelik uretimin yuz binlerce katini verir ve oyun dakikalar icinde biter.
        clickPower = baseClickPower * clickMultiplier * PrestigeManager.ClickMult()
                   + productionPerSecond * (clickPercentOfProduction + PrestigeManager.ClickPercent());

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

    /// <summary>Altin Doner odulu: kisa sureli guclu carpan.</summary>
    public void GrantEventBoost(double multiplier, float seconds)
    {
        eventMultiplier = multiplier;
        eventEndsAtUnix = NowUnix() + (long)seconds;
        lastBoostState  = true;
        RecalculateStats();
    }

    /// <summary>Altin Doner odulu: pesin dilim (saniye cinsinden uretim).</summary>
    public void GrantInstantSeconds(double seconds)
    {
        AddDoner(productionPerSecond * seconds);
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
        if (pendingPrestige > 0)
        {
            prestigePoints += pendingPrestige;
            SaveManager.Instance.PrestigeSave();
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
