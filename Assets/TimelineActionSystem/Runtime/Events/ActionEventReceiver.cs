using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ActionEventReceiver : MonoBehaviour
{
    [SerializeField] private Transform effectRoot;
    [SerializeField] private AudioSource audioSource;

    private readonly HashSet<string> activeAttacks = new();
    private readonly HashSet<GameObject> activeVfx = new();

    public GameObject PlayVfx(
        GameObject prefab,
        float duration,
        Vector3 positionOffset,
        Vector3 rotationOffset,
        Vector3 scaleMultiplier)
    {
        if (prefab == null)
            return null;

        Transform root = effectRoot != null
            ? effectRoot
            : transform;

        Vector3 position = root.TransformPoint(positionOffset);

        Quaternion rotation =
            root.rotation * Quaternion.Euler(rotationOffset);


        GameObject instance = Instantiate(
            prefab, position, rotation, root);

        instance.transform.localScale = Vector3.Scale(
            instance.transform.localScale,
            scaleMultiplier);

        activeVfx.Add(instance);

        if (Application.isPlaying)
        {
            StartCoroutine(
                DestroyVfxAfter(instance, Mathf.Max(0.01f, duration)));
        }
        else
        {
            // エディットモードのプレビュー専用オブジェクト
            instance.hideFlags = HideFlags.DontSave;
        }

        return instance;

    }

    private IEnumerator DestroyVfxAfter(
        GameObject instance,
        float duration)
    {
        yield return new WaitForSeconds(duration);

        if (instance != null)
            StopVfx(instance);
    }


    public void StopVfx(GameObject instance)
    {
        if (instance == null)
            return;

        activeVfx.Remove(instance);

        if (Application.isPlaying)
            Destroy(instance);
        else
            DestroyImmediate(instance);
    }

    public void StopAllVfx()
    {
        foreach (GameObject instance in activeVfx)
        {
            if (instance == null)
                continue;

            if (Application.isPlaying)
                Destroy(instance);
            else
                DestroyImmediate(instance);
        }

        activeVfx.Clear();
    }


    public void PlaySfx(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    public void SetAttackWindow(string attackId, bool active)
    {
        if (string.IsNullOrWhiteSpace(attackId))
            return;

        bool changed = active
            ? activeAttacks.Add(attackId)
            : activeAttacks.Remove(attackId);

        if (!changed)
            return;

        Debug.Log(
            $"Attack: {attackId}, Active: {active}", this);

        // ここで実際の攻撃判定システムと接続する。
    }

    public void ResetAllAttackWindows()
    {
        activeAttacks.Clear();
        StopAllVfx();

        // 実際の攻撃判定システムにも終了を通知する。
    }
}