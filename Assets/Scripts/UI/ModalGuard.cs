using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sekme olmayan paneller (basarimlar, istatistikler, ayarlar) acikken
/// ARKADAKI HER SEY tiklanabilir kaliyordu: oyuncu panelin arkasindaki alt
/// sekmeye ya da yandaki cark/kupa butonuna basip ikinci bir paneli ust uste
/// acabiliyordu.
///
/// Bu sinif acik modal sayisini tutuyor; ilk acilista arkadaki butonlari
/// kapatiyor, sonuncusu kapaninca geri aciyor. Sayac tutmasinin sebebi
/// panellerin birbirini kapatirken sirasinin garanti olmamasi.
/// </summary>
public static class ModalGuard
{
    static int depth;

    public static bool AnyOpen { get { return depth > 0; } }

    public static void Enter(Button[] sideButtons)
    {
        depth++;
        if (depth == 1) Apply(sideButtons, false);
        else            Apply(sideButtons, false);   // ust uste acilirsa yine kapali kalsin
    }

    public static void Exit(Button[] sideButtons)
    {
        depth--;
        if (depth < 0) depth = 0;
        if (depth == 0) Apply(sideButtons, true);
    }

    static void Apply(Button[] sideButtons, bool on)
    {
        if (UITabManager.Instance != null) UITabManager.Instance.SetTabsClickable(on);

        if (sideButtons == null) return;
        foreach (var b in sideButtons)
            if (b != null) b.enabled = on;
    }
}
