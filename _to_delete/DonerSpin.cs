using UnityEngine;
using DG.Tweening;

/// <summary>
/// Doneri donuyormus gibi gosterir.
///
/// Olcek DEGISTIRMEZ - donen bir silindirin dis hatti degismez, o yuzden
/// eni daraltip acmak "nefes alma" gibi gorunur. Bunun yerine etin uzerinde
/// yatay kayan bir isik/golge bandi gezdirir; goz bunu donen yuzey olarak okur.
///
/// Band, etin Image'ina eklenen Mask sayesinde et silueti disina tasmaz.
/// Band genisligi etin 2 kati ve icinde desenin 2 dongusu var; bir dongu
/// kaydirip basa donunce gecis dikissiz olur.
/// </summary>
public class DonerSpin : MonoBehaviour
{
    [Tooltip("Bir tam tur kac saniye sursun. Buyudukce yavaslar.")]
    public float cycle = 4f;

    [Tooltip("Etin uzerinde kayan isik bandi (genisligi etin 2 kati olmali).")]
    public RectTransform sheen;

    void Start()
    {
        if (sheen == null)
        {
            Debug.LogWarning("DonerSpin: sheen atanmamis, donme efekti calismayacak.");
            return;
        }

        float span = sheen.rect.width * 0.5f;      // bir dongu = bandin yarisi
        if (span <= 0.01f) return;

        float startX = sheen.anchoredPosition.x;
        sheen.DOAnchorPosX(startX - span, cycle)
             .SetEase(Ease.Linear)
             .SetLoops(-1, LoopType.Restart)
             .SetTarget(this);
    }

    void OnDestroy()
    {
        DOTween.Kill(this);
    }
}
