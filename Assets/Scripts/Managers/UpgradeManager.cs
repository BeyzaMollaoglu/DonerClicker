using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum UpgradeType
{
    ClickPowerAdd,
    ClickPowerMultiplier,
    PassiveProductionAdd,
    PassiveProductionMultiplier,
    SpecificWorkerMultiplier,
    /// <summary>Tiklama, saniyelik uretimin bu kadar yuzdesini de kazandirir (toplanir).</summary>
    ClickPercentOfProduction
}

[System.Serializable]
public class UpgradeItem
{
    public string upgradeName;
    public UpgradeType type;
    public double cost;
    public double effectAmount;
    public int targetWorkerIndex = -1;

    // Kilit: 0 = hemen acik, 1 = belirli iscinin seviyesi, 2 = tum iscilerin toplam seviyesi
    public int unlockKind = 0;
    public int unlockWorker = -1;
    public int unlockValue = 0;

    [HideInInspector] public bool isPurchased = false;
    [HideInInspector] public bool isNew    = false;   // kartta "YENI" etiketi dursun mu
    [HideInInspector] public bool isUnseen = false;   // sekme rozetinde sayilsin mi
    [HideInInspector] public Button buttonComponent;
    [HideInInspector] public TextMeshProUGUI buttonText;
    [HideInInspector] public CardView card;
}

