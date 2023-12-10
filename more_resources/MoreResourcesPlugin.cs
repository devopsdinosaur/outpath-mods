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
		
		private static bool Prefix(TakeOutResource __instance) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				for (int i = 0; i < __instance.itemPoolGroups.Length; i++) {
					if (!(UnityEngine.Random.value < __instance.itemPoolGroups[i].probToSpawn)) {
						continue;
					}
					float num = UnityEngine.Random.Range(__instance.itemPoolGroups[i].quantityToSpawn.x, __instance.itemPoolGroups[i].quantityToSpawn.y) * m_drop_multiplier.Value * (__instance.mult_PropFromCrop * __instance.mult_IsTresure);
					int num2 = Mathf.RoundToInt(num + (float)Mathf.RoundToInt(num * 0.3f * (float)UpgradesData.instance.GetUpgrade_Int(1)));
					if (num2 >= 1) {
						int num3 = num2 / __instance.itemPoolGroups[i].quantityItemDrops;
						if (num3 <= 0) {
							num3 = 1;
						}
						for (int j = 0; j < __instance.itemPoolGroups[i].quantityItemDrops; j++) {
							InventoryManager.instance.AddItemToInv((ItemInfo) __instance.GetType().GetMethod("GetItemInfo", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {i}), num3);
						}
					}
				}
				if (__instance.isRock) {
					float num4 = UpgradesData.instance.GetUpgrade_Int(39);
					num4 *= m_drop_multiplier.Value;
					if (num4 >= 1f) {
						InventoryManager.instance.AddItemToInv(ItemList.instance.itemList[4], (int) num4);
					}
				}
				return false;
			} catch (Exception e) {
				logger.LogError($"** HarmonyPatch_TakeOutResource_SpawnDrops ERROR - {e}");
			}
			return true;
		}
	}

	
	[HarmonyPatch(typeof(EnemyHealth), "SpawnDrops")]
	class HarmonyPatch_EnemyHealth_SpawnDrops {
		
		private static bool Prefix(EnemyHealth __instance, float mult) {
			if (!m_enabled.Value) {
				return true;
			}
			__instance.SpawnDropsAndPickup(mult * m_drop_multiplier.Value);
			return false;
		}
	}
}