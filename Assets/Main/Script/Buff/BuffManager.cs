using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager
{
    private List<StatusEffect> activeEffects = new List<StatusEffect>();

    public void AddEffect(StatusEffect effect, CharactorStatus owner)
    {
        var current = activeEffects.FirstOrDefault(x => x.GetType() == effect.GetType());

        if (current != null)
        {
            current.Merge(effect);
            current.OnReApply();
        }
        else
        {
            activeEffects.Add(effect);
            effect.OnApply(owner);
        }
        
    }

    public void UpdateEffects()
    {
        foreach (var effect in activeEffects)
        {
            effect.OnUpdate(Time.deltaTime);
            if (effect.IsExpired())
            {
                effect.OnRemove();
            }
        }
        activeEffects.RemoveAll(effect => effect.IsExpired());
    }

    public void Attack()
    {
        foreach (var effect in activeEffects)
        {
            effect.OnAttack();
        }
    }
}
