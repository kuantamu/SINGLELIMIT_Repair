using UnityEngine;
using UnityEngine.Playables;

public sealed class ActionEventMixer : PlayableBehaviour
{
    public override void ProcessFrame(
        Playable playable,
        FrameData info,
        object playerData)
    {
        if (playerData is not ActionEventReceiver receiver)
            return;

        int count = playable.GetInputCount();

        for (int i = 0; i < count; i++)
        {
            var input = playable.GetInput(i);
            float weight = playable.GetInputWeight(i);

            if (!input.IsValid()) continue;

            var behaviour =
                ((ScriptPlayable<ActionEventBehaviour>)input)
                .GetBehaviour();

            bool active = weight > 0.001f;

            if (active && !behaviour.wasActive)
            {
                if (behaviour.eventType == ActionEventType.PlayVfx)
                    receiver.PlayVfx(behaviour.effectPrefab);

                if (behaviour.eventType == ActionEventType.PlaySfx)
                    receiver.PlaySfx(behaviour.audioClip);

                if (behaviour.eventType == ActionEventType.AttackWindow)
                    receiver.SetAttackWindow(behaviour.attackId, true);

                behaviour.fired = true;
            }

            if (!active && behaviour.wasActive &&
                behaviour.eventType == ActionEventType.AttackWindow)
            {
                receiver.SetAttackWindow(behaviour.attackId, false);
            }

            behaviour.wasActive = active;

            if (!active)
                behaviour.fired = false;
        }
    }
}