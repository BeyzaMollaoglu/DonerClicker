using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prestij parasi (ALTIN MASA) otomatik bonus vermez, HARCANIR.
/// NOT: ekranda beliren "Altin Doner" bundan ayri bir seydir - o gecici odul verir.
///
/// Neden: otomatik "+%3 / puan" sistemi kacak yapiyordu. Bonus puana,
/// puan lifetime'a, lifetime da bonusa bagliydi - kapali bir dongu.
/// 8 saatlik turlarda puan 116 -> 3.7 milyona firliyor ve oyun bitiyordu.
/// Magazada maliyetler SABIT ve seviye tavanlari var, o yuzden toplam
/// kazanc ustten sinirli: kacak matematiksel olarak imkansiz.
/// </summary>
[System.Serializable]
public class PrestigeItem
{
    public string pname;
    public string pdesc;
    /// <summary>
    /// 0 global uretim carpani   1 tiklama carpani      2 baslangic isci seviyesi
    /// 3 cevrimdisi saat (+)     4 cevrimdisi carpani   5 Altin Doner bekleme carpani (&lt;1 = daha sik)
    /// 6 Altin Doner odul carpani 7 reklam boostu (+)   8 tiklama yuzdesi (+)
    /// 9 isci maliyet carpani (&lt;1 = ucuz)
    /// </summary>
    public int    type;
    public double effect;
    public int    baseCost;
    public float  costGrowth;
    public int    maxLevel;

    [HideInInspector] public int level = 0;
    [HideInInspector] public Button   buttonComponent;
    [HideInInspector] public CardView card;
}

[System.Serializable]
public class PrestigeDataWrapper { public List<PrestigeItem> prestige; }

public class PrestigeManager : MonoBehaviour
{
    public static PrestigeManager Instance;

    [Header("Magaza")]
    public GameObject cardPrefab;
    public Transform  content;

    [HideInInspector] public List<PrestigeItem> items;

    // ---------------------------------------------------------------- kurulum

