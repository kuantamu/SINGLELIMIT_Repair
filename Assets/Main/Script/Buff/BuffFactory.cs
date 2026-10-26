using System;

/// <summary>
/// BuffType から効果クラスのインスタンスを作る工場。
/// 新しい効果を追加したら、ここに case を1行足します。
/// </summary>
public static class BuffFactory
{
    /// <summary>指定した種類の効果を新規作成する(層数は0の状態)</summary>
    public static StatusEffect Create(BuffType type)
    {
        switch (type)
        {
            case BuffType.Poison:   return new Poison();
            case BuffType.AttackUp: return new AttackUp();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "未登録のバフ種類です。BuffFactory に追加してください。");
        }
    }
}
