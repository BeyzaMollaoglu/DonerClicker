using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class AutoUITranslator : MonoBehaviour
{
    private Dictionary<TMP_Text, string> originalTexts = new Dictionary<TMP_Text, string>();

    private void Start()
    {
        // Sadece sahnede var olan yazıları bul (Gizli prefabları değil)
        TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text txt in allTexts)
        {
            if (txt != null && !string.IsNullOrWhiteSpace(txt.text))
            {
                string cleanText = txt.text.Trim(); 
                
                // HAYAT KURTARAN KISIM: Sadece sözlüğe eklediğimiz sabit kelimeleri hafızaya al!
                // Böylece işçi seviyelerine, paralara veya silinen uçuşan yazılara bulaşmaz.
                if (LocalizationManager.Instance.HasKey(cleanText))
                {
                    originalTexts[txt] = cleanText; 
                }
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

            // Yazı objesi gerçekten hala sahnede duruyorsa çevir
            if (txt != null)
            {
                txt.text = LocalizationManager.Instance.GetLocalizedValue(originalText);
            }
        }
    }
}