using System;
using System.Collections.Generic;
using UnityEngine;

public enum AttributeType
{
    Slash,   // aŒ‚
    Pierce,  // ŠÑ’Ê
    Strike   // ‘ÅŒ‚
}

public enum HitState
{
    Normal,
    Defense,
    Invincible
}

[Serializable]
public struct StatusBase
{
    [SerializeField]
    public HitState HitState;
    //Å‘åHP
    [SerializeField]
    public float MaxHP;
    //Œ»İHP
    [SerializeField]
    public float CurrentHP;
    //UŒ‚
    [SerializeField]
    public float Attack;

    [SerializeField]
    public float CriticalRate;

    [SerializeField]
    public float CriticalMult;

    [SerializeField]
    public float DefenseRate;

    [SerializeField]
    public float SlashResistance;
    [SerializeField]
    public float PierceResistance;
    [SerializeField]
    public float StrikeResistance;

    public float GetResistance(AttributeType type)
    {
        switch (type)
        {
            case AttributeType.Slash:
                return SlashResistance;
            case AttributeType.Pierce:
                return PierceResistance;
            case AttributeType.Strike:
                return StrikeResistance;
        }
        return 1.0f;
    }

    public void GetStatusLog()
    {
        Debug.Log(
            "HP:" + CurrentHP
            + "\nUŒ‚" + Attack
            + "\n‹}Š—¦" + CriticalRate
            + "\n‹}Š”{—¦" + CriticalMult
            + "\naŒ‚‘Ï«" + SlashResistance
            + "\nŠÑ’Ê‘Ï«" + PierceResistance
            + "\n‘ÅŒ‚‘Ï«" + StrikeResistance
            );

    }
}