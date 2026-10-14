using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.2f, 0.8f, 0.6f)]
[TrackClipType(typeof(ActionEventClip))]
[TrackBindingType(typeof(ActionEventReceiver))]
public sealed class ActionEventTrack : TrackAsset
{
    public override Playable CreateTrackMixer(
        PlayableGraph graph,
        GameObject go,
        int inputCount)
    {
        return ScriptPlayable<ActionEventMixer>.Create(
            graph,
            inputCount
        );
    }
}