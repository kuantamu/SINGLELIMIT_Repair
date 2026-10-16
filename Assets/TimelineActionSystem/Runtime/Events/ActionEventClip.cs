using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public enum ActionEventType
{
    PlayVfx,
    PlaySfx,
    AttackWindow
}

public sealed class ActionEventClip : PlayableAsset
{
    public ActionEventType eventType;

    [Header("VFX")]
    public GameObject effectPrefab;

    [Min(0.01f)]
    public float effectDuration = 2f;

    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public Vector3 scaleMultiplier = Vector3.one;

    public bool stopWhenClipEnds = true;

    [Header("SFX")]
    public AudioClip audioClip;

    [Header("攻撃判定")]
    public string attackId;

    // Timeline 上での初期クリップ長として使用する。
    public override double duration =>
        Mathf.Max(0.01f, effectDuration);

    public override Playable CreatePlayable(
        PlayableGraph graph,
        GameObject owner)
    {
        var playable =
            ScriptPlayable<ActionEventBehaviour>.Create(graph);

        var behaviour = playable.GetBehaviour();

        behaviour.eventType = eventType;
        behaviour.effectPrefab = effectPrefab;
        behaviour.effectDuration = effectDuration;
        behaviour.positionOffset = positionOffset;
        behaviour.rotationOffset = rotationOffset;
        behaviour.scaleMultiplier = scaleMultiplier;
        behaviour.stopWhenClipEnds = stopWhenClipEnds;
        behaviour.audioClip = audioClip;
        behaviour.attackId = attackId;

        return playable;
    }
}

public sealed class ActionEventBehaviour : PlayableBehaviour
{
    public ActionEventType eventType;

    public GameObject effectPrefab;
    public float effectDuration;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public Vector3 scaleMultiplier;
    public bool stopWhenClipEnds;

    public AudioClip audioClip;
    public string attackId;

    public GameObject spawnedEffect;
    public bool wasActive;
}