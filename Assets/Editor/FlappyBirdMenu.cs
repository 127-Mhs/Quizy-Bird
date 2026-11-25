#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Menu bantu: buat scene utama dan build ke Windows (.exe).</summary>
public static class FlappyBirdMenu
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Quizy Bird/1. Buat Scene Utama")]
    static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.Refresh();
        Debug.Log("Scene dibuat: " + ScenePath + ". Tekan Play untuk bermain.");
    }

    [MenuItem("Quizy Bird/2. Build Windows (.exe)")]
    static void BuildWindows()
    {
        if (!File.Exists(ScenePath)) CreateScene();

        string path = EditorUtility.SaveFilePanel("Simpan Quizy Bird", "", "QuizyBird", "exe");
        if (string.IsNullOrEmpty(path)) return;

        PlayerSettings.productName = "Quizy Bird";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildPipeline.BuildPlayer(options);
    }
}
#endif
