using System.Collections.Generic;
using UnityEngine;

public sealed class ActionEventReceiver : MonoBehaviour
{
    [SerializeField] private Transform effectRoot;
    [SerializeField] private AudioSource audioSource;

    private readonly HashSet<string> activeAttacks = new();

    public void PlayVfx(GameObject prefab)
    {
        if (prefab == null) return;

        Transform root = effectRoot != null ? effectRoot : transform;

        Instantiate(prefab, root.position, root.rotation, root);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    public void SetAttackWindow(string attackId, bool active)
    {
        if (string.IsNullOrWhiteSpace(attackId)) return;

        bool changed = active
            ? activeAttacks.Add(attackId)
            : activeAttacks.Remove(attackId);

        if (!changed) return;

        // 既存の戦闘システムへの接続ポイント
        Debug.Log($"Attack: {attackId}, Active: {active}", this);
    }

    public void ResetAllAttackWindows()
    {
        foreach (string attackId in activeAttacks)
            Debug.Log($"Attack: {attackId}, Active: false", this);

        activeAttacks.Clear();

        // 実装時は戦闘システムにも全解除を通知する。
    }
}