using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIRainEffect : MonoBehaviour
{
    [Header("Yağmur Ayarları")]
    public GameObject rainPrefab;
    public RectTransform rainContainer;
    public Sprite[] donerSprites;

    [Header("Tıklama & Yağmur Yoğunluğu")]
    [Range(0f, 1f)]
    public float currentIntensity = 0f;
    [Tooltip("Her tıklamada yoğunluk ne kadar artsın?")]
    public float clickIncrement = 0.1f;
    [Tooltip("Tıklanmadığında saniyede yoğunluk ne kadar azalsın? (Eski haline dönmesi için)")]
    public float decayRate = 0.15f; 

    [Header("EN SAKİN HALİ (Oyun başı / Tıklanmıyorken)")]
    public float slowMinSpawnDelay = 0.4f; // Saniyede 1-2 tane dağınık düşer
    public float slowMaxSpawnDelay = 0.8f;
    public float slowMinFallDuration = 3.5f;
    public float slowMaxFallDuration = 5.0f;

    [Header("DÖNER YAĞMURU (Sürekli Tıklandığında)")]
    public float fastMinSpawnDelay = 0.01f; // Neredeyse beklemeden şelale gibi akar
    public float fastMaxSpawnDelay = 0.03f;
    public float fastMinFallDuration = 1.0f;
    public float fastMaxFallDuration = 2.0f;

    public bool addRotation = true;
    public int maxAlive = 300; // Şelale efekti için sınırı yükselttik

    private float minSpawnDelay;
    private float maxSpawnDelay;
    private float minFallDuration;
    private float maxFallDuration;

    private float spawnTimer;
    private int alive;

    void Awake()
    {
        currentIntensity = 0f;
        UpdateIntensityValues();
        spawnTimer = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    void Update()
    {
        // 1. TIKLAMA KONTROLÜ
        if (Input.GetMouseButtonDown(0))
        {
            currentIntensity += clickIncrement;
            currentIntensity = Mathf.Clamp01(currentIntensity);
            
            // Tıklama hissini artırmak için tıkladığın an anında 1-2 ekstra döner düşür
            SpawnRainDrop();
            if(currentIntensity > 0.5f) SpawnRainDrop(); 
        }

        // 2. YAVAŞÇA ESKİ SAKİN HALİNE DÖNME (Tıklanmıyorsa)
        if (currentIntensity > 0f)
        {
            currentIntensity -= decayRate * Time.deltaTime;
            currentIntensity = Mathf.Clamp01(currentIntensity);
        }

        // 3. DEĞERLERİ GÜNCELLE
        UpdateIntensityValues();

        // 4. ZAMANLAYICI İLE DÖNER ÜRETİMİ (Çok yüksek hızlara çıkabilmek için)
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnRainDrop();
            spawnTimer = Random.Range(minSpawnDelay, maxSpawnDelay);
        }
    }

    private void UpdateIntensityValues()
    {
        float t = currentIntensity;
        
        minSpawnDelay   = Mathf.Lerp(Mathf.Log(slowMinSpawnDelay),   Mathf.Log(fastMinSpawnDelay),   t);
        maxSpawnDelay   = Mathf.Lerp(Mathf.Log(slowMaxSpawnDelay),   Mathf.Log(fastMaxSpawnDelay),   t);
        minFallDuration = Mathf.Lerp(Mathf.Log(slowMinFallDuration), Mathf.Log(fastMinFallDuration), t);
        maxFallDuration = Mathf.Lerp(Mathf.Log(slowMaxFallDuration), Mathf.Log(fastMaxFallDuration), t);
        
        minSpawnDelay   = Mathf.Exp(minSpawnDelay);
        maxSpawnDelay   = Mathf.Exp(maxSpawnDelay);
        minFallDuration = Mathf.Exp(minFallDuration);
        maxFallDuration = Mathf.Exp(maxFallDuration);
    }

    // Diğer scriptlerin (DonerProgression.cs vb.) yağmur şiddetini 
    // dışarıdan ayarlayabilmesi için gerekli metod
    public void SetIntensity(float t)
    {
        currentIntensity = Mathf.Clamp01(t);
        UpdateIntensityValues();
    }

    private void SpawnRainDrop()
    {
        if (rainPrefab == null || rainContainer == null) return;
        if (alive >= maxAlive) return;

        alive++;
        GameObject drop = Instantiate(rainPrefab, rainContainer);
        drop.transform.SetAsFirstSibling();

        if (donerSprites.Length > 0)
        {
            Image dropImage = drop.GetComponent<Image>();
            if (dropImage != null)
            {
                dropImage.sprite = donerSprites[Random.Range(0, donerSprites.Length)];
            }
        }

        RectTransform rect = drop.GetComponent<RectTransform>();

        // Rastgele X pozisyonu (Dağınık düşmeyi sağlayan kısım)
        float containerWidth = rainContainer.rect.width;
        float startX = Random.Range(-containerWidth / 2f, containerWidth / 2f);
        
        // Yukarıdan başlat
        float startY = (rainContainer.rect.height / 2f) + 150f;
        rect.anchoredPosition = new Vector2(startX, startY);

        // Rastgele büyüklük
        rect.localScale = Vector3.one * Random.Range(0.6f, 1.3f);

        // Aşağı düşüş
        float endY = -(rainContainer.rect.height / 2f) - 150f;
        float fallSpeed = Random.Range(minFallDuration, maxFallDuration);

        rect.DOAnchorPosY(endY, fallSpeed).SetEase(Ease.Linear).OnComplete(() =>
        {
            alive--;
            drop.transform.DOKill();
            Destroy(drop);
        });

        // Dönme efekti
        if (addRotation)
        {
            float rotDuration = Random.Range(3f, 7f);
            int direction = Random.Range(0, 2) == 0 ? 1 : -1;
            rect.DORotate(new Vector3(0, 0, 360 * direction), rotDuration, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental)
                .SetEase(Ease.Linear);
        }
    }
}