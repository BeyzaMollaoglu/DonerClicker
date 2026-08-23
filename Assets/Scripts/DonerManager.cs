using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class DonerManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI txt_doner_count; 
    public Button button_icon_doner; 
    public GameObject floatingTextPrefab;
    public Transform mainCanvas;
    
    [Header("Game Data")]
    public double totalDoner = 0; 
    public double clickPower = 1;
    public double productionPerSecond = 2;
    public float productionInterval = 1f; 

    void Start()
    {
        button_icon_doner.onClick.AddListener(OnDonerClicked);
        UpdateUI();
        StartCoroutine(AutoProductionLoop());
    }

    private void OnDonerClicked()
    {
        totalDoner += clickPower;
        UpdateUI();

        button_icon_doner.transform.DOKill(true); 
        button_icon_doner.transform.localScale = Vector3.one * 0.9f;
        button_icon_doner.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);

        SpawnFloatingText();
    }

    private void SpawnFloatingText()
    {
        Vector2 spawnPosition = Input.mousePosition;
        GameObject floatingObj = Instantiate(floatingTextPrefab, mainCanvas);
        floatingObj.transform.position = spawnPosition;

        TextMeshProUGUI floatText = floatingObj.GetComponent<TextMeshProUGUI>();
        floatText.text = "+" + clickPower.ToString("F0");

        float randomX = Random.Range(-50f, 50f);
        Vector3 targetPos = floatingObj.transform.position + new Vector3(randomX, 150f, 0f);

        floatingObj.transform.DOMove(targetPos, 0.8f).SetEase(Ease.OutQuad);
        floatText.DOFade(0, 0.8f).OnComplete(() =>
        {
            Destroy(floatingObj);
        });
    }

    private IEnumerator AutoProductionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(productionInterval);
            
            if (productionPerSecond > 0)
            {
                totalDoner += productionPerSecond;
                UpdateUI();
            }
        }
    }

    private void UpdateUI()
    {
        txt_doner_count.text = totalDoner.ToString("F0"); 
    }
    
    private void OnDestroy()
    {
        if (button_icon_doner != null)
        {
            button_icon_doner.onClick.RemoveListener(OnDonerClicked);
        }
    }
}