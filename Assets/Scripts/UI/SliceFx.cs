using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tiklamada donerden kopup ucan dilimler.
/// Havuz kullanir - hizli tiklamada surekli Instantiate/Destroy yapmaz.
/// </summary>
public class SliceFx : MonoBehaviour
{
    public static SliceFx Instance;

    [Header("Kaynak")]
    public RectTransform spawnRoot;      // dilimlerin ekleneceği kok (Canvas altinda bir obje)
    public Sprite        sliceSprite;

    [Header("Ayarlar")]
    [Tooltip("Her tiklamada kac dilim ucsun.")]
    public int   perClick   = 3;
    [Tooltip("Havuzdaki toplam dilim sayisi. Ustune cikilirsa en eskisi geri kullanilir.")]
    public int   poolSize   = 28;
    public float sizeMin    = 46f;
    public float sizeMax    = 78f;
    public float speedMin   = 620f;
    public float speedMax   = 1050f;
    public float gravity    = 2600f;
    public float life       = 0.85f;
    public float spinSpeed  = 260f;

    class Slice
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float spin, age;
        public bool alive;
    }

    readonly List<Slice> pool = new List<Slice>();
    int next;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (spawnRoot == null || sliceSprite == null) return;
        for (int i = 0; i < poolSize; i++) pool.Add(Create());
    }

    Slice Create()
    {
        var go = new GameObject("slice", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = spawnRoot.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(spawnRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        var img = go.GetComponent<Image>();
        img.sprite = sliceSprite;
        img.raycastTarget = false;
        img.preserveAspect = true;
        go.SetActive(false);
        return new Slice { rt = rt, img = img };
    }

    /// <summary>Verilen ekran noktasindan dilim patlatir.</summary>
    public void Burst(Vector2 screenPos)
    {
        if (pool.Count == 0) return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnRoot, screenPos, null, out local);

        for (int i = 0; i < perClick; i++)
        {
            var s = pool[next]; next = (next + 1) % pool.Count;

            float size = Random.Range(sizeMin, sizeMax);
            s.rt.sizeDelta = new Vector2(size, size * 80f / 170f);
            s.rt.anchoredPosition = local + new Vector2(Random.Range(-28f, 28f), Random.Range(-28f, 28f));
            s.rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            float ang = Random.Range(20f, 160f) * Mathf.Deg2Rad;      // yukari dogru yay
            float spd = Random.Range(speedMin, speedMax);
            s.vel  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            s.spin = Random.Range(-spinSpeed, spinSpeed);
            s.age  = 0f;
            s.alive = true;
            s.img.color = Color.white;
            s.rt.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < pool.Count; i++)
        {
            var s = pool[i];
            if (!s.alive) continue;

            s.age += dt;
            if (s.age >= life)
            {
                s.alive = false;
                s.rt.gameObject.SetActive(false);
                continue;
            }

            s.vel.y -= gravity * dt;
            s.rt.anchoredPosition += s.vel * dt;
            s.rt.Rotate(0, 0, s.spin * dt);

            float t = s.age / life;
            if (t > 0.55f)
            {
                var c = s.img.color;
                c.a = 1f - (t - 0.55f) / 0.45f;
                s.img.color = c;
            }
        }
    }
}
