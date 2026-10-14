using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public sealed class TimelineActionEditorWindow : EditorWindow
{
    private readonly List<TimelineActionPreset> presets = new();

    private TimelineActionPreset selected;
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private SerializedObject serializedPreset;

    [MenuItem("Tools/Timeline Action System")]
    public static void Open()
    {
        GetWindow<TimelineActionEditorWindow>(
            "Timeline Action System"
        );
    }

    private void OnEnable()
    {
        RefreshPresets();
    }

    private void RefreshPresets()
    {
        presets.Clear();

        foreach (string guid in
                 AssetDatabase.FindAssets("t:TimelineActionPreset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            var preset =
                AssetDatabase.LoadAssetAtPath<TimelineActionPreset>(path);

            if (preset != null)
                presets.Add(preset);
        }

        if (selected == null && presets.Count > 0)
            SelectPreset(presets[0]);
    }

    private void SelectPreset(TimelineActionPreset preset)
    {
        selected = preset;
        serializedPreset = selected != null
            ? new SerializedObject(selected)
            : null;
    }

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.BeginHorizontal();

        DrawPresetList();

        EditorGUILayout.BeginVertical();
        DrawPresetDetails();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("更新", EditorStyles.toolbarButton))
            RefreshPresets();

        if (GUILayout.Button("新規プリセット", EditorStyles.toolbarButton))
            CreatePreset();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPresetList()
    {
        EditorGUILayout.BeginVertical(
            GUILayout.Width(210),
            GUILayout.ExpandHeight(true)
        );

        EditorGUILayout.LabelField(
            "プリセット",
            EditorStyles.boldLabel
        );

        listScroll = EditorGUILayout.BeginScrollView(listScroll);

        foreach (var preset in presets)
        {
            if (preset == null) continue;

            bool isSelected = preset == selected;

            if (GUILayout.Toggle(
                    isSelected,
                    preset.PresetName,
                    "Button"))
            {
                if (!isSelected)
                    SelectPreset(preset);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawPresetDetails()
    {
        if (selected == null || serializedPreset == null)
        {
            EditorGUILayout.HelpBox(
                "プリセットを選択してください。",
                MessageType.Info
            );
            return;
        }

        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

        serializedPreset.Update();

        EditorGUILayout.LabelField(
            "プリセット設定",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            serializedPreset.FindProperty("presetName")
        );

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "登録 Timeline",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            serializedPreset.FindProperty("entries"),
            true
        );

        serializedPreset.ApplyModifiedProperties();

        EditorGUILayout.Space(8);

        if (GUILayout.Button("変更を保存"))
        {
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("選択中のプリセットを Project で表示"))
            EditorGUIUtility.PingObject(selected);

        EditorGUILayout.EndScrollView();
    }

    private void CreatePreset()
    {
        const string root = "Assets/TimelineActionSystem";

        if (!AssetDatabase.IsValidFolder(root))
            AssetDatabase.CreateFolder("Assets", "TimelineActionSystem");

        const string folder = root + "/Presets";

        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(root, "Presets");

        var preset = CreateInstance<TimelineActionPreset>();
        string path = AssetDatabase.GenerateUniqueAssetPath(
            folder + "/NewTimelinePreset.asset"
        );

        AssetDatabase.CreateAsset(preset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshPresets();
        SelectPreset(preset);
    }
}