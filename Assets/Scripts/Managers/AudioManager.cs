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

    public AudioClip CelebrateSound;

    [Header("Özel Ses Atamaları (Buraya Ekle)")]
    public List<CustomButtonSound> ozelButonlar = new List<CustomButtonSound>();

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
            SoundType.DonerClick => donerClickSound,
            SoundType.Celebrate => CelebrateSound,
            _ => null
        };

        if (clipToPlay != null)
        {
            sfxSource.PlayOneShot(clipToPlay);
        }
    }
}