#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class LevelBatchGenerator  : EditorWindow
{
    private int startLevel = 1;
    private int endLevel = 50;
    private int regenerateLevel = 1;
    private string savePath = "Assets/Game/Levels";

    [MenuItem("Tools/Water Sort/Level Batch Generator")]
    public static void ShowWindow()
    {
        GetWindow<LevelBatchGenerator>("Level Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Level Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        startLevel = EditorGUILayout.IntField("Start Level", startLevel);
        endLevel = EditorGUILayout.IntField("End Level", endLevel);
        regenerateLevel = EditorGUILayout.IntField("Level Number", regenerateLevel);
        EditorGUILayout.Space();
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        if (GUILayout.Button("Browse...", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                savePath = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Generate All Levels", GUILayout.Height(40)))
        {
            GenerateAllLevels();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Regenerate Single Level", GUILayout.Height(30)))
        {
            RegenerateSingleLevel(regenerateLevel);
        }
    }

    private void GenerateAllLevels()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        for (int i = startLevel; i <= endLevel; i++)
        {
            GenerateLevel(i);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✓ Generated {endLevel - startLevel + 1} levels in {savePath}");
    }

    private void RegenerateSingleLevel(int leveNumber)
    {
        GenerateLevel(leveNumber);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void GenerateLevel(int levelNumber)
    {
        string assetPath = $"{savePath}/Level_{levelNumber:D3}.asset";

        // Check if exists
        LevelDataSO level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);

        if (level == null)
        {
            level = ScriptableObject.CreateInstance<LevelDataSO>();
            AssetDatabase.CreateAsset(level, assetPath);
        }

        // Generate
        level.GenerateFromLevel(levelNumber);

        EditorUtility.SetDirty(level);

        Debug.Log($"Generated Level {levelNumber}: {level.totalColor} colors, {level.tubes.Count} tubes, {level.viewType}");
    }
}
#endif