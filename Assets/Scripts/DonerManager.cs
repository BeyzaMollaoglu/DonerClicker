using UnityEngine;
using TMPro;
using DG.Tweening;

public class DonerManager : MonoBehaviour
{
    [Header("Arayüz (UI) Referansları")]
    public TextMeshProUGUI txt_doner_count;
    public RectTransform button_icon_doner;
    
    public GameObject floatingTextPrefab;
    public Transform floatingTextSpawnPoint; 

    [Header("Oyun İçi Değişkenler")]
    public double totalDoner = 0; 
    public double tiklamaGucu = 1;

    void Start()
    {
        UpdateUI();
    }

    public void OnDonerClicked()
    {
        totalDoner += tiklamaGucu;
        UpdateUI();

        button_icon_doner.DOKill(true); 
        button_icon_doner.localScale = Vector3.one * 0.9f; 
        button_icon_doner.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);

    }

    private void UpdateUI()
    {
        txt_doner_count.text = totalDoner.ToString("F0"); 
    }
}