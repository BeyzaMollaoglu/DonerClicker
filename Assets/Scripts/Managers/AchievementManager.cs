using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AchievementItem
{
    public string id;   // KAYITTA saklanir - degistirilirse eski kayitlardaki
                        // o basarim kaybolur. Oyuncuya hic gorunmez.
    /// <summary>
    /// Kosul turu:
    ///  0 kariyer dilim      1 bu turdaki dilim    2 saniyelik uretim
    ///  3 toplam tiklama     4 belirli usta sv.    5 toplam usta sv.
    ///  6 alinan gelistirme  7 prestij sayisi      8 toplam Altin Sikke
    ///  9 Altin Doner        10 cevrimdisi toplam  11 oynama suresi (sn)
    /// 12 farkli usta cesidi
    /// 20-23 ozel (gizli) kosullar - bkz. SpecialMet()
    /// </summary>
    public int    kind;
    public double value;
    public int    worker = -1;      // kind 4 icin

    public string nameTr, nameEn, descTr, descEn;

    /// <summary>
    /// 1 = gizli. Acilana kadar listede "???" gorunur ve SOHRET BONUSU VERMEZ.
    /// Cookie Clicker'daki golge basarimlarin karsiligi.
    /// </summary>
    public int hidden;

    [System.NonSerialized] public bool unlocked;
}

[System.Serializable]
public class AchievementWrapper { public List<AchievementItem> achievements; }

