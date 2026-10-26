using System;
using UnityEngine;

/// <summary>
/// バフ・デバフの種類。
/// 新しい効果を追加するときは、ここに項目を足して
/// BuffEffects.cs に効果クラスを作り、BuffFactory.cs に登録します。
/// </summary>
public enum BuffType
{
    Poison,    // 毒(デバフ):一定間隔でダメージ
    AttackUp,  // 攻撃力アップ(バフ)
}

/// <summary>
/// 「どのバフを、何層、何秒付与するか」をまとめたデータ。
/// インスペクターで設定でき、攻撃時付与などに使います。
///
/// ■ 秒数のルール
///  ・OverrideDuration が OFF … そのバフの標準秒数を使う
///  ・OverrideDuration が ON  … Duration の秒数を使う
///  ・Duration がマイナス     … 時間で消えない(永続)
/// </summary>
[Serializable]
public struct BuffGrant
{
    /// <summary>付与するバフの種類</summary>
    public BuffType Type;

    /// <summary>付与する層数(1以上)</summary>
    [Min(1)] public int Stacks;

    /// <summary>true なら下の Duration を使う。false ならバフの標準秒数</summary>
    public bool OverrideDuration;

    /// <summary>付与する秒数(マイナスで永続)。OverrideDuration が ON のときだけ有効</summary>
    public float Duration;

    /// <summary>標準の秒数で付与するデータを作る</summary>
    public BuffGrant(BuffType type, int stacks = 1)
    {
        Type = type;
        Stacks = stacks;
        OverrideDuration = false;
        Duration = 0f;
    }

    /// <summary>秒数を指定して付与するデータを作る(マイナスで永続)</summary>
    public BuffGrant(BuffType type, int stacks, float duration)
    {
        Type = type;
        Stacks = stacks;
        OverrideDuration = true;
        Duration = duration;
    }

    /// <summary>実際に使う秒数。標準秒数を使う場合は null を返す</summary>
    public float? GetDuration()
    {
        return OverrideDuration ? Duration : (float?)null;
    }
}
