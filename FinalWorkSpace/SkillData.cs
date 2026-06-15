using System;

namespace Peace_of_Tanks
{
    public enum TankSkillType
    {
        RepairPulse,
        Dash,
        Barrage,
        Overdrive,
        Lifesteal
    }

    internal static class TankSkillCatalog
    {
        public static TankSkillType GetSkillType(TankType tankType)
        {
            switch (tankType)
            {
                case TankType.Yellow:
                    return TankSkillType.Dash;
                case TankType.Blue:
                    return TankSkillType.Barrage;
                case TankType.Red:
                    return TankSkillType.Overdrive;
                case TankType.Purple:
                    return TankSkillType.Lifesteal;
                default:
                    return TankSkillType.RepairPulse;
            }
        }

        public static double GetCooldownSeconds(TankSkillType skillType)
        {
            switch (skillType)
            {
                case TankSkillType.Dash:
                    return 15.0;
                case TankSkillType.Barrage:
                    return 20.0;
                case TankSkillType.Overdrive:
                    return 22.0;
                case TankSkillType.Lifesteal:
                    return 25.0;
                default:
                    return 18.0;
            }
        }

        public static string GetDisplayName(TankSkillType skillType)
        {
            switch (skillType)
            {
                case TankSkillType.Dash:
                    return "推进冲刺";
                case TankSkillType.Barrage:
                    return "弹幕齐射";
                case TankSkillType.Overdrive:
                    return "过载射击";
                case TankSkillType.Lifesteal:
                    return "血能汲取";
                default:
                    return "快速维修";
            }
        }

        public static string GetDescription(TankSkillType skillType)
        {
            switch (skillType)
            {
                case TankSkillType.Dash:
                    return "沿当前朝向快速突进一段距离。";
                case TankSkillType.Barrage:
                    return "接下来三次射击变为三连发。";
                case TankSkillType.Overdrive:
                    return "短时间内提升伤害并缩短开火间隔。";
                case TankSkillType.Lifesteal:
                    return "短时间内按造成伤害的一部分恢复自身生命。";
                default:
                    return "立即恢复一部分生命值。";
            }
        }
    }
}
