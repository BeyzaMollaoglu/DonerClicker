using UnityEngine;
using DG.Tweening;

/// <summary>
/// Ilk acilis rehberi.
///
/// Anlatim TutorialOverlay uzerinden yapiliyor:
///   - HOSGELDIN ve PRESTIJ adimlari MODAL (ortada butonlu kart),
///   - digerleri SPOTLIGHT (ekran kararir, sadece hedef aydinlik ve
///     tiklanabilir kalir, ok isareti oraya bakar).
///
/// Bir adim "sirasi gelmis" olabilir ama GOSTERILMEYEBILIR: gelistirme
/// ipucu parasi yetene kadar, prestij ipucu ilk hak dogana kadar sessiz
/// bekler. Boylece oyuncu bos bir panele yonlendirilmez.
///
/// Her adim oyuncu isi yapinca kendiliginden kapanir, sonra bir daha cikmaz.
/// </summary>
public class Onboarding : MonoBehaviour
{
    public const int STEP_WELCOME  = -1;  // hos geldin karti
    public const int STEP_TAP      = 0;   // donere dokun
    public const int STEP_WORKER   = 1;   // ilk isciyi al
    public const int STEP_PRODUCE  = 2;   // uretim basladi
    public const int STEP_UPGRADE  = 3;   // ilk gelistirmeyi al
    public const int STEP_PRESTIGE = 4;   // prestij anlatimi
    public const int STEP_DONE     = 99;

    [Header("Kaplama")]
    public TutorialOverlay overlay;

    [Header("Isik tutulacak hedefler")]
    public RectTransform donerTarget;
    public RectTransform workerTabTarget;
    public RectTransform upgradeTabTarget;
    public RectTransform counterTarget;

    [Header("Nabiz atacak sekme ikonlari")]
    public RectTransform workerTabIcon;
    public RectTransform upgradeTabIcon;
    public RectTransform prestigeTabIcon;

    [Header("Panel acikken gizle")]
    public UITabManager tabManager;

    [Header("Sure sinirlari (saniye)")]
    [Tooltip("Emniyet suresi: buton calismazsa bu kadar sonra kendiliginden gecer.")]
    public float produceSeconds = 45f;
    [Tooltip("Gelistirme / prestij ipucu ilgilenilmezse bu kadar sonra kapanir.")]
    public float ignoreSeconds = 45f;

    float tick;
    float stepAge;               // ipucu KAC SANIYEDIR EKRANDA (gizliyken islemez)
    int   ageStep = -99;         // stepAge hangi adima ait
    int   shownStep = -99;       // -1 gecerli bir adim oldugu icin baslangic -99
    int   shownRemain = -1;      // TAP adiminda yazilan "kalan dilim" degeri
    RectTransform pulsing;

    void OnEnable()  { LocalizationManager.OnLanguageChanged += OnLanguageUpdated; }
    void OnDisable() { LocalizationManager.OnLanguageChanged -= OnLanguageUpdated; }

    void Start() { Apply(); }

    void Update()
    {
        // ADIM DEGISTIYSE SAYAC SIFIRLANMALI.
        // Yoksa: oyuncu "ilk ustani tut" adiminda 45 saniyeden fazla oyalanip
        // sonra isciyi alirsa, eskimis stepAge yuzunden Advance() tek karede
        // URETIM -> GELISTIRME -> PRESTIJ -> BITTI diye ucup gidiyor ve isciyi
        // aldiktan sonra oyuncuya bir daha hicbir mesaj gosterilmiyordu.
        // Adim disaridan degistiyse (kart butonlari) yine sifirla.
        if (Step != ageStep) EnterStep(Step);

        if (shownStep != -99) stepAge += Time.unscaledDeltaTime;

        // Kalan dilim sayaci her karede tazelenir, 0.3sn'lik gecikme olmasin.
        if (shownStep == STEP_TAP) RefreshTapBody();

        tick += Time.unscaledDeltaTime;
        if (tick < 0.3f) return;
        tick = 0f;

        Advance();
        Apply();
    }

