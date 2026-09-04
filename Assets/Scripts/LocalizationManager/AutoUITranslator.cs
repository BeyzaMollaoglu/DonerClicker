using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class AutoUITranslator : MonoBehaviour
{
    private Dictionary<TMP_Text, string> originalTexts = new Dictionary<TMP_Text, string>();

    private void Start()
    {
        TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();

        foreach (TMP_Text txt in allTexts)
        {
            if (txt.gameObject.scene.isLoaded && !string.IsNullOrWhiteSpace(txt.text))
            {
                string cleanText = txt.text.Trim(); 
                originalTexts[txt] = cleanText; 
            }
        }
        UpdateAllTexts();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateAllTexts;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateAllTexts;
    }

    private void UpdateAllTexts()
    {
        if (LocalizationManager.Instance == null) return;

        foreach (var kvp in originalTexts)
        {
            TMP_Text txt = kvp.Key;
            string originalText = kvp.Value;

            if (txt != null)
            {
                txt.text = LocalizationManager.Instance.GetLocalizedValue(originalText);
            }
        }
    }
}