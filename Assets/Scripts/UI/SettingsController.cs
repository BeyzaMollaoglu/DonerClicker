using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingsController : MonoBehaviour
{
    [Header("Panel Bağlantısı")]
    public RectTransform settingsPanel;

    [Header("Butonlar")]
    public Button btn_openSettings;    // Ayarları açan çark butonu
    public Button btn_closeSettings;   // Ayarları kapatan X butonu
    
    [Header("Dil Butonları")]
    public Button btn_languageTR;      // Türkçe yapma butonu
    public Button btn_languageEN;      // İngilizce yapma butonu

    private void Start()
    {
        // Başlangıçta panel kapalı olsun
        if (settingsPanel != null)
            settingsPanel.gameObject.SetActive(false);

        // Butonlara görevlerini kod üzerinden (Listener ile) atıyoruz
        if (btn_openSettings != null)
            btn_openSettings.onClick.AddListener(OpenSettingsPanel);

        if (btn_closeSettings != null)
            btn_closeSettings.onClick.AddListener(CloseSettingsPanel);

        // Yeni dil butonlarını bağlıyoruz
        if (btn_languageTR != null)
            btn_languageTR.onClick.AddListener(SetLanguageTurkish);

        if (btn_languageEN != null)
            btn_languageEN.onClick.AddListener(SetLanguageEnglish);
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel == null) return;

        settingsPanel.gameObject.SetActive(true);
        
        // DOTween ile popup açılış animasyonu
        settingsPanel.localScale = Vector3.one * 0.85f;
        settingsPanel.DOKill();
        settingsPanel.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel == null) return;

        // DOTween ile küçülerek kapanış animasyonu
        settingsPanel.DOKill();
        settingsPanel.DOScale(Vector3.one * 0.85f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => settingsPanel.gameObject.SetActive(false));
    }

    // --- YENİ DİL KONTROL METOTLARI ---
    public void SetLanguageTurkish()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage(LocalizationManager.Language.Turkish);
        }
    }

    public void SetLanguageEnglish()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage(LocalizationManager.Language.English);
        }
    }

    private void OnDestroy()
    {
        // Hafıza sızıntısı olmaması için obje yok olduğunda listener'ları siliyoruz
        if (btn_openSettings != null) btn_openSettings.onClick.RemoveAllListeners();
        if (btn_closeSettings != null) btn_closeSettings.onClick.RemoveAllListeners();
        if (btn_languageTR != null) btn_languageTR.onClick.RemoveAllListeners();
        if (btn_languageEN != null) btn_languageEN.onClick.RemoveAllListeners();
    }
}