    /// <summary>
    /// "Kesmeye devam et - X dilim kaldi" yazisini gunceller. Oyuncu ayni yere
    /// 10-15 kere basacak, ilerledigini gormesi lazim.
    /// </summary>
    void RefreshTapBody()
    {
        int r = SlicesLeft();
        if (r == shownRemain) return;
        shownRemain = r;
        if (overlay != null) overlay.SetBody(string.Format(T("tut_tap_body"), r));
    }

    int SlicesLeft()
    {
        var gm = GameManager.Instance;
        var wm = WorkerManager.Instance;
        if (gm == null || wm == null || wm.workerList == null || wm.workerList.Count == 0) return 0;
        double need = WorkerManager.CostFor(wm.workerList[0], 1) - gm.totalDoner;
        if (need <= 0d) return 0;
        return (int)System.Math.Ceiling(need);
    }

    /// <summary>
    /// Yeni bir adima girildiginde cagrilir.
    ///
    /// Sure sayaci SIFIRLANMAZSA su oluyordu: oyuncu "ilk ustani tut"
    /// adiminda 45 saniyeden fazla oyalanip sonra isciyi aldiginda, eskimis
    /// stepAge yuzunden Advance() dongusu TEK karede
    ///   URETIM -> GELISTIRME -> PRESTIJ -> BITTI
    /// diye ucup gidiyor, oyuncuya isciden sonra hicbir mesaj gosterilmiyordu.
    ///
    /// panelSeen de burada sifirlaniyor; Apply() icinde yapmak yetmiyordu
    /// cunku gelistirme ipucu "parasi yetene kadar" hic gosterilmiyor, o
    /// yuzden Apply oraya hic girmiyor ve eski bir panel ziyareti yuzunden
    /// adim sessizce atlaniyordu.
    /// </summary>
    void EnterStep(int s)
    {
        ageStep = s;
        stepAge = 0f;
        if (s == STEP_UPGRADE && UpgradeManager.Instance != null)
            UpgradeManager.Instance.panelSeen = false;
    }

    int Step
    {
        get { return GameManager.Instance != null ? GameManager.Instance.tutorialStep : STEP_DONE; }
        set { if (GameManager.Instance != null) GameManager.Instance.tutorialStep = value; }
    }

    void Save() { if (SaveManager.Instance != null) SaveManager.Instance.SaveGame(); }

    string T(string key)
    {
        return LocalizationManager.Instance != null
             ? LocalizationManager.Instance.GetLocalizedValue(key) : key;
    }

    // ------------------------------------------------------------ ilerleme

    /// <summary>
    /// Dongu icinde: devam eden bir oyuncunun kaydi yuklendiginde butun
    /// adimlar TEK tikta gecilsin, ipucu bir an bile parlamasin.
    /// </summary>
    void Advance()
    {
        int before = Step;
        for (int i = 0; i < 8; i++)
        {
            int s = Step;
            AdvanceOnce();
            if (Step == s) break;
            EnterStep(Step);   // sure sayaci YENI adim icin sifirlanmali
        }
        if (Step != before) Save();
    }

