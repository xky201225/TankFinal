using System.Collections.Generic;

namespace Peace_of_Tanks
{
    internal sealed class UserSyncPayload
    {
        public string UserName { get; set; } = string.Empty;
        public int TotalExp { get; set; }
        public int SelectedTank { get; set; }
        public List<TankSyncPayload> Tanks { get; set; } = new List<TankSyncPayload>();
    }

    internal sealed class TankSyncPayload
    {
        public int TankType { get; set; }
        public int Level { get; set; }
        public int SpeedLevel { get; set; }
        public int AttackLevel { get; set; }
        public int HealthLevel { get; set; }
        public int BulletSpeedLevel { get; set; }
        public int CooldownLevel { get; set; }
        public int CurrentExp { get; set; }
        public bool IsUnlocked { get; set; }
    }
}
