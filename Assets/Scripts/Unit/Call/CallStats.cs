public class CallStats : UnitStatsBase
{
    public override void Initialize(UnitData unitData)
    {
        this.profile = unitData.callingProfile;
        hp = profile.maxHp;
    }
}
