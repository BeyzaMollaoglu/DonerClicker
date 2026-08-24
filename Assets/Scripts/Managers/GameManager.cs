using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton mimarisi: Diğer scriptlerin GameManager.Instance diyerek bu koda ulaşmasını sağlar
    public static GameManager Instance;

    [Header("Oyun Verileri")]
    public double totalDoner = 0;
    public double clickPower = 1;
    public double productionPerSecond = 0;
    public float productionInterval = 1f;

    [HideInInspector] public double baseClickPower = 1;
    [HideInInspector] public double clickMultiplier = 1; 
    [HideInInspector] public double basePassiveProduction = 0;
    [HideInInspector] public double passiveMultiplier = 1;

    // Arayüzü güncellemek için UIManager'a referans
    public UIManager uiManager;

    private void Awake()
    {
        // Singleton Kurulumu
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        RecalculateStats();
        uiManager.UpdateTotalDonerText(totalDoner);
        StartCoroutine(AutoProductionLoop());
    }

    public void OnDonerClicked()
    {
        totalDoner += clickPower;
        uiManager.UpdateTotalDonerText(totalDoner);
        uiManager.PlayClickFeedback(clickPower);
    }

    // Matematik formülü
    public void RecalculateStats()
    {
        clickPower = baseClickPower * clickMultiplier;
        productionPerSecond = basePassiveProduction * passiveMultiplier;
    }

    // Harcama yapma fonksiyonu (Upgrade ve Worker scriptleri burayı çağıracak)
    public bool SpendDoner(double amount)
    {
        if (totalDoner >= amount)
        {
            totalDoner -= amount;
            uiManager.UpdateTotalDonerText(totalDoner);
            return true; // Para yetti ve harcandı
        }
        return false; // Para yetmedi
    }

    private IEnumerator AutoProductionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(productionInterval);
            if (productionPerSecond > 0)
            {
                totalDoner += productionPerSecond;
                uiManager.UpdateTotalDonerText(totalDoner);
            }
        }
    }
}