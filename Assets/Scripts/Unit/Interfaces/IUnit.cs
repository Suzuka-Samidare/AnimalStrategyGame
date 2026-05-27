// 共通
public interface IUnit
{
    UnitStats BaseStats { get; }
    UnitController BaseController { get; }
}

// 攻撃系ロール
public interface IAttackable
{
    AttackUnitStats AttackStats { get; }
    AttackUnitController AttackController { get; }
}

// 防衛系ロール
public interface IDefendable
{
    DefenceUnitStats DefenceStats { get; }
    // DefenceUnitController DefenceController { get; }
}

// サポート系ロール
public interface ISupportable
{
    SupportUnitStats SupportStats { get; }
    // SupportUnitController SupportController { get; }
}

