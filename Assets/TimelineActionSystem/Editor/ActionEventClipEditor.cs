#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// ActionEventClip のインスペクターを、イベントの種類ごとに
/// 必要な項目だけ表示するようにするエディター拡張。
///
/// 新しいイベントの種類を足したときは、
///  1. ActionEventClip に項目を足す
///  2. OnEnable で FindProperty する
///  3. OnInspectorGUI の switch に case を足す
/// の3か所を直します。
///
/// (このファイルは Editor フォルダに置くのが基本ですが、
///  #if UNITY_EDITOR で囲んであるので、どこに置いてもビルドは壊れません)
/// </summary>
[CustomEditor(typeof(ActionEventClip))]
public sealed class ActionEventClipEditor : Editor
{
    private SerializedProperty eventType;

    // VFX
    private SerializedProperty effectPrefab;
    private SerializedProperty effectDuration;
    private SerializedProperty positionOffset;
    private SerializedProperty rotationOffset;
    private SerializedProperty scaleMultiplier;
    private SerializedProperty stopWhenClipEnds;

    // SFX
    private SerializedProperty audioClip;

    // 攻撃判定
    private SerializedProperty attackId;
    private SerializedProperty attackHit;

    // 自分へのバフ・デバフ
    private SerializedProperty selfBuffs;

    private void OnEnable()
    {
        eventType = serializedObject.FindProperty("eventType");

        effectPrefab = serializedObject.FindProperty("effectPrefab");
        effectDuration = serializedObject.FindProperty("effectDuration");
        positionOffset = serializedObject.FindProperty("positionOffset");
        rotationOffset = serializedObject.FindProperty("rotationOffset");
        scaleMultiplier = serializedObject.FindProperty("scaleMultiplier");
        stopWhenClipEnds = serializedObject.FindProperty("stopWhenClipEnds");

        audioClip = serializedObject.FindProperty("audioClip");

        attackId = serializedObject.FindProperty("attackId");
        attackHit = serializedObject.FindProperty("attackHit");

        selfBuffs = serializedObject.FindProperty("selfBuffs");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // イベントの種類は常に表示
        EditorGUILayout.PropertyField(eventType, new GUIContent("イベントの種類"));
        EditorGUILayout.Space(4);

        // 種類に合う項目だけを表示する
        switch ((ActionEventType)eventType.intValue)
        {
            case ActionEventType.PlayVfx:
                EditorGUILayout.PropertyField(effectPrefab);
                EditorGUILayout.PropertyField(effectDuration);
                EditorGUILayout.PropertyField(positionOffset);
                EditorGUILayout.PropertyField(rotationOffset);
                EditorGUILayout.PropertyField(scaleMultiplier);
                EditorGUILayout.PropertyField(stopWhenClipEnds);
                break;

            case ActionEventType.PlaySfx:
                EditorGUILayout.PropertyField(audioClip);
                break;

            case ActionEventType.AttackWindow:
                EditorGUILayout.HelpBox(
                    "クリップの開始で判定(Box)が生まれ、終了で消えます。",
                    MessageType.None);
                EditorGUILayout.PropertyField(attackId);
                EditorGUILayout.PropertyField(attackHit, true); // 中の項目も表示
                break;

            case ActionEventType.ComboWindow:
                EditorGUILayout.HelpBox(
                    "設定項目はありません。クリップの間だけコンボ受付になります。",
                    MessageType.None);
                break;

            case ActionEventType.ApplyBuff:
                EditorGUILayout.HelpBox(
                    "クリップの開始時に、自分へバフ・デバフを1回付与します。" +
                    "(エディットモードのプレビューでは付与されません)",
                    MessageType.None);
                EditorGUILayout.PropertyField(selfBuffs, true);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
