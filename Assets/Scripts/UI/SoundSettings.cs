using UnityEngine;

/// <summary>
/// Ses efekti ve muzik acik/kapali tercihi. PlayerPrefs'te kalici.
///
/// MonoBehaviour DEGIL - sahneye bir sey eklemeye gerek yok, her yerden
/// SoundSettings.SfxOn diye okunur. Ses dosyalari eklendiginde
/// AudioManager sadece bu iki bayragi kontrol edecek; arayuz tarafi hazir.
/// </summary>
public static class SoundSettings
{
    const string K_SFX   = "opt_sfx";
    const string K_MUSIC = "opt_music";

    static int sfx = -1, music = -1;      // -1 = henuz okunmadi

    public static bool SfxOn
    {
        get { if (sfx   < 0) sfx   = PlayerPrefs.GetInt(K_SFX,   1); return sfx   == 1; }
        set { sfx   = value ? 1 : 0; PlayerPrefs.SetInt(K_SFX,   sfx);   PlayerPrefs.Save(); }
    }

    public static bool MusicOn
    {
        get { if (music < 0) music = PlayerPrefs.GetInt(K_MUSIC, 1); return music == 1; }
        set { music = value ? 1 : 0; PlayerPrefs.SetInt(K_MUSIC, music); PlayerPrefs.Save(); Apply(); }
    }

    /// <summary>Muzik kaynagi eklenince buraya baglanir; susturma otomatik uygulanir.</summary>
    public static AudioSource MusicSource;

    public static void Apply()
    {
        if (MusicSource != null) MusicSource.mute = !MusicOn;
    }
}
