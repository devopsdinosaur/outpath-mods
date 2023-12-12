using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using System.Reflection;
using OverfortGames.FirstPersonController;

using MyPooler;


[BepInPlugin("devopsdinosaur.outpath.testing", "Testing", "0.0.1")]
public class TestingPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.outpath.testing");
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
			logger.LogInfo("devopsdinosaur.outpath.testing v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	public static bool list_descendants(Transform parent, Func<Transform, bool> callback, int indent) {
		Transform child;
		string indent_string = "";
		for (int counter = 0; counter < indent; counter++) {
			indent_string += " => ";
		}
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			logger.LogInfo(indent_string + child.gameObject.name);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			list_descendants(child, callback, indent + 1);
		}
		return true;
	}

	public static bool enum_descendants(Transform parent, Func<Transform, bool> callback) {
		Transform child;
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			enum_descendants(child, callback);
		}
		return true;
	}

	public static void list_component_types(Transform obj) {
		foreach (Component component in obj.GetComponents<Component>()) {
			logger.LogInfo(component.GetType().ToString());
		}
	}

	[HarmonyPatch(typeof(DayNightCycle), "Update")]
	class HarmonyPatch_DayNightCycle_Update {
		
		private static void Postfix(DayNightCycle __instance) {	
		}
	}

	[HarmonyPatch(typeof(PlayerGarden), "Update")]
	class HarmonyPatch_PlayerGarden_Update {
		
		private static bool Prefix() {	
			return true;
		}
	}

}