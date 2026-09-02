using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIRainEffect : MonoBehaviour
{
    [Header("Yağmur Ayarları")]
    public GameObject rainPrefab;
    public RectTransform rainContainer;

    [Header("Zamanlama (Az az yağması için)")]
    public float minSpawnDelay = 1.5f;
    public float maxSpawnDelay = 3.5f;

    [Header("Düşüş Hızı ve Animasyon")]
    public float minFallDuration = 8f;
    public float maxFallDuration = 14f;
    
    [Tooltip("Düşerken kendi etrafında yavaşça dönsün mü?")]
    public bool addRotation = true; 

    [Header("EN SEYREK hal (oyunun basi)")]
    [Tooltip("Damlalar arasi bekleme. Buyuk = daha seyrek.")]
    public float slowMinSpawnDelay = 7f;
    public float slowMaxSpawnDelay = 15f;
    [Tooltip("Dusus suresi. Buyuk = daha agir.")]
    public float slowMinFallDuration = 12f;
    public float slowMaxFallDuration = 20f;

    [Header("EN YOGUN hal (oyunun sonu)")]
    public float fastMinSpawnDelay = 0.07f;
    public float fastMaxSpawnDelay = 0.16f;
    public float fastMinFallDuration = 1.4f;
    public float fastMaxFallDuration = 2.6f;

    [Tooltip("Ayni anda ekranda en fazla kac damla olsun (performans siniri).")]
    public int maxAlive = 90;

    int alive;

    void Awake()
    {
        // Sahnedeki eski degerler ne olursa olsun en seyrek halden basla
        SetIntensity(0f);
    }

    /// <summary>
    /// 0 = oyunun basi (tek tuk, agir), 1 = oyunun sonu (saganak).
    /// Bekleme suresi USSEL olarak kisalir - dogrusal lerp ile artis
    /// baslarda hic hissedilmiyor, cunku 15sn ile 7sn arasi goze ayni geliyor.
    /// </summary>
    public void SetIntensity(float t)
    {
        t = Mathf.Clamp01(t);
        minSpawnDelay   = Mathf.Lerp(Mathf.Log(slowMinSpawnDelay),   Mathf.Log(fastMinSpawnDelay),   t);
        maxSpawnDelay   = Mathf.Lerp(Mathf.Log(slowMaxSpawnDelay),   Mathf.Log(fastMaxSpawnDelay),   t);
        minFallDuration = Mathf.Lerp(Mathf.Log(slowMinFallDuration), Mathf.Log(fastMinFallDuration), t);
        maxFallDuration = Mathf.Lerp(Mathf.Log(slowMaxFallDuration), Mathf.Log(fastMaxFallDuration), t);
        minSpawnDelay   = Mathf.Exp(minSpawnDelay);
        maxSpawnDelay   = Mathf.Exp(maxSpawnDelay);
        minFallDuration = Mathf.Exp(minFallDuration);
        maxFallDuration = Mathf.Exp(maxFallDuration);
    }

    void Start()
    {
        StartCoroutine(RainLoop());
    }

    private IEnumerator RainLoop()
    {
        while (true)
        {
            SpawnRainDrop();
            
            float waitTime = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void SpawnRainDrop()
    {
        if (rainPrefab == null || rainContainer == null) return;
        if (alive >= maxAlive) return;          // performans siniri

        alive++;
        GameObject drop = Instantiate(rainPrefab, rainContainer);
        drop.transform.SetAsFirstSibling();
        RectTransform rect = drop.GetComponent<RectTransform>();

        float containerWidth = rainContainer.rect.width;
        float startX = Random.Range(-containerWidth / 2f, containerWidth / 2f);

        float startY = (rainContainer.rect.height / 2f) + 150f;
        rect.anchoredPosition = new Vector2(startX, startY);

        float randomScale = Random.Range(0.7f, 1.2f);
        rect.localScale = Vector3.one * randomScale;

        float endY = -(rainContainer.rect.height / 2f) - 150f;
        float fallSpeed = Random.Range(minFallDuration, maxFallDuration);

        rect.DOAnchorPosY(endY, fallSpeed).SetEase(Ease.Linear).OnComplete(() => 
        {
            alive--;
            drop.transform.DOKill();
            Destroy(drop);
        });

        if (addRotation)
        {
            float rotDuration = Random.Range(4f, 8f);
            int direction = Random.Range(0, 2) == 0 ? 1 : -1;
            rect.DORotate(new Vector3(0, 0, 360 * direction), rotDuration, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental)
                .SetEase(Ease.Linear);
        }
    }
}