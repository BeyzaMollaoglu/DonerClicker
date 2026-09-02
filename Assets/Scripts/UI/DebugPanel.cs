using UnityEngine;

/// <summary>
/// Test paneli. Idle oyunu elle test etmek imkansiz - 8 saat beklemek gerekir.
/// Bu panel zaman atlamayi, para vermeyi ve seviye yuklemeyi saglar.
///
/// IMGUI kullaniyor, yani hicbir sahne objesi gerektirmez ve yayina cikarken
/// tek bir bool ile kapanir. Editor'de her zaman acik.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    [Tooltip("Build alinca da gorunsun mu? Yayinda KAPALI olmali.")]
    public bool showInBuild = false;

    bool open;
    Rect win = new Rect(16, 70, 360, 470);
    Vector2 scroll;

    bool Visible
    {
#if UNITY_EDITOR
        get { return true; }
#else
        get { return showInBuild; }
#endif
    }

    void OnGUI()
    {
        if (!Visible) return;

        GUI.depth = -1000;
        if (GUI.Button(new Rect(16, 16, 74, 44), open ? "KAPAT" : "TEST"))
            open = !open;

        if (open) win = GUI.Window(9911, win, Draw, "Test Paneli");
    }

    void AddMoney(double amount)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.AddDoner(amount);
    }

    void SkipTime(double seconds)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        AddMoney(gm.productionPerSecond * seconds);
    }

    void AddLevels(int n)
    {
        var wm = WorkerManager.Instance;
        if (wm == null || wm.workerList == null) return;
        foreach (var w in wm.workerList) w.level += n;
        foreach (var w in wm.workerList) wm.UpdateWorkerUI(w);
        if (GameManager.Instance != null) GameManager.Instance.RecalculateStats();
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.RefreshUnlocks();
        wm.RefreshAffordability();
    }

    void Draw(int id)
    {
        var gm = GameManager.Instance;
        var wm = WorkerManager.Instance;
        var um = UpgradeManager.Instance;

        scroll = GUILayout.BeginScrollView(scroll);

        if (gm != null)
        {
            GUILayout.Label($"dilim      : {UIManager.FormatNumber(gm.totalDoner)}");
            GUILayout.Label($"uretim     : {UIManager.FormatNumber(gm.productionPerSecond)}/sn");
            GUILayout.Label($"tik gucu   : {UIManager.FormatNumber(gm.clickPower)}");
            GUILayout.Label($"lifetime   : {UIManager.FormatNumber(gm.lifetimeDoner)}");
            GUILayout.Label($"Altin Masa: {gm.prestigePoints} kese / {gm.prestigeSpent} harcandi  (bekleyen {gm.pendingPrestige})");
            GUILayout.Label($"prestij carpani: x{PrestigeManager.GlobalMult():0.##}   tik x{PrestigeManager.ClickMult():0.##}");
            GUILayout.Label($"boost      : {(gm.BoostActive ? gm.boostMultiplier.ToString("0.#") + "x " + UIManager.ShortTime(gm.BoostSecondsLeft) : "yok")}");
        }
        if (wm != null) GUILayout.Label($"toplam sv  : {wm.TotalLevels()}");
        if (um != null && um.upgradeList != null)
        {
            int acik = 0, alinmis = 0;
            foreach (var u in um.upgradeList) { if (u.buttonComponent != null) acik++; if (u.isPurchased) alinmis++; }
            GUILayout.Label($"upgrade    : {alinmis} alindi / {acik} acik / {um.upgradeList.Count} toplam");
        }

        GUILayout.Space(8);
        GUILayout.Label("— ZAMAN ATLA (uretim kadar dilim ekler) —");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1 sa"))  SkipTime(3600);
        if (GUILayout.Button("8 sa"))  SkipTime(3600 * 8);
        if (GUILayout.Button("1 gun")) SkipTime(86400);
        if (GUILayout.Button("1 hafta")) SkipTime(86400 * 7);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.Label("— HIZLANDIR (Time.timeScale) —");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1x"))   Time.timeScale = 1f;
        if (GUILayout.Button("5x"))   Time.timeScale = 5f;
        if (GUILayout.Button("25x"))  Time.timeScale = 25f;
        if (GUILayout.Button("100x")) Time.timeScale = 100f;
        GUILayout.EndHorizontal();
        GUILayout.Label($"su an: {Time.timeScale:0.#}x");

        GUILayout.Space(6);
        GUILayout.Label("— SEVIYE —");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+5"))   AddLevels(5);
        if (GUILayout.Button("+25"))  AddLevels(25);
        if (GUILayout.Button("+100")) AddLevels(100);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.Label("— DIGER —");
        if (GUILayout.Button("Reklam boostu ver (3x / 4sa)") && gm != null)
            gm.GrantBoost(3.0, 4f);

        // prestigeSpent'i de dusuruyoruz ki "kazanilan toplam" degismesin
        // ve bekleyen puan yanlis hesaplanmasin.
        if (GUILayout.Button("+100 Altin Masa") && gm != null)
        {
            gm.prestigePoints += 100;
            gm.prestigeSpent -= 100;
            gm.RefreshPrestigeUI();
            if (PrestigeManager.Instance != null) PrestigeManager.Instance.RefreshAll();
        }

        if (GUILayout.Button("PRESTIJ AT (reset)") && gm != null)
        {
            Time.timeScale = 1f;
            gm.PrestigeAscension();
        }

        if (GUILayout.Button("Offline penceresini test et") && gm != null && gm.uiManager != null)
            gm.uiManager.ShowOfflineReward(gm.productionPerSecond * 3600 * 4, 3600 * 4, false);

        GUILayout.Space(6);
        GUI.color = new Color(1f, 0.6f, 0.5f);
        if (GUILayout.Button("KAYDI SIL ve yeniden basla") && SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearSave();
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
        GUI.color = Color.white;

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 22));
    }

    void OnDisable() { Time.timeScale = 1f; }
}
