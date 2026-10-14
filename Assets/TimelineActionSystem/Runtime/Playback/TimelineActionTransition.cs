using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed class TimelineActionTransition
{
    private readonly AnimationMixerPlayable mixer;

    private int currentInput = 0;
    private int nextInput = 1;

    private float elapsed;
    private float duration;
    private bool transitioning;

    public TimelineActionTransition(AnimationMixerPlayable mixer)
    {
        this.mixer = mixer;
    }

    public int CurrentInput => currentInput;
    public int NextInput => nextInput;
    public bool IsTransitioning => transitioning;

    public void Begin(float transitionDuration)
    {
        duration = Mathf.Max(0f, transitionDuration);
        elapsed = 0f;

        if (duration <= 0f)
        {
            Complete();
            return;
        }

        mixer.SetInputWeight(currentInput, 1f);
        mixer.SetInputWeight(nextInput, 0f);

        transitioning = true;
    }

    public void Update(float deltaTime)
    {
        if (!transitioning)
            return;

        elapsed += deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);

        mixer.SetInputWeight(currentInput, 1f - t);
        mixer.SetInputWeight(nextInput, t);

        if (t >= 1f)
            Complete();
    }

    private void Complete()
    {
        mixer.SetInputWeight(currentInput, 0f);
        mixer.SetInputWeight(nextInput, 1f);

        currentInput = nextInput;
        nextInput = 1 - currentInput;

        transitioning = false;
    }
}