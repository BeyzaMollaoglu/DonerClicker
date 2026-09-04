using UnityEngine;
using TMPro; // TextMeshPro kullandığın için bu şart

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("LocalizationManager içindeki anahtar kelimeyi buraya yazın (örn: settings_title)")]
    public string textKey; 

    private TMP_Text targetText;

    private void Awake()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        // Obje aktif olduğunda ve dil değiştiğinde metni güncelle
        LocalizationManager.OnLanguageChanged += UpdateText;
        UpdateText(); 
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateText;
    }

    private void UpdateText()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(textKey))
        {
            targetText.text = LocalizationManager.Instance.GetLocalizedValue(textKey);
        }
    }
}