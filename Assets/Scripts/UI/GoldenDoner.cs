using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Rastgele beliren Altin Doner. Tiklanirsa odul verir.
/// Turun en guclu "ekrana geri bak" mekanigi - Cookie Clicker'in altin kurabiyesi.
/// </summary>
public class GoldenDoner : MonoBehaviour
{
    [Header("Zamanlama (saniye)")]
    public float minDelay = 180f;
    public float maxDelay = 420f;
    [Tooltip("Ekranda kac saniye kalsin.")]
    public float lifeTime = 13f;

    [Header("Odul")]
    [Tooltip("Uretim firtinasi carpani.")]
    public double stormMultiplier = 7.0;
    public float  stormSeconds    = 60f;
    [Tooltip("Pesin odul: kac saniyelik uretim.")]
    public float  instantSeconds  = 900f;

    [Header("Sahne")]
    public RectTransform spawnArea;     // genelde main_container
    public RectTransform golden;        // altin doner objesi (kapali baslar)
    public Button        goldenButton;

    float timer;
    bool  visible;

    void Start()
    {
        if (golden != null) golden.gameObject.SetActive(false);
        if (goldenButton != null) goldenButton.onClick.AddListener(Collect);
        ResetTimer();
    }

    void ResetTimer() { timer = Random.Range(minDelay, maxDelay) * PrestigeManager.GoldenDelay(); }

    void Update()
    {
        if (visible) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) Spawn();
    }

    void Spawn()
    {
        if (golden == null || spawnArea == null) { ResetTimer(); return; }

        // Ekranin ortasindaki guvenli alana yerlestir (HUD ve tab bar disina denk gelmesin)
        Rect area = spawnArea.rect;
        float x = Random.Range(-area.width  * 0.34f, area.width  * 0.34f);
        float y = Random.Range(-area.height * 0.18f, area.height * 0.26f);
        golden.anchoredPosition = new Vector2(x, y);

        visible = true;
        golden.gameObject.SetActive(true);
        golden.localScale = Vector3.zero;
        golden.DOKill();
        golden.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
        golden.DORotate(new Vector3(0, 0, 8f), 1.1f)
              .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetId(this);

        DOVirtual.DelayedCall(lifeTime, Expire, false).SetId(this);
    }

    void Expire()
    {
        if (!visible) return;
        Hide();
    }

    void Collect()
    {
        if (!visible) return;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            // Iki odulden biri: kisa sureli firtina ya da pesin dilim
            // "Comert Usta" prestij kalemi odulu buyutur: firtina uzar, pesin odul artar
            double rw = PrestigeManager.GoldenReward();

            if (Random.value < 0.5f)
            {
                float secs = (float)(stormSeconds * rw);
                gm.GrantEventBoost(stormMultiplier, secs);
                ShowText($"<color=#F0B441>×{stormMultiplier:0.#} ÜRETİM!</color>\n{secs:0} saniye");
            }
            else
            {
                double secs = instantSeconds * rw;
                gm.GrantInstantSeconds(secs);
                ShowText($"<color=#F0B441>+{UIManager.FormatNumber(gm.productionPerSecond * secs)}</color>\ndilim");
            }
        }
        Hide();
    }

    void ShowText(string msg)
    {
        var ui = GameManager.Instance != null ? GameManager.Instance.uiManager : null;
        if (ui != null) ui.ShowEventToast(msg, golden.anchoredPosition);
    }

    void Hide()
    {
        visible = false;
        DOTween.Kill(this);
        if (golden == null) return;
        golden.DOKill();
        golden.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack)
              .OnComplete(() => golden.gameObject.SetActive(false));
        ResetTimer();
    }

    void OnDestroy()
    {
        DOTween.Kill(this);
        if (goldenButton != null) goldenButton.onClick.RemoveAllListeners();
    }
}
