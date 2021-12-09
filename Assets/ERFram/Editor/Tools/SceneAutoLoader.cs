using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
static class SceneAutoLoader
{
    // Static constructor binds a playmode-changed callback.
    // [InitializeOnLoad] above makes sure this gets executed.
    static SceneAutoLoader()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    // Menu items to select the "master" scene and control whether or not to load it.
    [MenuItem("Tools/Scene Autoload/Select Master Scene...")]
    private static void SelectMasterScene()
    {
        string masterScene = EditorUtility.OpenFilePanel("Select Master Scene", Application.dataPath, "unity");
        masterScene = masterScene.Replace(Application.dataPath, "Assets");  //project relative instead of absolute path
        if (!string.IsNullOrEmpty(masterScene))
        {
            MasterScene = masterScene;
            LoadMasterOnPlay = true;
        }
    }

    [MenuItem("Tools/Scene Autoload/Load Master On Play", true)]
    private static bool ShowLoadMasterOnPlay()
    {
        return !LoadMasterOnPlay;
    }
    [MenuItem("Tools/Scene Autoload/Load Master On Play")]
    private static void EnableLoadMasterOnPlay()
    {
        LoadMasterOnPlay = true;
    }

    [MenuItem("Tools/Scene Autoload/Don't Load Master On Play", true)]
    private static bool ShowDontLoadMasterOnPlay()
    {
        return LoadMasterOnPlay;
    }
    [MenuItem("Tools/Scene Autoload/Don't Load Master On Play")]
    private static void DisableLoadMasterOnPlay()
    {
        LoadMasterOnPlay = false;
    }

    // Play mode change callback handles the scene load/reload.
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!LoadMasterOnPlay)
        {
            return;
        }

        if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // User pressed play -- autoload master scene.
            PreviousScene = EditorSceneManager.GetActiveScene().path;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                try
                {
                    EditorSceneManager.OpenScene(MasterScene);
                }
                catch
                {
                    Debug.LogError(string.Format("error: scene not found: {0}", MasterScene));
                    EditorApplication.isPlaying = false;

                }
            }
            else
            {
                // User cancelled the save operation -- cancel play as well.
                EditorApplication.isPlaying = false;
            }
        }

        // isPlaying check required because cannot OpenScene while playing
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // User pressed stop -- reload previous scene.
            try
            {
                EditorSceneManager.OpenScene(PreviousScene);
            }
            catch
            {
                Debug.LogError(string.Format("error: scene not found: {0}", PreviousScene));
            }
        }
    }

    // Properties are remembered as editor preferences.
    private const string EditorPrefLoadMasterOnPlay = "ER.SceneAutoLoader.LoadMasterOnPlay";
    private const string EditorPrefMasterScene = "ER.SceneAutoLoader.SplashScene";
    private const string EditorPrefPreviousScene = "ER.SceneAutoLoader.PreviousScene";

    private static bool LoadMasterOnPlay
    {
        get { return EditorPrefs.GetBool(EditorPrefLoadMasterOnPlay, false); }
        set { EditorPrefs.SetBool(EditorPrefLoadMasterOnPlay, value); }
    }

    private static string MasterScene
    {
        get { return EditorPrefs.GetString(EditorPrefMasterScene, "Master.unity"); }
        set { EditorPrefs.SetString(EditorPrefMasterScene, value); }
    }

    private static string PreviousScene
    {
        get { return EditorPrefs.GetString(EditorPrefPreviousScene, EditorSceneManager.GetActiveScene().path); }
        set { EditorPrefs.SetString(EditorPrefPreviousScene, value); }
    }
}