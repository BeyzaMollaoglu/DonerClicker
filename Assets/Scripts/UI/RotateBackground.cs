using UnityEngine;
using DG.Tweening; 

public class RotateBackground : MonoBehaviour
{
    [Header("Dönüş Ayarları")]
    [Tooltip("Dönüş süresi. Sayı büyüdükçe yavaşlar.")]
    public float duration = 40f; 
    
    [Tooltip("Saat yönünde mi dönsün?")]
    public bool clockwise = true; 

    void Start()
    {
        // Saat yönünde eksi (-360), tersine artı (360) derece döner.
        float targetAngle = clockwise ? -360f : 360f;

        // Kendi etrafında (Z ekseninde) sonsuza kadar pürüzsüz dön
        transform.DORotate(new Vector3(0, 0, targetAngle), duration, RotateMode.FastBeyond360)
                 .SetLoops(-1, LoopType.Restart) 
                 .SetEase(Ease.Linear); 
    }
}