namespace SoTired
{
    public class ModConfig
    {
        // Stamina Configuration
        public float StaminaCostMultiplier { get; set; } = 1.85f; // Yields ~7 total stamina per swing
        public bool EnableRandomZeroStaminaChance { get; set; } = true;
        public float RandomZeroStaminaChance { get; set; } = 0.25f;

        // Grace Period Configuration
        public bool EnableGracePeriod { get; set; } = true;
        public float GracePeriodThresholdPercent { get; set; } = 0.10f; // 10%
        public int GracePeriodDurationSeconds { get; set; } = 15;

        // Sleep Penalties
        public bool EnableSleepPenalties { get; set; } = true;
        
        // Times are in Stardew Valley format (e.g. 2300 = 11:00 PM, 2400 = 12:00 AM)
        public int Penalty11PmTimeThreshold { get; set; } = 2300;
        public float Penalty11PmStaminaPercent { get; set; } = 0.75f;
        
        public int Penalty12AmTimeThreshold { get; set; } = 2400;
        public float Penalty12AmStaminaPercent { get; set; } = 0.50f;
        
        public float Penalty11PMLowStaminaPercent { get; set; } = 0.35f;
        
        public float PenaltyStaminaCostMultiplier { get; set; } = 1.5f;

        // Buff Configuration
        public bool EnableGreatRestingBuff { get; set; } = true;
        public float GreatRestingDurationMinutes { get; set; } = 3.5f;
    }
}
