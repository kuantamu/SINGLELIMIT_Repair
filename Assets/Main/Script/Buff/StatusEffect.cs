using System;
using UnityEngine;
using UnityEngine.UI;
public abstract class StatusEffect:ScriptableObject
{
    public String Name;
    public int Potency;
    public int Count;
    public Image Icon; 
    public CharactorStatus Owner = null;
    public float Interval = 1.0f;
    public bool IsActive = true;

    protected float timer = 0.0f;
    public abstract void Merge(StatusEffect other);
    public abstract void OnApply(CharactorStatus owner);
    public abstract void OnReApply();
    public abstract void OnAttack();
    public abstract void OnUpdate(float deltaTime);
    public abstract void OnRemove();

    public virtual bool IsExpired()
    {
        return Count <= 0 || !IsActive;
    }

}
