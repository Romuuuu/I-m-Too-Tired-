using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Buffs;

namespace SoTired
{
    public class ModEntry : Mod
    {
        internal static ModConfig Config = null!;
        internal static IMonitor ModMonitor = null!;
        
        private int recordedTimeOfDay;
        private float recordedStamina;
        private bool HasGracePeriodActivatedToday = false;
        private float PenaltyMultiplierForToday = 1.0f;
        private bool wasUsingTool = false;
        private float previousStamina;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = this.Monitor;

            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            recordedTimeOfDay = Game1.timeOfDay;
            recordedStamina = Game1.player.Stamina;
            HasGracePeriodActivatedToday = false;
            PenaltyMultiplierForToday = 1.0f;
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!Config.EnableSleepPenalties) return;

            bool appliedPenalty = false;
            bool gotSuperRest = false;

            if (recordedTimeOfDay <= 1800 && recordedStamina > 0)
            {
                Game1.player.Stamina = Game1.player.MaxStamina;
                gotSuperRest = true;
                
                if (Config.EnableGreatRestingBuff)
                {
                    Game1.player.applyBuff(new Buff(
                        id: "SoTired.GreatResting",
                        source: "SO TIRED!",
                        displaySource: "Great Resting",
                        duration: (int)(Config.GreatRestingDurationMinutes * 60 * 1000),
                        iconTexture: Game1.buffsIcons,
                        iconSheetIndex: 12
                    ));
                }
                Game1.addHUDMessage(new HUDMessage("You have rested well. Your body is recovered.", 2));
            }
            else if (recordedTimeOfDay >= Config.Penalty12AmTimeThreshold && recordedStamina >= Game1.player.MaxStamina * 0.5f)
            {
                Game1.player.Stamina = Game1.player.MaxStamina * Config.Penalty12AmStaminaPercent;
                PenaltyMultiplierForToday = Config.PenaltyStaminaCostMultiplier;
                appliedPenalty = true;
            }
            else if (recordedTimeOfDay >= Config.Penalty11PmTimeThreshold && recordedStamina < Game1.player.MaxStamina * 0.5f)
            {
                Game1.player.Stamina = Game1.player.MaxStamina * Config.Penalty11PMLowStaminaPercent;
                PenaltyMultiplierForToday = Config.PenaltyStaminaCostMultiplier;
                appliedPenalty = true;
            }
            else if (recordedTimeOfDay >= Config.Penalty11PmTimeThreshold && recordedStamina >= Game1.player.MaxStamina * 0.5f)
            {
                Game1.player.Stamina = Game1.player.MaxStamina * Config.Penalty11PmStaminaPercent;
                PenaltyMultiplierForToday = Config.PenaltyStaminaCostMultiplier;
                appliedPenalty = true;
            }

            if (!appliedPenalty && !gotSuperRest && Config.EnableGreatRestingBuff && recordedStamina >= Game1.player.MaxStamina * 0.5f)
            {
                Game1.player.applyBuff(new Buff(
                    id: "SoTired.GreatResting",
                    source: "SO TIRED!",
                    displaySource: "Great Resting",
                    duration: (int)(Config.GreatRestingDurationMinutes * 60 * 1000),
                    iconTexture: Game1.buffsIcons,
                    iconSheetIndex: 12
                ));
            }

            if (appliedPenalty)
            {
                Game1.addHUDMessage(new HUDMessage("You were bad rested last night. Your body is sore and you couldn't sleep well.", 3));
            }

            previousStamina = Game1.player.Stamina;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null) return;

            bool isUsingTool = Game1.player.UsingTool;
            if (isUsingTool && !wasUsingTool)
            {
                OnToolSwung();
            }
            wasUsingTool = isUsingTool;

            if (Config.EnableGracePeriod && !HasGracePeriodActivatedToday)
            {
                float staminaPercent = Game1.player.Stamina / (float)Game1.player.MaxStamina;
                if (staminaPercent <= Config.GracePeriodThresholdPercent && Game1.player.Stamina < previousStamina)
                {
                    ActivateGracePeriod();
                }
            }
            previousStamina = Game1.player.Stamina;

            if (Game1.player.hasBuff("SoTired.GracePeriod"))
            {
                // Ensure player doesn't take damage during the grace period
                Game1.player.temporaryInvincibilityTimer = 1000;
            }
        }

        private void ActivateGracePeriod()
        {
            HasGracePeriodActivatedToday = true;
            
            Game1.player.applyBuff(new Buff(
                id: "SoTired.GracePeriod",
                source: "SO TIRED!",
                displaySource: "Grace Period",
                duration: Config.GracePeriodDurationSeconds * 1000,
                iconTexture: Game1.buffsIcons,
                iconSheetIndex: 21
            ));

            Game1.addHUDMessage(new HUDMessage("Grace Period Activated!", 2));
        }

        private void OnToolSwung()
        {
            if (Game1.player.hasBuff("SoTired.GreatResting")) return;
            if (Game1.player.hasBuff("SoTired.GracePeriod")) return;

            if (Config.EnableRandomZeroStaminaChance && Game1.random.NextDouble() < Config.RandomZeroStaminaChance)
            {
                return;
            }

            var tool = Game1.player.CurrentTool;
            if (tool == null) return;

            float baseExtraCost = Game1.player.MaxStamina * 0.01f;
            baseExtraCost *= Config.StaminaCostMultiplier;
            baseExtraCost *= PenaltyMultiplierForToday;

            int skillLevel = 0;
            if (tool is Pickaxe) skillLevel = Game1.player.MiningLevel;
            else if (tool is Axe) skillLevel = Game1.player.ForagingLevel;
            else if (tool is Hoe || tool is WateringCan) skillLevel = Game1.player.FarmingLevel;
            else if (tool is MeleeWeapon || tool is Slingshot) skillLevel = Game1.player.CombatLevel;
            else if (tool is FishingRod) skillLevel = Game1.player.FishingLevel;

            float proficiencyReduction = 1.0f - (skillLevel * 0.05f);
            if (proficiencyReduction < 0.5f) proficiencyReduction = 0.5f;

            float finalCost = baseExtraCost * proficiencyReduction;
            
            Game1.player.Stamina -= finalCost;
        }
    }
}
