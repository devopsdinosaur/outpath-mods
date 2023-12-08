using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using System.Reflection;

[BepInPlugin("devopsdinosaur.outpath.more_resources", "More Resources", "0.0.1")]
public class MoreResourcesPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.outpath.more_resources");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	public static ConfigEntry<float> m_drop_multiplier;
	public static ConfigEntry<float> m_credits_multiplier;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_drop_multiplier = this.Config.Bind<float>("General", "Drop Multiplier", 1f, "Multiplier for amount of dropped resources (float)");
			m_credits_multiplier = this.Config.Bind<float>("General", "Credits Multiplier", 1f, "Multiplier for credits (float)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.outpath.more_resources v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(PlayerGarden), "AddCredits")]
	class HarmonyPatch_PlayerGarden_AddCredits {
		
		private static bool Prefix(ref float _quantity) {
			try {
				if (m_enabled.Value) {
					_quantity *= m_credits_multiplier.Value;
				}
			} catch (Exception e) {
				logger.LogError($"** HarmonyPatch_PlayerGarden_AddCredits ERROR - {e}");
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PlayerGarden), "spawnCreditsText")]
	class HarmonyPatch_PlayerGarden_spawnCreditsText {
		
		private static bool Prefix(ref float _credits) {
			try {
				if (m_enabled.Value) {
					_credits *= m_credits_multiplier.Value;
				}
			} catch (Exception e) {
				logger.LogError($"** HarmonyPatch_PlayerGarden_spawnCreditsText ERROR - {e}");
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(TakeOutResource), "SpawnDrops")]
	class HarmonyPatch_TakeOutResource_SpawnDrops {
		
		private static bool Prefix(ref float _mult) {
			try {
				if (m_enabled.Value) {
					_mult = m_drop_multiplier.Value;
				}
			} catch (Exception e) {
				logger.LogError($"** HarmonyPatch_TakeOutResource_SpawnDrops ERROR - {e}");
			}
			return true;
		}
	}
}