using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum UpgradeType
{
    ClickPowerAdd,
    ClickPowerMultiplier,
    PassiveProductionAdd,
    PassiveProductionMultiplier,
    SpecificWorkerMultiplier
}

[System.Serializable]
public class UpgradeItem
{
    public string upgradeName;
    public UpgradeType type;
    public double cost;
    public double effectAmount;
    public int targetWorkerIndex = -1;

    [HideInInspector] public bool isPurchased = false;
    [HideInInspector] public Button buttonComponent;
    [HideInInspector] public TextMeshProUGUI buttonText;
    [HideInInspector] public CardView card;
}

[System.Serializable]
public class UpgradeDataWrapper { public List<UpgradeItem> upgrades; }

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
    public GameObject upgradeButtonPrefab;
    public Transform upgradeContent;
    [HideInInspector] public List<UpgradeItem> upgradeList;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        TextAsset jsonFile = Resources.Load<TextAsset>("upgrades");
        if (jsonFile != null) upgradeList = JsonUtility.FromJson<UpgradeDataWrapper>(jsonFile.text).upgrades;
    }

    void Start()
    {
        GameSaveData saveData = SaveManager.Instance.LoadGame();
        if (saveData != null && saveData.upgradePurchased.Count == upgradeList.Count)
        {
            for (int i = 0; i < upgradeList.Count; i++)
            {
                upgradeList[i].isPurchased = saveData.upgradePurchased[i];
                if (upgradeList[i].isPurchased) ApplyUpgradeEffect(upgradeList[i]);
            }
        }

        for (int i = 0; i < upgradeList.Count; i++)
        {
            int index = i;
            UpgradeItem item = upgradeList[i];

            GameObject newBtnObj = Instantiate(upgradeButtonPrefab, upgradeContent);
            item.buttonComponent = newBtnObj.GetComponent<Button>();
            item.card            = newBtnObj.GetComponent<CardView>();
            item.buttonText      = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            item.buttonComponent.onClick.AddListener(() => BuyUpgrade(index));
            UpdateUpgradeUI(item);
        }

        SortPurchasedUpgradesToBottom();
        GameManager.Instance.RecalculateStats();
        RefreshAffordability();
    }

    private string EffectLabel(UpgradeItem item)
    {
        string sym = (item.type == UpgradeType.ClickPowerAdd || item.type == UpgradeType.PassiveProductionAdd) ? "+" : "x";
        return $"Güç: {sym}{item.effectAmount}";
    }

    private string TargetLabel(UpgradeItem item)
    {
        if (item.targetWorkerIndex < 0) return "Tüm üretim";
        if (WorkerManager.Instance == null || WorkerManager.Instance.workerList == null) return "";
        if (item.targetWorkerIndex >= WorkerManager.Instance.workerList.Count) return "";
        return "Hedef: " + WorkerManager.Instance.workerList[item.targetWorkerIndex].workerName;
    }

    private void UpdateUpgradeUI(UpgradeItem item)
    {
        if (item.card != null)
        {
            if (item.isPurchased)
            {
                item.card.Set(item.upgradeName, "Satın alındı", EffectLabel(item), "ALINDI");
                item.card.SetPurchased();
                item.buttonComponent.interactable = false;
            }
            else
            {
                item.card.Set(item.upgradeName, EffectLabel(item), TargetLabel(item),
                              $"{UIManager.FormatNumber(item.cost)} TL");
                item.buttonComponent.interactable = true;
            }
            return;
        }

        // CardView yoksa eski davranis
        if (item.buttonText == null) return;
        if (item.isPurchased)
        {
            item.buttonText.text = $"<color=#808080><s>{item.upgradeName}</s>\n(ALINDI)</color>";
            item.buttonComponent.interactable = false;
        }
        else
        {
            item.buttonText.text = $"{item.upgradeName}\n{EffectLabel(item)}\nMaliyet: {UIManager.FormatNumber(item.cost)} TL";
            item.buttonComponent.interactable = true;
        }
    }

    private void BuyUpgrade(int index)
    {
        UpgradeItem item = upgradeList[index];

        if (item.targetWorkerIndex != -1 && WorkerManager.Instance != null)
        {
            if (WorkerManager.Instance.workerList[item.targetWorkerIndex].level == 0)
            {
                StartCoroutine(ShowWarningRoutine(item));
                return;
            }
        }

        if (GameManager.Instance.SpendDoner(item.cost))
        {
            item.isPurchased = true;
            ApplyUpgradeEffect(item);

            UpdateUpgradeUI(item);
            item.buttonComponent.transform.SetAsLastSibling();
            GameManager.Instance.RecalculateStats();

            if (WorkerManager.Instance != null)
            {
                foreach (var w in WorkerManager.Instance.workerList)
                    WorkerManager.Instance.UpdateWorkerUI(w);
            }
        }
    }

    private IEnumerator ShowWarningRoutine(UpgradeItem item)
    {
        item.buttonComponent.interactable = false;

        if (item.card != null && item.card.txtDetail != null)
            item.card.txtDetail.text = "<color=#FF5555>Önce işçiyi satın al!</color>";
        else if (item.buttonText != null)
            item.buttonText.text = "<color=red>ÖNCE İŞÇİYİ SATIN AL!</color>";

        yield return new WaitForSeconds(1.5f);

        item.buttonComponent.interactable = true;
        UpdateUpgradeUI(item);
    }

    private void ApplyUpgradeEffect(UpgradeItem item)
    {
        switch (item.type)
        {
            case UpgradeType.ClickPowerAdd: GameManager.Instance.baseClickPower += item.effectAmount; break;
            case UpgradeType.ClickPowerMultiplier: GameManager.Instance.clickMultiplier *= item.effectAmount; break;
            case UpgradeType.PassiveProductionAdd: GameManager.Instance.basePassiveProduction += item.effectAmount; break;
            case UpgradeType.PassiveProductionMultiplier: GameManager.Instance.passiveMultiplier *= item.effectAmount; break;
        }
    }

    public void RefreshAffordability()
    {
        if (upgradeList == null || GameManager.Instance == null) return;
        double money = GameManager.Instance.totalDoner;
        foreach (var u in upgradeList)
            if (u.card != null && !u.isPurchased) u.card.SetAffordable(money >= u.cost);
    }

    public double GetWorkerMultiplier(int workerIndex)
    {
        double multiplier = 1.0;
        foreach (var upg in upgradeList)
        {
            if (upg.isPurchased && upg.type == UpgradeType.SpecificWorkerMultiplier && upg.targetWorkerIndex == workerIndex)
                multiplier *= upg.effectAmount;
        }
        return multiplier;
    }

    private void SortPurchasedUpgradesToBottom()
    {
        foreach (var item in upgradeList)
            if (item.isPurchased && item.buttonComponent != null)
                item.buttonComponent.transform.SetAsLastSibling();
    }
}