    // Awake'te yukluyoruz cunku WorkerManager.Start baslangic seviyelerini
    // buradan soruyor ve Start sirasi Unity'de garanti degil.
    private void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(this); return; }

        TextAsset json = Resources.Load<TextAsset>("prestige");
        if (json != null) items = JsonUtility.FromJson<PrestigeDataWrapper>(json.text).prestige;
        if (items == null) items = new List<PrestigeItem>();

        GameSaveData s = SaveManager.LoadData();
        if (s != null && s.prestigeLevels != null && s.prestigeLevels.Count == items.Count)
            for (int i = 0; i < items.Count; i++) items[i].level = s.prestigeLevels[i];
    }

    private void Start()
    {
        if (cardPrefab != null && content != null)
            for (int i = 0; i < items.Count; i++) CreateCard(i);

        RefreshAll();
    }

    // ---------------------------------------------------------------- maliyet

    public static int CostOf(PrestigeItem it)
    {
        return (int)System.Math.Ceiling(it.baseCost * System.Math.Pow(it.costGrowth, it.level));
    }

    public bool IsMaxed(PrestigeItem it) => it.level >= it.maxLevel;

    // ---------------------------------------------------------------- etkiler

    double Mult(int type)
    {
        double m = 1.0;
        if (items != null)
            foreach (var it in items) if (it.type == type && it.level > 0)
                m *= System.Math.Pow(it.effect, it.level);
        return m;
    }

    double Sum(int type)
    {
        double s = 0.0;
        if (items != null)
            foreach (var it in items) if (it.type == type) s += it.effect * it.level;
        return s;
    }

    public static double GlobalMult()   => Instance == null ? 1.0 : Instance.Mult(0);
    public static double ClickMult()    => Instance == null ? 1.0 : Instance.Mult(1);
    public static int    StartLevels()  => Instance == null ? 0   : (int)Instance.Sum(2);
    public static float  OfflineHours() => Instance == null ? 0f  : (float)Instance.Sum(3);
    public static double OfflineMult()  => Instance == null ? 1.0 : Instance.Mult(4);
    public static float  GoldenDelay()  => Instance == null ? 1f  : (float)Instance.Mult(5);
    public static double GoldenReward() => Instance == null ? 1.0 : Instance.Mult(6);
    public static double AdBoostAdd()   => Instance == null ? 0.0 : Instance.Sum(7);
    public static double ClickPercent() => Instance == null ? 0.0 : Instance.Sum(8);
    public static double WorkerCost()   => Instance == null ? 1.0 : Instance.Mult(9);

    /// <summary>Kac isciye baslangic seviyesi verilecek.</summary>
    public const int START_WORKER_COUNT = 8;

    /// <summary>Kayitli tum seviyeler - SaveManager icin.</summary>
    public List<int> Levels()
    {
        var l = new List<int>();
        if (items != null) foreach (var it in items) l.Add(it.level);
        return l;
    }

    // ---------------------------------------------------------------- arayuz

    private void OnEnable() { LocalizationManager.OnLanguageChanged += OnLanguageUpdated; }
    private void OnDisable() { LocalizationManager.OnLanguageChanged -= OnLanguageUpdated; }
    
    private void OnLanguageUpdated()
    {
        RefreshAll(); // Dil değiştiğinde tüm kartların metinlerini anında baştan çizer
    }

    private void CreateCard(int index)
    {
        PrestigeItem it = items[index];
        GameObject obj = Instantiate(cardPrefab, content);
        it.buttonComponent = obj.GetComponent<Button>();
        it.card            = obj.GetComponent<CardView>();
        if (it.buttonComponent != null) it.buttonComponent.onClick.AddListener(() => Buy(index));
    }

    void UpdateCard(PrestigeItem it)
    {
        if (it.card == null) return;

        // JSON'dan gelen ismi ve açıklamayı çeviriciden geçiriyoruz
        string localizedName = LocalizationManager.Instance.GetLocalizedValue(it.pname);
        string localizedDesc = LocalizationManager.Instance.GetLocalizedValue(it.pdesc);

        if (IsMaxed(it))
        {
            string maxLevelStr = string.Format(LocalizationManager.Instance.GetLocalizedValue("pr_level_max"), it.level, it.maxLevel);
            string maxBadge = LocalizationManager.Instance.GetLocalizedValue("pr_maxed");
            
            it.card.Set(localizedName, maxLevelStr, localizedDesc, maxBadge);
            it.card.SetPurchased();
            if (it.buttonComponent != null) it.buttonComponent.interactable = false;
            return;
        }

        string sub = it.level > 0
            ? string.Format(LocalizationManager.Instance.GetLocalizedValue("pr_level_add"), it.level, it.maxLevel)
            : string.Format(LocalizationManager.Instance.GetLocalizedValue("pr_level_zero"), it.maxLevel);

        string costStr = string.Format(LocalizationManager.Instance.GetLocalizedValue("pr_cost"), CostOf(it));

        it.card.Set(localizedName, sub, localizedDesc, costStr);
        if (it.buttonComponent != null) it.buttonComponent.interactable = true;
    }

    /// <summary>Metinleri de tazeler - satin alma / prestij sonrasi.</summary>
    public void RefreshAll()
    {
        if (items == null) return;
        foreach (var it in items) UpdateCard(it);
        RefreshAffordability();
    }

    /// <summary>Sadece renk durumu - sik cagrilabilir, degisim yoksa is yapmaz.</summary>
    public void RefreshAffordability()
    {
        if (items == null || GameManager.Instance == null) return;
        int purse = GameManager.Instance.prestigePoints;
        foreach (var it in items)
            if (it.card != null && !IsMaxed(it)) it.card.SetAffordable(purse >= CostOf(it));
    }

    void Buy(int index)
    {
        PrestigeItem it = items[index];
        if (IsMaxed(it)) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        int cost = CostOf(it);
        if (gm.prestigePoints < cost) return;

        gm.prestigePoints -= cost;
        gm.prestigeSpent += cost;
        it.level++;

        gm.RecalculateStats();
        gm.RefreshPrestigeUI();
        RefreshAll();

        // Isci maliyetini dusuren kalem kartlari da tazelemeli
        if (it.type == 9 && WorkerManager.Instance != null)
        {
            foreach (var w in WorkerManager.Instance.workerList) WorkerManager.Instance.UpdateWorkerUI(w);
            WorkerManager.Instance.RefreshAffordability();
        }
        if (AdsManager.Instance != null) AdsManager.Instance.RefreshOffer();

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    private void OnDestroy()
    {
        if (items == null) return;
        foreach (var it in items)
            if (it.buttonComponent != null) it.buttonComponent.onClick.RemoveAllListeners();
    }
}