    void AdvanceOnce()
    {
        var gm = GameManager.Instance;
        var wm = WorkerManager.Instance;
        var um = UpgradeManager.Instance;
        if (gm == null || wm == null || wm.workerList == null || wm.workerList.Count == 0) return;

        switch (Step)
        {
            case STEP_WELCOME:
                // "BASLA" butonu ilerletiyor. Ama devam eden bir oyuncuysa
                // (zaten isci almis) karti hic gostermeden gec.
                if (wm.TotalLevels() > 0 || gm.totalDoner > 0) Step = STEP_TAP;
                break;

            case STEP_TAP:
                if (gm.totalDoner >= WorkerManager.CostFor(wm.workerList[0], 1)) Step = STEP_WORKER;
                break;

            case STEP_WORKER:
                if (wm.TotalLevels() > 0) Step = STEP_PRODUCE;
                break;

            case STEP_PRODUCE:
                // Butonla geciliyor. Sure sadece emniyet: buton bir sekilde
                // calismazsa oyuncu karartmanin arkasinda kilitli kalmasin.
                if (stepAge > produceSeconds) Step = STEP_UPGRADE;
                break;

            case STEP_UPGRADE:
                if (um == null || um.upgradeList == null) { Step = STEP_PRESTIGE; break; }
                foreach (var u in um.upgradeList)
                    if (u.isPurchased) { Step = STEP_PRESTIGE; break; }

                // Gostermeye hazir degilken (parasi yetmiyorken) "gormezden
                // geldi" sayma - oyuncunun gorebildigi bir sey yok ki.
                if (ReadyToShow(STEP_UPGRADE))
                {
                    if (Step == STEP_UPGRADE && um.panelSeen)            Step = STEP_PRESTIGE;
                    if (Step == STEP_UPGRADE && stepAge > ignoreSeconds) Step = STEP_PRESTIGE;
                }
                break;

            case STEP_PRESTIGE:
                if (gm.prestigePoints + gm.prestigeSpent > 0)                     Step = STEP_DONE;
                else if (ReadyToShow(STEP_PRESTIGE) && stepAge > ignoreSeconds)    Step = STEP_DONE;
                break;
        }
    }

    /// <summary>Sirasi gelmis olsa bile bu adim SU AN gosterilmeli mi?</summary>
    bool ReadyToShow(int s)
    {
        switch (s)
        {
            case STEP_UPGRADE:
                var um = UpgradeManager.Instance;
                return um != null && um.AffordableCount() > 0;

            case STEP_PRESTIGE:
                var gm = GameManager.Instance;
                return gm != null && gm.pendingPrestige > 0;
        }
        return true;
    }

    // -------------------------------------------------------------- gosterim

    void Apply()
    {
        int s = Step;
        bool hidden = s == STEP_DONE
                   || (tabManager != null && tabManager.AnyPanelOpen)
                   || AchievementPanel.IsOpen
                   || StatsPanel.IsOpen
                   || !ReadyToShow(s);

        if (hidden)
        {
            if (overlay != null) overlay.Hide();
            StopPulse();
            shownStep = -99;
            return;
        }

        if (s == shownStep) return;
        shownStep   = s;
        stepAge     = 0f;
        shownRemain = -1;
        StopPulse();

        Draw(s);
    }

    void Draw(int s)
    {
        if (overlay == null) return;

        switch (s)
        {
            case STEP_WELCOME:
                overlay.ShowModal(T("tut_welcome_title"), T("tut_welcome_body"), T("tut_welcome_btn"),
                                  () => { Step = STEP_TAP; Save(); shownStep = -99; Apply(); });
                break;

            case STEP_TAP:
                shownRemain = SlicesLeft();
                overlay.ShowSpotlight(donerTarget, T("tut_tap_title"),
                                      string.Format(T("tut_tap_body"), shownRemain));
                break;

            case STEP_WORKER:
                overlay.ShowSpotlight(workerTabTarget, T("tut_worker_title"), T("tut_worker_body"));
                StartPulse(workerTabIcon);
                break;

            case STEP_PRODUCE:
                overlay.ShowSpotlight(counterTarget, T("tut_produce_title"), T("tut_produce_body"),
                                      T("tut_ok"), () => { Step = STEP_UPGRADE; Save(); shownStep = -99; Apply(); });
                break;

            case STEP_UPGRADE:
                overlay.ShowSpotlight(upgradeTabTarget, T("tut_upgrade_title"), T("tut_upgrade_body"));
                StartPulse(upgradeTabIcon);
                break;

            case STEP_PRESTIGE:
                overlay.ShowModal(T("tut_prestige_title"), T("tut_prestige_body"), T("tut_prestige_btn"),
                                  () => { Step = STEP_DONE; Save(); shownStep = -99; Apply(); });
                StartPulse(prestigeTabIcon);
                break;
        }
    }

    void OnLanguageUpdated()
    {
        if (shownStep != -99) Draw(shownStep);
    }

    // ----------------------------------------------------------------- nabiz

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

    void OnDestroy() { StopPulse(); }
}
