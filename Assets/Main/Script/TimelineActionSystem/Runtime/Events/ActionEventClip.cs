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

    public GameObject effectPrefab;
    public AudioClip audioClip;
    public string attackId;

    public override Playable CreatePlayable(
        PlayableGraph graph,
        GameObject owner)
    {
        var playable =
            ScriptPlayable<ActionEventBehaviour>.Create(graph);

        var behaviour = playable.GetBehaviour();

        behaviour.eventType = eventType;
        behaviour.effectPrefab = effectPrefab;
        behaviour.audioClip = audioClip;
        behaviour.attackId = attackId;

        return playable;
    }
}

public sealed class ActionEventBehaviour : PlayableBehaviour
{
    public ActionEventType eventType;
    public GameObject effectPrefab;
    public AudioClip audioClip;
    public string attackId;

    public bool wasActive;
    public bool fired;
}
