using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using System.Reflection;

[BepInPlugin("devopsdinosaur.outpath.super_food", "Super Food", "0.0.1")]
public class SuperFoodPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.outpath.super_food");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	public static ConfigEntry<float> m_food_time_multiplier;
	public static ConfigEntry<float> m_food_power_multiplier;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_food_time_multiplier = this.Config.Bind<float>("General", "Food Time Multiplier", 1f, "Multiplier for amount of time food buff lasts (float)");
			m_food_power_multiplier = this.Config.Bind<float>("General", "Food Power Multiplier", 1f, "Multiplier for power of food buff (float)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.outpath.super_food v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}
	
	[HarmonyPatch(typeof(StatusManager), "AddEffect")]
	class HarmonyPatch_StatusManager_AddEffect {
		
		private static bool Prefix(StatusManager __instance, int _index, ItemInfo _itemInfo) {	
			try {
				if (_index == -1 || __instance.EffectDurationExceeded(_itemInfo.duration, _index)) {
					return false;
				}
				StatusTimer statusTimer = null;
				for (int i = 0; i < __instance.currStatusTimerList.Count; i++) {
					if (__instance.currStatusTimerList[i].itemInfo == _itemInfo) {
						statusTimer = __instance.currStatusTimerList[i];
						break;
					}
				}
				float num = 0f;
				if (UpgradesData.instance.GetUpgrade_Bool(14)) {
					num = _itemInfo.duration * 0.5f * (float) UpgradesData.instance.GetUpgrade_Int(14);
				}
				if (statusTimer == null) {
					StatusTimer statusTimer2 = UnityEngine.Object.Instantiate(__instance.statusTimerPrefab, __instance.statusTimerParent);
					statusTimer2.SetupInfo(__instance.statusList[_index], _itemInfo, _itemInfo.itemIcon, (_itemInfo.duration + num) * m_food_time_multiplier.Value);
					__instance.effectsPowerList[_index] += _itemInfo.power * m_food_power_multiplier.Value;
					__instance.currStatusTimerList.Add(statusTimer2);
				}
				else {
					statusTimer.AddTimeToDestroy(_itemInfo.resetDurationUponEat, _itemInfo.duration + num);
				}
				if (_itemInfo.isPotion) {
					SteamIntegration.instance.UnlockAchievement("First Sip", 22);
				}
				int num2 = 0;
				for (int j = 0; j < __instance.statusTimerParent.childCount; j++) {
					StatusTimer component = __instance.statusTimerParent.GetChild(j).GetComponent<StatusTimer>();
					if (component != null && component.itemInfo.isPotion) {
						num2++;
					}
				}
				if (num2 >= __instance.maxPotionEffects) {
					SteamIntegration.instance.UnlockAchievement("Effects Overload", 23);
				}
				int num3 = 0;
				for (int k = 0; k < __instance.statusTimerParent.childCount; k++) {
					StatusTimer component2 = __instance.statusTimerParent.GetChild(k).GetComponent<StatusTimer>();
					if (component2 != null && (component2.itemInfo.itemID == 246 || component2.itemInfo.itemID == 244 || component2.itemInfo.itemID == 243)) {
						num3++;
					}
				}
				if (num3 >= 3) {
					SteamIntegration.instance.UnlockAchievement("My diet", 65);
				}
				return false;
			} catch (Exception e) {
				logger.LogError($"** HarmonyPatch_StatusManager_AddEffect ERROR - {e}");
			}
			return true;
		}
	}
}