/// <summary>
/// Basarimlar.
///
/// Cookie Clicker'daki mantik: az sayida KURALDAN cok sayida hedef uretiliyor
/// (kademeli merdivenler), ve her basarim rozet degil MEKANIK odul veriyor.
/// Burada odulun adi SOHRET: acilan her basarim kalici olarak uretimi
/// %0.4 artiriyor. Gizli basarimlar sohret vermez, sadece keyif icin.
///
/// Basarimlar prestijde SIFIRLANMAZ - kariyer boyudur.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    [Tooltip("Acilan her basarimin verdigi kalici uretim bonusu (0.004 = %0.4).")]
    public double famePerAchievement = 0.004;

    [Tooltip("Basarim acilinca ekranda beliren bildirim.")]
    public AchievementToast toast;

    [HideInInspector] public List<AchievementItem> list = new List<AchievementItem>();

    /// <summary>Acilan basarim sayisi degisti - panel ve rozet tazelensin.</summary>
    public static event System.Action OnChanged;

    float tick;
    float idleSeconds;          // gizli "sabirli musteri" icin
    long  lastSeenClicks;
    bool  ready;

    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(this); return; }

        TextAsset json = Resources.Load<TextAsset>("achievements");
        if (json != null)
        {
            var w = JsonUtility.FromJson<AchievementWrapper>(json.text);
            if (w != null && w.achievements != null) list = w.achievements;
        }

        // Kayittan acilmislari isaretle. Awake'te okuyoruz cunku GameManager
        // Start'ta uretimi hesaplarken sohret bonusunu hazir bulmali.
        var save = SaveManager.LoadData();
        if (save != null && save.achievements != null)
        {
            var set = new HashSet<string>(save.achievements);
            foreach (var a in list) if (set.Contains(a.id)) a.unlocked = true;
        }
    }

    void Start()
    {
        // Ilk degerlendirmede bildirim cikmasin: devam eden bir oyuncunun
        // kaydi yuklendiginde 40 tane toast ust uste binmesin.
        Evaluate(false);
        ready = true;
        if (OnChanged != null) OnChanged();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Bosta gecen sure (gizli basarim): dokunus olunca sifirlanir.
        if (gm.totalClicks != lastSeenClicks) { lastSeenClicks = gm.totalClicks; idleSeconds = 0f; }
        else idleSeconds += Time.deltaTime;

        tick += Time.deltaTime;
        if (tick < 0.5f) return;
        tick = 0f;
        Evaluate(ready);
    }

    // ------------------------------------------------------------ degerlendirme

    void Evaluate(bool announce)
    {
        bool changed = false;
        foreach (var a in list)
        {
            if (a.unlocked) continue;
            if (!Met(a)) continue;

            a.unlocked = true;
            changed = true;
            if (announce && toast != null) toast.Show(Name(a), Desc(a));
        }

        if (!changed) return;

        // Sohret uretimi carptigi icin yeniden hesaplanmali.
        if (GameManager.Instance != null) GameManager.Instance.RecalculateStats();
        if (SaveManager.Instance  != null) SaveManager.Instance.SaveGame();
        if (OnChanged != null) OnChanged();
    }

    bool Met(AchievementItem a)
    {
        var gm = GameManager.Instance;
        var wm = WorkerManager.Instance;
        var um = UpgradeManager.Instance;
        if (gm == null) return false;

        switch (a.kind)
        {
            case 0:  return gm.lifetimeDoner        >= a.value;
            case 1:  return gm.RunDoner             >= a.value;
            case 2:  return gm.productionPerSecond  >= a.value;
            case 3:  return gm.totalClicks          >= a.value;
            case 4:
                if (wm == null || wm.workerList == null) return false;
                if (a.worker < 0 || a.worker >= wm.workerList.Count) return false;
                return wm.workerList[a.worker].level >= a.value;
            case 5:  return wm != null && wm.TotalLevels() >= a.value;
            case 6:  return UpgradesBought(um)      >= a.value;
            case 7:  return gm.prestigeCount        >= a.value;
            case 8:  return gm.PrestigeEarnedTotal  >= a.value;
            case 9:  return gm.goldenCaught         >= a.value;
            case 10: return gm.offlineEarnedTotal   >= a.value;
            case 11: return gm.playSeconds          >= a.value;
            case 12: return DistinctWorkers(wm)     >= a.value;
        }
        return SpecialMet(a);
    }

    /// <summary>Gizli basarimlarin ozel kosullari.</summary>
    bool SpecialMet(AchievementItem a)
    {
        var gm = GameManager.Instance;
        var wm = WorkerManager.Instance;

        switch (a.kind)
        {
            // Hic dokunmadan ilk ustayi tut (cevrimdisi/olay geliriyle mumkun)
            case 20: return gm.totalClicks == 0 && DistinctWorkers(wm) >= 1;

            // Oyun acikken hic dokunmadan bekle
            case 21: return idleSeconds >= (float)a.value;

            // Gece 03.00 - 05.00 arasinda oynuyor
            case 22:
                int h = System.DateTime.Now.Hour;
                return h >= 3 && h < 5;

            // Gizli olmayan her basarimi ac
            case 23:
                foreach (var x in list)
                    if (x.hidden == 0 && !x.unlocked) return false;
                return true;
        }
        return false;
    }

    static int UpgradesBought(UpgradeManager um)
    {
        if (um == null || um.upgradeList == null) return 0;
        int n = 0;
        foreach (var u in um.upgradeList) if (u.isPurchased) n++;
        return n;
    }

    static int DistinctWorkers(WorkerManager wm)
    {
        if (wm == null || wm.workerList == null) return 0;
        int n = 0;
        foreach (var w in wm.workerList) if (w.level > 0) n++;
        return n;
    }

    // ------------------------------------------------------------- disari acik

    public int UnlockedCount()
    {
        int n = 0;
        foreach (var a in list) if (a.unlocked) n++;
        return n;
    }

    public int Total() { return list.Count; }

    /// <summary>Gizli basarimlar sohret vermez.</summary>
    public int ScoringCount()
    {
        int n = 0;
        foreach (var a in list) if (a.unlocked && a.hidden == 0) n++;
        return n;
    }

    /// <summary>Uretim carpani. GameManager.RecalculateStats bunu kullaniyor.</summary>
    public static double FameMultiplier()
    {
        if (Instance == null) return 1.0;
        return 1.0 + Instance.ScoringCount() * Instance.famePerAchievement;
    }

    /// <summary>Yuzde olarak sohret - arayuzde gosterilmek icin.</summary>
    public static double FamePercent() { return (FameMultiplier() - 1.0) * 100.0; }

    public List<string> UnlockedIds()
    {
        var l = new List<string>();
        foreach (var a in list) if (a.unlocked) l.Add(a.id);
        return l;
    }

    public bool IsTurkish()
    {
        return LocalizationManager.Instance == null
            || LocalizationManager.Instance.currentLanguage == LocalizationManager.Language.Turkish;
    }

    public string Name(AchievementItem a) { return IsTurkish() ? a.nameTr : a.nameEn; }
    public string Desc(AchievementItem a) { return IsTurkish() ? a.descTr : a.descEn; }
}
