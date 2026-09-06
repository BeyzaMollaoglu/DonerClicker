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

    [Header("Ses Butonları")]
    public Button btn_sound;           // ses efektleri aç/kapa
    public Button btn_music;           // müzik aç/kapa
    [Tooltip("Butonların içindeki ikon Image'ları - açık/kapalı rengi bunlara uygulanır.")]
    public Image  img_sound;
    public Image  img_music;

    [Header("Panel dışına dokununca kapansın")]
    [Tooltip("Diğer panellerin kullandığı arka plan kapatıcı (blocker_button).")]
    public Button btn_blocker;

    static readonly Color OnCol  = new Color(0.941f, 0.706f, 0.255f);   // #F0B441
    static readonly Color OffCol = new Color(0.353f, 0.267f, 0.196f);   // sönük

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

        if (btn_sound != null) btn_sound.onClick.AddListener(ToggleSound);
        if (btn_music != null) btn_music.onClick.AddListener(ToggleMusic);

        SoundSettings.Apply();
        RefreshAudioIcons();
    }

    /// <summary>Açık = altın, kapalı = sönük. Ayrı bir "kapalı" ikonu gerekmiyor.</summary>
    void RefreshAudioIcons()
    {
        if (img_sound != null) img_sound.color = SoundSettings.SfxOn   ? OnCol : OffCol;
        if (img_music != null) img_music.color = SoundSettings.MusicOn ? OnCol : OffCol;
    }

    public void ToggleSound()
    {
        SoundSettings.SfxOn = !SoundSettings.SfxOn;
        RefreshAudioIcons();
    }

    public void ToggleMusic()
    {
        SoundSettings.MusicOn = !SoundSettings.MusicOn;
        RefreshAudioIcons();
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel == null) return;

        settingsPanel.gameObject.SetActive(true);
        RefreshAudioIcons();

        // Diğer paneller gibi: dışarı dokununca kapansın
        if (btn_blocker != null)
        {
            btn_blocker.gameObject.SetActive(true);
            btn_blocker.onClick.AddListener(CloseSettingsPanel);
        }

        // DOTween ile popup açılış animasyonu
        settingsPanel.localScale = Vector3.one * 0.85f;
        settingsPanel.DOKill();
        settingsPanel.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel == null) return;

        // Kapatıcıyı bize ait dinleyiciden arındırıp gizle.
        // UITabManager'ın kendi dinleyicisi kalır ama açık panel yokken zararsız.
        if (btn_blocker != null)
        {
            btn_blocker.onClick.RemoveListener(CloseSettingsPanel);
            btn_blocker.gameObject.SetActive(false);
        }

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
        if (btn_sound != null) btn_sound.onClick.RemoveAllListeners();
        if (btn_music != null) btn_music.onClick.RemoveAllListeners();
        if (btn_blocker != null) btn_blocker.onClick.RemoveListener(CloseSettingsPanel);
    }
}