using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Basarim acilinca ekranin ustunde beliren serit.
///
/// Ayni anda birkac basarim birden acilabildigi icin (ozellikle prestij
/// sonrasi) gelenler kuyruga giriyor ve tek tek gosteriliyor; ust uste
/// binmiyorlar.
/// </summary>
public class AchievementToast : MonoBehaviour
{
    public RectTransform root;
    public TextMeshProUGUI title;
    public TextMeshProUGUI body;

    [Tooltip("Serit ekranda kac saniye kalsin.")]
    public float holdSeconds = 2.6f;
    [Tooltip("Gizliyken durdugu Y konumu (ekranin ustunde).")]
    public float hiddenY = 260f;
    [Tooltip("Gorunurken durdugu Y konumu.")]
    public float shownY = -40f;

    readonly Queue<KeyValuePair<string, string>> queue = new Queue<KeyValuePair<string, string>>();
    bool busy;

    void Awake()
    {
        if (root != null)
        {
            root.anchoredPosition = new Vector2(root.anchoredPosition.x, hiddenY);
            root.gameObject.SetActive(false);
        }
    }

    public void Show(string name, string desc)
    {
        queue.Enqueue(new KeyValuePair<string, string>(name, desc));
        if (!busy) Next();
    }

    void Next()
    {
        if (root == null) return;
        if (queue.Count == 0) { busy = false; return; }

        busy = true;
        var item = queue.Dequeue();

        string head = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetLocalizedValue("ach_unlocked")
                    : "BAŞARIM AÇILDI";

        if (title != null) title.text = head;
        if (body  != null) body.text  = item.Key;

        root.gameObject.SetActive(true);
        root.DOKill();
        root.anchoredPosition = new Vector2(root.anchoredPosition.x, hiddenY);
        root.DOAnchorPosY(shownY, 0.38f).SetEase(Ease.OutBack).SetUpdate(true)
            .OnComplete(() =>
            {
                root.DOAnchorPosY(hiddenY, 0.32f).SetEase(Ease.InBack).SetDelay(holdSeconds).SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (queue.Count == 0) root.gameObject.SetActive(false);
                        Next();
                    });
            });
    }

    void OnDestroy() { if (root != null) root.DOKill(); }
}
