// 共通
public interface IUnit
{
    UnitStats Stats { get; }
    UnitController Controller { get; }
}

// 攻撃系ロール
public interface IAttackable
{
    AttackUnitStats Stats { get; }
    AttackUnitController Controller { get; }
}

// 防衛系ロール
public interface IDefendable
{
    DefenceUnitStats Stats { get; }
    DefenceUnitController Controller { get; }
}

// サポート系ロール
public interface ISupportable
{
    SupportUnitStats Stats { get; }
    // SupportUnitController Controller { get; }
}

public interface ICallable
{
    CallingUnitController Controller { get; }
}

