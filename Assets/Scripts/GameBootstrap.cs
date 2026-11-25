using UnityEngine;

/// <summary>
/// Membuat game otomatis begitu Play ditekan, pada scene apa pun.
/// Tidak perlu menyusun GameObject manual di Unity Editor.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Start()
    {
        if (GameManager.Instance != null) return;
        new GameObject("QuizyBird").AddComponent<GameManager>();
    }
}
