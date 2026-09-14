using UnityEngine;

[CreateAssetMenu(menuName = "Buff/Effects/Poison")]
public class Poison : StatusEffect
{
    public Poison(int potency, int count)
    {
        Name = "Poison";
        Potency = potency;
        Count = count;
    }
    public override void Merge(StatusEffect other)
    {
        if (other is Poison poison)
        {
            Potency += poison.Potency;
            Count += poison.Count;
        }
    }
    public override void OnApply(CharactorStatus owner)
    {
        Owner = owner;
        Debug.Log("Poison applied with potency: " + Potency + " and count: " + Count);
    }
    public override void OnReApply() { }
    public override void OnAttack() { }
    public override void OnUpdate(float deltaTime)
    {
        timer += deltaTime;

        if (timer >= Interval)
        {
            timer -= Interval; // 経過時間を保持

            // 一定時間ごとに発動
            Debug.Log($"毒ダメージ {Potency}");
            Owner.AddDamage(Potency);


            Count--;

        }
    }
    public override void OnRemove()
    {
        Debug.Log("Poison effect removed.");
    }
}

[CreateAssetMenu(menuName = "Buff/Effects/AttackUp")]
public class AtkUp : StatusEffect
{
    public AtkUp(int potency, int count)
    {
        Name = "AttackUp";
        Potency = potency;
        Count = count;
    }
    public override void Merge(StatusEffect other)
    {
        if (other is Poison poison)
        {
            Potency = poison.Potency;
            Count += poison.Count;
        }
    }
    public override void OnApply(CharactorStatus owner)
    {
        Owner = owner;
        Owner.statusBase.Attack += Potency;
        Debug.Log("Attack effect applied with potency: " + Potency + " and count: " + Count);
    }
    public override void OnReApply() {

    }
    public override void OnAttack() { }
    public override void OnUpdate(float deltaTime)
    {
        timer += deltaTime;

        if (timer >= Interval)
        {
            timer -= Interval; // 経過時間を保持
            Count--;

        }
    }
    public override void OnRemove() { }
}
