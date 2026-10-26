using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【再生中の攻撃判定デバッグ表示】
/// ActionEventReceiver と同じオブジェクトに付けると、
/// 再生中に有効な攻撃判定(Box)が色付きで見えるようになります。
/// 付けなければ何も表示されないので、本番ではこのコンポーネントを外すだけで消せます。
///
/// ■ 見え方
///  ・Scene ビュー … 常に表示
///  ・Game ビュー  … 右上の「Gizmos」ボタンをONにすると表示
///
/// ■ エディットモード(Timeline のプレビュー)
///  このコンポーネントが無くても、ActionEventReceiver が自動で Scene ビューに表示します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ActionEventReceiver))]
public sealed class AttackHitboxDebugView : MonoBehaviour
{
    [Header("表示")]
    [Tooltip("OFF にすると再生中の表示を止める")]
    [SerializeField] private bool visible = true;

    [Header("色")]
    [SerializeField] private Color fillColor = new Color(1f, 0.15f, 0.15f, 0.25f);
    [SerializeField] private Color lineColor = new Color(1f, 0.2f, 0.2f, 1f);

    private ActionEventReceiver receiver;

    /// <summary>表示のON/OFF(他のスクリプトやデバッグメニューから切り替え可)</summary>
    public bool Visible
    {
        get => visible;
        set => visible = value;
    }

    /// <summary>表示のON/OFFを切り替える</summary>
    [ContextMenu("表示を切り替え")]
    public void Toggle()
    {
        visible = !visible;
    }

    private void OnDrawGizmos()
    {
        // エディットモードの表示は ActionEventReceiver 側が担当
        if (!Application.isPlaying || !visible)
            return;

        if (receiver == null)
            receiver = GetComponent<ActionEventReceiver>();

        if (receiver == null)
            return;

        DrawBoxes(receiver.ActiveAttackBoxes, fillColor, lineColor);
    }

    /// <summary>攻撃判定の Box を塗りと枠線で描く(エディット・再生中で共通)</summary>
    public static void DrawBoxes(
        IReadOnlyList<ActionEventReceiver.AttackBox> boxes,
        Color fill,
        Color line)
    {
        if (boxes == null)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;

        foreach (var box in boxes)
        {
            if (box == null || box.Root == null)
                continue;

            Gizmos.matrix = box.LocalToWorld;

            Gizmos.color = fill;
            Gizmos.DrawCube(Vector3.zero, box.Size);

            Gizmos.color = line;
            Gizmos.DrawWireCube(Vector3.zero, box.Size);
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
