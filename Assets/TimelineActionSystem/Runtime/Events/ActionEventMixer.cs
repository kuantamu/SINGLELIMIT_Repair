using UnityEngine;
using UnityEngine.Playables;

public sealed class ActionEventMixer : PlayableBehaviour
{

    private ActionEventReceiver cachedReceiver;

    public override void OnGraphStop(Playable playable)
    {
        // エディットモードのプレビューだけを後片付けする。
        // ゲーム実行中のエフェクトの寿命には干渉しない。
        if (!Application.isPlaying && cachedReceiver != null)
        {
            cachedReceiver.StopAllVfx();
            cachedReceiver = null;
        }
    }


    public override void ProcessFrame(
        Playable playable,
        FrameData info,
        object playerData)
    {
        if (playerData is not ActionEventReceiver receiver)
            return;

        cachedReceiver = receiver;

        int count = playable.GetInputCount();

        // クリップのアクティブ状態を確認して、イベントを発火させる。
        for (int i = 0; i < count; i++)
        {
            Playable input = playable.GetInput(i);

            if (!input.IsValid())
                continue;

            float weight = playable.GetInputWeight(i);

            var behaviour =
                ((ScriptPlayable<ActionEventBehaviour>)input)
                .GetBehaviour();

            bool active = weight > 0.001f;

            // クリップ開始
            if (active && !behaviour.wasActive)
            {
                switch (behaviour.eventType)
                {
                    case ActionEventType.PlayVfx:
                        behaviour.spawnedEffect = receiver.PlayVfx(
                            behaviour.effectPrefab,
                            behaviour.effectDuration,
                            behaviour.positionOffset,
                            behaviour.rotationOffset,
                            behaviour.scaleMultiplier);
                        break;

                    case ActionEventType.PlaySfx:
                        receiver.PlaySfx(behaviour.audioClip);
                        break;

                    case ActionEventType.AttackWindow:
                        receiver.SetAttackWindow(
                            behaviour.attackId, true);
                        break;
                    case ActionEventType.ComboWindow:
                        receiver.SetComboWindow(true);
                        break;
                }
            }

            // クリップ終了
            if (!active && behaviour.wasActive)
            {
                switch (behaviour.eventType)
                {
                    case ActionEventType.PlayVfx:
                        if (behaviour.stopWhenClipEnds)
                        {
                            receiver.StopVfx(
                                behaviour.spawnedEffect);

                            behaviour.spawnedEffect = null;
                        }
                        break;

                    case ActionEventType.AttackWindow:
                        receiver.SetAttackWindow(
                            behaviour.attackId, false);
                        break;
                    case ActionEventType.ComboWindow:
                        receiver.SetComboWindow(false);
                        break;
                }
            }


            if (!Application.isPlaying &&
                active &&
                behaviour.eventType == ActionEventType.PlayVfx &&
                behaviour.spawnedEffect != null)
            {
                float previewTime = Mathf.Max(
                    0f, (float)input.GetTime());

                ParticleSystem[] particles =
                    behaviour.spawnedEffect
                        .GetComponentsInChildren<ParticleSystem>(true);

                foreach (ParticleSystem particle in particles)
                {
                    particle.Simulate(
                        previewTime,
                        withChildren: true,
                        restart: true,
                        fixedTimeStep: true);
                }
            }

            behaviour.wasActive = active;

        }
    }
}