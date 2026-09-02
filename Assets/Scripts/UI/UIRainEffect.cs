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