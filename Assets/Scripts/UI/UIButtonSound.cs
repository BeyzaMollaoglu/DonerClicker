using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Header("Bu buton hangi sesi çıkarsın?")]
    public SoundType soundType = SoundType.TabClick;

    private void Start()
    {
        Button btn = GetComponent<Button>();
        
        btn.onClick.AddListener(() => 
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(soundType);
        });
    }
}