[System.Serializable]
public class UpgradeDataWrapper { public List<UpgradeItem> upgrades; }

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
    public GameObject upgradeButtonPrefab;
    public Transform upgradeContent;
    [HideInInspector] public List<UpgradeItem> upgradeList;

    // Acilista zaten kilidi acik olanlar "yeni" sayilmamali.
    // Bu bayrak ilk kare bittikten sonra acilir.
    bool initialLoadDone = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        TextAsset jsonFile = Resources.Load<TextAsset>("upgrades");
        if (jsonFile != null) upgradeList = JsonUtility.FromJson<UpgradeDataWrapper>(jsonFile.text).upgrades;
        if (upgradeList == null) upgradeList = new List<UpgradeItem>();
    }

    void Start()
    {
        GameSaveData saveData = SaveManager.Instance.LoadGame();
        if (saveData != null && saveData.upgradePurchased.Count == upgradeList.Count)
        {
            for (int i = 0; i < upgradeList.Count; i++)
            {
                upgradeList[i].isPurchased = saveData.upgradePurchased[i];
                if (upgradeList[i].isPurchased) ApplyUpgradeEffect(upgradeList[i]);
            }
        }

        RefreshUnlocks();
        StartCoroutine(EndInitialLoad());

        GameManager.Instance.RecalculateStats();
        RefreshAffordability();
    }

    /// <summary>
    /// WorkerManager.Start ile bu Start'in sirasi Unity'de garanti degil.
    /// Bir kare bekleyip her ikisi de bittikten sonra bayraklari temizliyoruz,
    /// boylece kayittan gelen seviyelerin actigi upgrade'ler "yeni" gorunmuyor.
    /// </summary>
    private IEnumerator EndInitialLoad()
    {
        yield return null;

        RefreshUnlocks();                       // kayit yuklendikten sonra acilanlari da olustur
        foreach (var u in upgradeList) { u.isNew = false; u.isUnseen = false; }

        initialLoadDone = true;
        SortCards();
        RefreshAffordability();
    }

    /// <summary>Bu upgrade'in kilidi acildi mi?</summary>
    public bool IsUnlocked(UpgradeItem item)
    {
        var wm = WorkerManager.Instance;
        switch (item.unlockKind)
        {
            case 1:
                if (wm == null || wm.workerList == null) return false;
                if (item.unlockWorker < 0 || item.unlockWorker >= wm.workerList.Count) return false;
                return wm.workerList[item.unlockWorker].level >= item.unlockValue;
            case 2:
                if (wm == null) return false;
                return wm.TotalLevels() >= item.unlockValue;
            default:
                return true;
        }
    }

    /// <summary>
    /// Kilidi yeni acilan upgrade'ler icin kart olusturur.
    /// Tum listeyi bastan yaratmaz - 171 upgrade'in hepsini sahneye koymak
    /// gereksiz agir olurdu, kartlar acildikca dogar.
    /// </summary>
    public void RefreshUnlocks()
    {
        bool changed = false;
        for (int i = 0; i < upgradeList.Count; i++)
        {
            UpgradeItem item = upgradeList[i];
            if (item.buttonComponent != null) continue;   // karti zaten var
            if (item.isPurchased) continue;               // alinmis, gostermeye gerek yok
            if (!IsUnlocked(item)) continue;

            if (initialLoadDone) { item.isNew = true; item.isUnseen = true; }
            CreateCard(i);
            changed = true;
        }
        if (changed) { SortCards(); RefreshAffordability(); }
    }

    /// <summary>Oyuncunun SU AN parasinin yettigi, alinmamis gelistirme sayisi.</summary>
    public int AffordableCount()
    {
        if (upgradeList == null || GameManager.Instance == null) return 0;
        double money = GameManager.Instance.totalDoner;
        int n = 0;
        foreach (var u in upgradeList)
            if (!u.isPurchased && IsUnlocked(u) && money >= u.cost) n++;
        return n;
    }

    /// <summary>Gelistirmeler paneli acildi mi - rehber "gordu" saysin diye.</summary>
    [HideInInspector] public bool panelSeen = false;

    /// <summary>
    /// Sekme rozetinde gosterilecek sayi: kilidi acik, alinmamis ve
    /// (ya yeni acilmis ya da PARASI OLAN) gelistirmeler.
    /// Oyuncu paneli acmadan da "alabilecegim bir sey var" diye anlasin.
    /// </summary>
    public int AlertCount()
    {
        if (upgradeList == null || GameManager.Instance == null) return 0;
        double money = GameManager.Instance.totalDoner;
        int n = 0;
        foreach (var u in upgradeList)
        {
            if (u.isPurchased || !IsUnlocked(u)) continue;
            if (u.isUnseen || money >= u.cost) n++;
        }
        return n;
    }

    /// <summary>Oyuncunun henuz gormedigi, kilidi yeni acilmis upgrade sayisi.</summary>
    public int NewCount()
    {
        if (upgradeList == null) return 0;
        int n = 0;
        foreach (var u in upgradeList) if (u.isUnseen && !u.isPurchased) n++;
        return n;
    }

    /// <summary>
    /// Panel ACILDIGINDA cagrilir - sadece sekme rozetini sifirlar.
    /// "YENI" etiketleri durmaya devam eder, yoksa oyuncu bakmadan kaybolurlar.
    /// </summary>
    public void MarkAllSeen()
    {
        panelSeen = true;
        if (upgradeList == null) return;
        foreach (var u in upgradeList) u.isUnseen = false;
    }

    /// <summary>Panel KAPANDIGINDA cagrilir - artik goruldu, etiketleri kaldir.</summary>
    public void ClearNewLabels()
    {
        if (upgradeList == null) return;
        bool changed = false;
        foreach (var u in upgradeList)
            if (u.isNew) { u.isNew = false; changed = true; if (u.card != null) UpdateUpgradeUI(u); }
        if (changed) SortCards();
    }

    private void CreateCard(int index)
    {
        UpgradeItem item = upgradeList[index];
        GameObject obj = Instantiate(upgradeButtonPrefab, upgradeContent);
        item.buttonComponent = obj.GetComponent<Button>();
        item.card           = obj.GetComponent<CardView>();
        item.buttonText     = obj.GetComponentInChildren<TextMeshProUGUI>();
        item.buttonComponent.onClick.AddListener(() => BuyUpgrade(index));
        UpdateUpgradeUI(item);
    }

    /// <summary>Ucuzdan pahaliya sirala, alinmislari en alta at.</summary>
    private void SortCards()
    {
        var visible = new List<UpgradeItem>();
        foreach (var it in upgradeList) if (it.buttonComponent != null) visible.Add(it);
        visible.Sort((a, b) =>
        {
            if (a.isPurchased != b.isPurchased) return a.isPurchased ? 1 : -1;
            if (a.isNew != b.isNew) return a.isNew ? -1 : 1;      // yeniler en uste
            return a.cost.CompareTo(b.cost);
        });
        for (int i = 0; i < visible.Count; i++)
            visible[i].buttonComponent.transform.SetSiblingIndex(i);
    }

    private void OnEnable() { LocalizationManager.OnLanguageChanged += OnLanguageUpdated; }
    private void OnDisable() { LocalizationManager.OnLanguageChanged -= OnLanguageUpdated; }

    private void OnLanguageUpdated()
    {
        if (upgradeList == null) return;
        foreach (var u in upgradeList) if (u.card != null) UpdateUpgradeUI(u);
    }

    private string EffectLabel(UpgradeItem item)
    {
        if (item.type == UpgradeType.ClickPercentOfProduction)
            return string.Format(LocalizationManager.Instance.GetLocalizedValue("upg_effect_click_percent"), (item.effectAmount * 100).ToString("0.##"));
        string sym = (item.type == UpgradeType.ClickPowerAdd || item.type == UpgradeType.PassiveProductionAdd) ? "+" : "x";
        return string.Format(LocalizationManager.Instance.GetLocalizedValue("upg_effect_power"), sym, item.effectAmount.ToString("0.##"));
    }

    private string TargetLabel(UpgradeItem item)
    {
        if (item.type == UpgradeType.ClickPercentOfProduction || item.type == UpgradeType.ClickPowerMultiplier || item.type == UpgradeType.ClickPowerAdd)
            return LocalizationManager.Instance.GetLocalizedValue("upg_target_click");
        if (item.type == UpgradeType.PassiveProductionMultiplier)
            return LocalizationManager.Instance.GetLocalizedValue("upg_target_all");
        if (item.targetWorkerIndex < 0) return "";
        
        var wm = WorkerManager.Instance;
        // DEĞİŞİKLİK: Hedef işçinin adını çeviriciden geçirerek alıyoruz
        return (wm != null && wm.workerList != null && item.targetWorkerIndex < wm.workerList.Count) 
            ? LocalizationManager.Instance.GetLocalizedValue(wm.workerList[item.targetWorkerIndex].workerName) 
            : "";
    }

    private void UpdateUpgradeUI(UpgradeItem item)
    {
        if (item.card == null) return;
        
        // DEĞİŞİKLİK: Geliştirmenin (Upgrade) kendi adını çeviriciden geçiriyoruz
        string localizedUpgName = LocalizationManager.Instance.GetLocalizedValue(item.upgradeName);

        if (item.isPurchased)
        {
            item.card.Set(localizedUpgName, LocalizationManager.Instance.GetLocalizedValue("upg_purchased"), EffectLabel(item), LocalizationManager.Instance.GetLocalizedValue("upg_bought"));
            item.card.SetPurchased();
            item.buttonComponent.interactable = false;
        }
        else
        {
            string title = item.isNew 
                ? $"<color=#9CB84A>{LocalizationManager.Instance.GetLocalizedValue("upg_new")}</color>  {localizedUpgName}" 
                : localizedUpgName;
            
            string costStr = string.Format(LocalizationManager.Instance.GetLocalizedValue("upg_cost"), UIManager.FormatNumber(item.cost));
            
            item.card.Set(title, EffectLabel(item), TargetLabel(item), costStr);
            item.buttonComponent.interactable = true;
        }
    }

    private void BuyUpgrade(int index)
    {
        UpgradeItem item = upgradeList[index];
        if (item.isPurchased) return;

        if (GameManager.Instance.SpendDoner(item.cost))
        {
            item.isPurchased = true;
            ApplyUpgradeEffect(item);
            UpdateUpgradeUI(item);
            SortCards();
            GameManager.Instance.RecalculateStats();

            if (WorkerManager.Instance != null)
                foreach (var w in WorkerManager.Instance.workerList)
                    WorkerManager.Instance.UpdateWorkerUI(w);
        }
    }

    private void ApplyUpgradeEffect(UpgradeItem item)
    {
        switch (item.type)
        {
            case UpgradeType.ClickPowerAdd:               GameManager.Instance.baseClickPower       += item.effectAmount; break;
            case UpgradeType.ClickPowerMultiplier:        GameManager.Instance.clickMultiplier      *= item.effectAmount; break;
            case UpgradeType.PassiveProductionAdd:        GameManager.Instance.basePassiveProduction+= item.effectAmount; break;
            case UpgradeType.PassiveProductionMultiplier: GameManager.Instance.passiveMultiplier    *= item.effectAmount; break;
            case UpgradeType.ClickPercentOfProduction:    GameManager.Instance.clickPercentOfProduction += item.effectAmount; break;
        }
    }

    public void RefreshAffordability()
    {
        if (upgradeList == null || GameManager.Instance == null) return;
        double money = GameManager.Instance.totalDoner;
        foreach (var u in upgradeList)
            if (u.card != null && !u.isPurchased) u.card.SetAffordable(money >= u.cost);
    }

    public double GetWorkerMultiplier(int workerIndex)
    {
        double multiplier = 1.0;
        foreach (var upg in upgradeList)
            if (upg.isPurchased && upg.type == UpgradeType.SpecificWorkerMultiplier && upg.targetWorkerIndex == workerIndex)
                multiplier *= upg.effectAmount;
        return multiplier;
    }
}
