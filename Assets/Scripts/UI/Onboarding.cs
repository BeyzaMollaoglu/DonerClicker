using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Ilk acilis rehberi. Oyuncu ekranda bir doner gorup "bu ne" deyip
/// kapatmasin diye kucuk adimlarla elinden tutar, sonra bir daha gorunmez.
/// </summary>
public class Onboarding : MonoBehaviour
{
    public const int STEP_TAP      = 0;   // donere dokun
    public const int STEP_WORKER   = 1;   // ilk isciyi al
    public const int STEP_PRODUCE  = 2;   // uretim basladi, devam et
    public const int STEP_UPGRADE  = 3;   // ilk gelistirmeyi al
    public const int STEP_PRESTIGE = 4;   // ilk prestij aciklamasi
    public const int STEP_DONE     = 99;

    [Header("Sahne")]
    [Tooltip("Ipucu seridi (kapali baslar).")]
    public RectTransform tipRoot;
    public TextMeshProUGUI tipText;

    [Header("Nabiz atacak sekme ikonlari")]
    public RectTransform workerTabIcon;
    public RectTransform upgradeTabIcon;
    public RectTransform prestigeTabIcon;

    [Header("Panel acikken gizle")]
    public UITabManager tabManager;

    [Header("Sure sinirlari (saniye)")]
    public float produceSeconds = 12f;
    public float ignoreSeconds = 45f;

    float tick;
    float stepAge;                 
    int   shownStep = -1;
    RectTransform pulsing;

    private void OnEnable() { LocalizationManager.OnLanguageChanged += OnLanguageUpdated; }
    private void OnDisable() { LocalizationManager.OnLanguageChanged -= OnLanguageUpdated; }

    void Start()
    {
        if (tipRoot != null) tipRoot.gameObject.SetActive(false);
        Apply();
    }

    void Update()
    {
        if (shownStep >= 0) stepAge += Time.unscaledDeltaTime;

        tick += Time.unscaledDeltaTime;
        if (tick < 0.3f) return;
        tick = 0f;

        Advance();
        Apply();
    }

    int Step
    {
        get { return GameManager.Instance != null ? GameManager.Instance.tutorialStep : STEP_DONE; }
        set { if (GameManager.Instance != null) GameManager.Instance.tutorialStep = value; }
    }

    void Advance()
    {
        int before = Step;
        for (int i = 0; i < 6; i++)
        {
            int s = Step;
            AdvanceOnce();
            if (Step == s) break;
        }
        if (Step != before && SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    void AdvanceOnce()
    {
        var gm = GameManager.Instance;
        var wm = WorkerManager.Instance;
        var um = UpgradeManager.Instance;
        if (gm == null || wm == null || wm.workerList == null || wm.workerList.Count == 0) return;

        switch (Step)
        {
            case STEP_TAP:
                if (gm.totalDoner >= WorkerManager.CostFor(wm.workerList[0], 1)) Step = STEP_WORKER;
                break;
            case STEP_WORKER:
                if (wm.TotalLevels() > 0) Step = STEP_PRODUCE;
                break;
            case STEP_PRODUCE:
                if (um != null && um.AffordableCount() > 0) { Step = STEP_UPGRADE; break; }
                if (stepAge > produceSeconds) Step = STEP_UPGRADE;
                break;
            case STEP_UPGRADE:
                if (um == null || um.upgradeList == null) { Step = STEP_PRESTIGE; break; }
                foreach (var u in um.upgradeList)
                    if (u.isPurchased) { Step = STEP_PRESTIGE; break; }

                if (Step == STEP_UPGRADE && um.panelSeen) Step = STEP_PRESTIGE;
                if (Step == STEP_UPGRADE && stepAge > ignoreSeconds) Step = STEP_PRESTIGE;
                break;
            case STEP_PRESTIGE:
                if (gm.prestigePoints + gm.prestigeSpent > 0) Step = STEP_DONE;
                else if (stepAge > ignoreSeconds) Step = STEP_DONE;
                break;
        }
    }

    bool ReadyToShow(int s)
    {
        var gm = GameManager.Instance;
        switch (s)
        {
            case STEP_UPGRADE:
                var um = UpgradeManager.Instance;
                return um != null && um.AffordableCount() > 0;
            case STEP_PRESTIGE:
                return gm != null && gm.pendingPrestige > 0;
        }
        return true;
    }

    private void OnLanguageUpdated() 
    { 
        if (shownStep >= 0 && tipRoot != null && tipRoot.gameObject.activeSelf) 
        {
            ApplyTextForStep(shownStep);
        }
    }

    // İŞTE EKSİK OLAN APPLY METODU BURADA:
    void Apply()
    {
        int s = Step;
        bool hidden = s == STEP_DONE
                   || (tabManager != null && tabManager.AnyPanelOpen)
                   || !ReadyToShow(s);

        if (tipRoot != null && tipRoot.gameObject.activeSelf == hidden)
            tipRoot.gameObject.SetActive(!hidden);

        if (hidden) { StopPulse(); shownStep = -1; return; }
        if (s == shownStep) return;
        shownStep = s;
        stepAge   = 0f;

        StopPulse();

        if (s == STEP_UPGRADE && UpgradeManager.Instance != null)
            UpgradeManager.Instance.panelSeen = false;

        // Dil metotlarını çağırdığımız yer
        ApplyTextForStep(s);
    }

    private void ApplyTextForStep(int s)
    {
        switch (s)
        {
            case STEP_TAP:
                SetText(LocalizationManager.Instance.GetLocalizedValue("onb_tap"));
                break;
            case STEP_WORKER:
                SetText(LocalizationManager.Instance.GetLocalizedValue("onb_worker"));
                StartPulse(workerTabIcon);
                break;
            case STEP_PRODUCE:
                SetText(LocalizationManager.Instance.GetLocalizedValue("onb_produce"));
                break;
            case STEP_UPGRADE:
                SetText(LocalizationManager.Instance.GetLocalizedValue("onb_upgrade"));
                StartPulse(upgradeTabIcon);
                break;
            case STEP_PRESTIGE:
                SetText(LocalizationManager.Instance.GetLocalizedValue("onb_prestige"));
                StartPulse(prestigeTabIcon);
                break;
        }
    }

    void SetText(string t)
    {
        if (tipText != null) tipText.text = t;
        if (tipRoot == null) return;
        tipRoot.DOKill();
        tipRoot.localScale = Vector3.one * 0.9f;
        tipRoot.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    void StartPulse(RectTransform t)
    {
        if (t == null) return;
        pulsing = t;
        t.DOKill();
        t.localScale = Vector3.one;
        t.DOScale(1.14f, 0.55f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
    }

    void StopPulse()
    {
        if (pulsing == null) return;
        pulsing.DOKill();
        pulsing.localScale = Vector3.one;
        pulsing = null;
    }

    void OnDestroy()
    {
        StopPulse();
        if (tipRoot != null) tipRoot.DOKill();
    }
}