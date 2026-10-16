using UnityEngine;

[RequireComponent(typeof(DamagePopupGenerator))]
public class CharactorStatus : MonoBehaviour
{
    [SerializeField] public StatusBase statusBase = new StatusBase();

    [SerializeField] public BuffManager buffManager = new BuffManager();

    [SerializeField] bool isEnemy = false;

    private DamagePopupGenerator damagePopupGenerator;

    private void Awake()
    {
        statusBase.CurrentHP = statusBase.MaxHP;
        damagePopupGenerator = GetComponent<DamagePopupGenerator>();
    }

    private void Update()
    {
        buffManager.UpdateEffects();
    }

    public void ReceiveDamage(CharactorStatus target)
    {
        CharaAttack(target);
    }

    public void CharaAttack(CharactorStatus tagetStatus)
    {
        DamageInfo damage = new DamageInfo()
        {
            AttackPower = statusBase.Attack,
            IsCritical = Random.Range(0.0f,100.0f) < statusBase.CriticalRate,
            CriticalMultiplier = statusBase.CriticalMult
        };
        tagetStatus.TakeDamage(damage);
        tagetStatus.AddBuff(new Poison(5, 3));

    }

    public void AddBuff(StatusEffect effect)
    {
        damagePopupGenerator.Buff(effect.Name, effect.Potency);
        buffManager.AddEffect(effect, this);
    }

    public void AddDamage(int damage)
    {
        statusBase.CurrentHP -= damage;
        damagePopupGenerator.Damage(damage);
    }

    public void TakeDamage(DamageInfo damage)
    {
        if (statusBase.HitState == HitState.Invincible) {
            damagePopupGenerator.Damage(0);
            return;
        }
        float Critical = damage.IsCritical ? damage.CriticalMultiplier : 1;
        float dam = damage.AttackPower * Critical;
        statusBase.CurrentHP -= dam;
        damagePopupGenerator.Damage((int)dam);
        Debug.Log(statusBase.CurrentHP + "/" + statusBase.MaxHP + "Critical:" + damage.IsCritical);
    }
}
