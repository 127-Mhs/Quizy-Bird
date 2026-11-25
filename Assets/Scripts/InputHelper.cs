using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Pembungkus input agar game berjalan baik di project dengan Input Manager lama
/// maupun Input System baru (Unity 6).
/// </summary>
public static class InputHelper
{
    /// <summary>Spasi / Panah Atas / W / klik kiri / sentuh layar.</summary>
    public static bool FlapPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool r = false;
        var k = Keyboard.current;
        var m = Mouse.current;
        var t = Touchscreen.current;
        if (k != null) r |= k.spaceKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame;
        if (m != null) r |= m.leftButton.wasPressedThisFrame;
        if (t != null) r |= t.primaryTouch.press.wasPressedThisFrame;
        return r;
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetKeyDown(KeyCode.W) || Input.GetMouseButtonDown(0);
#endif
    }

    /// <summary>Spasi atau Enter (tanpa mouse, supaya tidak bentrok dengan tombol UI).</summary>
    public static bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
    }

    public static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    /// <summary>Tombol angka 1-4 (juga A-D) untuk menjawab kuis. -1 jika tidak ada.</summary>
    public static int AnswerKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return -1;
        if (k.digit1Key.wasPressedThisFrame || k.aKey.wasPressedThisFrame) return 0;
        if (k.digit2Key.wasPressedThisFrame || k.bKey.wasPressedThisFrame) return 1;
        if (k.digit3Key.wasPressedThisFrame || k.cKey.wasPressedThisFrame) return 2;
        if (k.digit4Key.wasPressedThisFrame || k.dKey.wasPressedThisFrame) return 3;
        return -1;
#else
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.A)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.B)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.C)) return 2;
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.D)) return 3;
        return -1;
#endif
    }

    public static bool MutePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.mKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.M);
#endif
    }
}
