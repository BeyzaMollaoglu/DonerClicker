using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Ses türlerimiz
public enum SoundType
{
    TabClick,
    Buy,
    DonerClick,
    Celebrate
}

// Inspector'da görünecek olan özel atama yapımız
[System.Serializable]
public class CustomButtonSound
{
    public Button buton;
    public SoundType sesTuru;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Ses Kaynağı")]
    public AudioSource sfxSource;

    [Header("Ses Dosyaları")]
    public AudioClip tabClickSound;
    public AudioClip buySound;
    public AudioClip donerClickSound;

    [Tooltip("Doner kesme varyantlari. Doluysa donerClickSound yerine bunlar SIRAYLA calar - " +
             "ayni sesin bininci kez tekrarladigi hissi kalkar.")]
    public AudioClip[] donerClickVariants;
    int variantIndex;

    public AudioClip CelebrateSound;

    [Header("Özel Ses Atamaları (Buraya Ekle)")]
    public List<CustomButtonSound> ozelButonlar = new List<CustomButtonSound>();

    [Header("Canlilik - ayni sesin tekduze duyulmasini onler")]
    [Tooltip("Her calista perde bu araliktan rastgele secilir. 1 = degisme yok.")]
    public float pitchMin = 0.94f;
    public float pitchMax = 1.07f;
    [Tooltip("Her calista ses seviyesi bu araliktan rastgele secilir.")]
    public float volumeMin = 0.85f;
    public float volumeMax = 1.00f;
    [Tooltip("Ayni tur ses en fazla bu siklikta calar (saniye). Ust uste binip kirilmasini onler.")]
    public float minGap = 0.045f;

    readonly Dictionary<int, float> lastPlayed = new Dictionary<int, float>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 1. İşlem yaptığımız butonları hatırlamak için bir kayıt defteri oluşturuyoruz
        HashSet<Button> islenenButonlar = new HashSet<Button>();

        // 2. Inspector'dan listeye eklediğin özel butonlara seçtiğin sesleri atıyoruz
        foreach (CustomButtonSound ayar in ozelButonlar)
        {
            if (ayar.buton != null)
            {
                ayar.buton.onClick.AddListener(() => PlaySound(ayar.sesTuru));
                islenenButonlar.Add(ayar.buton); // Butonu kayıt defterine ekle
            }
        }

        // 3. Sahnede var olan TÜM butonları buluyoruz
        Button[] tumButonlar = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // 4. Eğer bir buton senin özel listende YOKSA, ona otomatik olarak standart sesi veriyoruz
        foreach (Button btn in tumButonlar)
        {
            if (islenenButonlar.Contains(btn)) continue;

            // LISTE KARTLARINI ATLA. Kartlarin sesini manager'lar caliyor ve
            // sadece satin alma GERCEKLESTIGINDE caliyor; buraya dahil edilirse
            // alinmis ya da parasi yetmeyen karta basinca da ses cikar.
            if (btn.GetComponent<CardView>() != null) continue;

            btn.onClick.AddListener(() => PlaySound(SoundType.TabClick));
        }
    }

    /// <summary>
    /// Kesme sesini SIRAYLA dondurur (rastgele degil: rastgelede ayni ses
    /// arka arkaya iki kez gelebiliyor ve tekrar hissi geri geliyor).
    /// </summary>
    AudioClip NextDonerClip()
    {
        if (donerClickVariants == null || donerClickVariants.Length == 0) return donerClickSound;
        AudioClip c = donerClickVariants[variantIndex % donerClickVariants.Length];
        variantIndex++;
        return c != null ? c : donerClickSound;
    }

    public void PlaySound(SoundType type)
    {
        if (sfxSource == null) return;

        // Ayarlar panelindeki ses dugmesi: kapaliysa hic calma.
        // (Muzik ayri; SoundSettings.MusicSource uzerinden susturuluyor.)
        if (!SoundSettings.SfxOn) return;

        AudioClip clipToPlay = type switch
        {
            SoundType.TabClick => tabClickSound,
            SoundType.Buy => buySound,
            SoundType.DonerClick => NextDonerClip(),
            SoundType.Celebrate => CelebrateSound,
            _ => null
        };

        if (clipToPlay == null) return;

        // --- TIKLAMA OYUNUNDA SES NEDEN BOZUK DUYULUR ---
        // Ayni klip saniyede 5-10 kez, HEP AYNI perdeden calininca kulak bunu
        // ses degil "makineli tufek" gibi duyuyor. Her calista perdeyi ve
        // sesi hafifce degistirmek, ayni dosyayla bile duyumu tamamen degistirir.
        float p0 = sfxSource.pitch;
        sfxSource.pitch = Random.Range(pitchMin, pitchMax);

        // Ust uste binen sesler toplanip kirilma (clipping) yapiyordu.
        // Ayni turden ses cok kisa arayla tekrar istenirse atlaniyor.
        float now = Time.unscaledTime;
        int   key = (int)type;
        if (lastPlayed.TryGetValue(key, out float t) && now - t < minGap)
        {
            sfxSource.pitch = p0;
            return;
        }
        lastPlayed[key] = now;

        sfxSource.PlayOneShot(clipToPlay, Random.Range(volumeMin, volumeMax));
        sfxSource.pitch = p0;
    }
}