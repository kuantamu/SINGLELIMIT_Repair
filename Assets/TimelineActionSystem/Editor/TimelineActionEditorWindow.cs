using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

public sealed class TimelineActionEditorWindow : EditorWindow
{
    private readonly List<TimelineActionPreset> presets = new();

    private TimelineActionPreset selected;
    private SerializedObject serializedPreset;

    private Vector2 listScroll;
    private Vector2 detailScroll;

    [MenuItem("Tools/Timeline Action System")]
    public static void Open()
    {
        GetWindow<TimelineActionEditorWindow>(
            "Timeline Action System");
    }

    private void OnEnable()
    {
        RefreshPresets();
    }

    private void RefreshPresets()
    {
        presets.Clear();

        foreach (string guid in AssetDatabase.FindAssets(
                     "t:TimelineActionPreset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var preset =
                AssetDatabase.LoadAssetAtPath<TimelineActionPreset>(path);

            if (preset != null)
            {
                presets.Add(preset);
            }
        }

        if (selected != null && !presets.Contains(selected))
        {
            selected = null;
            serializedPreset = null;
        }

        if (selected == null && presets.Count > 0)
        {
            SelectPreset(presets[0]);
        }

        Repaint();
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
        {
            RefreshPresets();
        }

        if (GUILayout.Button(
                "新規プリセット", EditorStyles.toolbarButton))
        {
            CreatePreset();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPresetList()
    {
        EditorGUILayout.BeginVertical(
            GUILayout.Width(210),
            GUILayout.ExpandHeight(true));

        EditorGUILayout.LabelField("プリセット", EditorStyles.boldLabel);

        listScroll = EditorGUILayout.BeginScrollView(listScroll);

        foreach (var preset in presets)
        {
            if (preset == null) continue;

            bool isSelected = preset == selected;

            if (GUILayout.Toggle(
                    isSelected, preset.PresetName, "Button") &&
                !isSelected)
            {
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
                MessageType.Info);
            return;
        }

        serializedPreset.Update();

        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

        EditorGUILayout.LabelField(
            "プリセット設定", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            serializedPreset.FindProperty("presetName"),
            new GUIContent("プリセット名"));

        EditorGUILayout.Space(8);

        DrawEntries();

        EditorGUILayout.Space(8);

        DrawDuplicateWarnings();

        EditorGUILayout.Space(8);

        if (GUILayout.Button("変更を保存"))
        {
            serializedPreset.ApplyModifiedProperties();
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("選択中のプリセットを Project で表示"))
        {
            EditorGUIUtility.PingObject(selected);
        }

        EditorGUILayout.EndScrollView();

        // UI 上の編集を SerializedObject に反映する。
        serializedPreset.ApplyModifiedProperties();
    }

    private void DrawEntries()
    {
        SerializedProperty entries =
            serializedPreset.FindProperty("entries");

        EditorGUILayout.LabelField(
            $"登録 Timeline ({entries.arraySize})",
            EditorStyles.boldLabel);

        if (GUILayout.Button("＋ Timeline を追加"))
        {
            AddEntry(entries);
        }

        EditorGUILayout.Space(4);

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry =
                entries.GetArrayElementAtIndex(i);

            SerializedProperty id =
                entry.FindPropertyRelative("id");

            SerializedProperty actionName =
                entry.FindPropertyRelative("actionName");

            SerializedProperty timeline =
                entry.FindPropertyRelative("timeline");

            SerializedProperty animationClip =
                entry.FindPropertyRelative("animationClip");

            SerializedProperty speed =
                entry.FindPropertyRelative("defaultSpeed");

            SerializedProperty transition =
                entry.FindPropertyRelative("transitionDuration");

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                $"Timeline {i + 1}",
                EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("削除", GUILayout.Width(60)))
            {
                entries.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(id, new GUIContent("ID"));
            EditorGUILayout.PropertyField(
                actionName, new GUIContent("名前"));

            EditorGUILayout.PropertyField(
                timeline, new GUIContent("Timeline Asset"));

            EditorGUILayout.PropertyField(
                animationClip, new GUIContent("Animation Clip"));

            EditorGUILayout.PropertyField(
                speed, new GUIContent("再生速度"));

            EditorGUILayout.PropertyField(
                transition, new GUIContent("ブレンド時間"));

            EditorGUILayout.EndVertical();
        }
    }

    private void AddEntry(SerializedProperty entries)
    {
        int newId = GetNextId(entries);
        int index = entries.arraySize;

        entries.InsertArrayElementAtIndex(index);

        SerializedProperty entry =
            entries.GetArrayElementAtIndex(index);

        entry.FindPropertyRelative("id").intValue = newId;
        entry.FindPropertyRelative("actionName").stringValue =
            $"Action_{newId:D3}";

        entry.FindPropertyRelative("timeline").objectReferenceValue = null;
        entry.FindPropertyRelative("animationClip").objectReferenceValue = null;
        entry.FindPropertyRelative("defaultSpeed").floatValue = 1f;
        entry.FindPropertyRelative("transitionDuration").floatValue = 0.15f;
    }

    private int GetNextId(SerializedProperty entries)
    {
        int maxId = 0;

        for (int i = 0; i < entries.arraySize; i++)
        {
            int id = entries
                .GetArrayElementAtIndex(i)
                .FindPropertyRelative("id")
                .intValue;

            if (id > maxId)
            {
                maxId = id;
            }
        }

        return maxId + 1;
    }

    private void DrawDuplicateWarnings()
    {
        SerializedProperty entries =
            serializedPreset.FindProperty("entries");

        var duplicateIds = new HashSet<int>();
        var duplicateNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var seenIds = new HashSet<int>();
        var seenNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry =
                entries.GetArrayElementAtIndex(i);

            int id = entry.FindPropertyRelative("id").intValue;

            string actionName = entry
                .FindPropertyRelative("actionName")
                .stringValue
                .Trim();

            if (!seenIds.Add(id))
            {
                duplicateIds.Add(id);
            }

            if (!string.IsNullOrWhiteSpace(actionName) &&
                !seenNames.Add(actionName))
            {
                duplicateNames.Add(actionName);
            }
        }

        foreach (int id in duplicateIds)
        {
            EditorGUILayout.HelpBox(
                $"ID「{id}」が重複しています。",
                MessageType.Error);
        }

        foreach (string actionName in duplicateNames)
        {
            EditorGUILayout.HelpBox(
                $"名前「{actionName}」が重複しています。",
                MessageType.Error);
        }
    }

    private void CreatePreset()
    {
        const string root = "Assets/TimelineActionSystem";

        if (!AssetDatabase.IsValidFolder(root))
        {
            AssetDatabase.CreateFolder(
                "Assets", "TimelineActionSystem");
        }

        const string folder = root + "/Presets";

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(
                root, "Presets");
        }

        var preset = CreateInstance<TimelineActionPreset>();

        string path = AssetDatabase.GenerateUniqueAssetPath(
            folder + "/NewTimelinePreset.asset");

        AssetDatabase.CreateAsset(preset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshPresets();
        SelectPreset(preset);
    }
}