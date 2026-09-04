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
    public Button btn_toggleLanguage;  // Dil değiştirme butonu

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

        if (btn_toggleLanguage != null)
            btn_toggleLanguage.onClick.AddListener(ToggleLanguage);
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

    public void ToggleLanguage()
    {
        // Dil yöneticisi üzerinden kontrol ve değişim yapıyoruz
        if (LocalizationManager.Instance.currentLanguage == LocalizationManager.Language.Turkish)
        {
            LocalizationManager.Instance.SetLanguage(LocalizationManager.Language.English);
        }
        else
        {
            LocalizationManager.Instance.SetLanguage(LocalizationManager.Language.Turkish);
        }
    }

    private void OnDestroy()
    {
        // Hafıza sızıntısı olmaması için obje yok olduğunda listener'ları siliyoruz
        if (btn_openSettings != null) btn_openSettings.onClick.RemoveAllListeners();
        if (btn_closeSettings != null) btn_closeSettings.onClick.RemoveAllListeners();
        if (btn_toggleLanguage != null) btn_toggleLanguage.onClick.RemoveAllListeners();
    }
}