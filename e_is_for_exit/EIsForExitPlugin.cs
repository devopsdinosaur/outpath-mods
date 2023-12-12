using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using System.Collections.Generic;


[BepInPlugin("devopsdinosaur.outpath.e_is_for_exit", "E is for Exit", "0.0.1")]
public class EIsForExitPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.outpath.e_is_for_exit");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;

	public static float m_craft_menu_open_elapsed = 0;
	public static bool m_do_spoof_escape = false;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.outpath.e_is_for_exit v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(CraftManager), "ToggleMenu")]
	class HarmonyPatch_CraftManager_ToggleMenu {
		
		private static bool Prefix(bool _state) {
			if (_state) {
				m_craft_menu_open_elapsed = 0;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Build_Research), "OpenCraftMenu")]
	class HarmonyPatch_Build_Research_OpenCraftMenu {
		
		private static bool Prefix() {
			m_craft_menu_open_elapsed = 0;
			return true;
		}
	}

	[HarmonyPatch(typeof(Build_Bed), "OpenCraftMenu")]
	class HarmonyPatch_Build_Bed_OpenCraftMenu {
		
		private static bool Prefix() {
			m_craft_menu_open_elapsed = 0;
			return true;
		}
	}

	[HarmonyPatch(typeof(InventoryManager), "HideBars")]
	class HarmonyPatch_InventoryManager_HideBars {
		
		private static bool Prefix() {
			m_craft_menu_open_elapsed = 0;
			return true;
		}
	}

	[HarmonyPatch(typeof(PauseUI), "Update")]
	class HarmonyPatch_testing_1 {
		
		const int MENU_CRAFT = 2;
		const int MENU_BUILD = 3;
		const int MENU_CREDIT = 4;
		const int MENU_BED = 5;
		const int MENU_MARKET = 6;
		const int MENU_RESEARCH = 7;

		static Dictionary<int, string> menu_key_name_map = null;

		private static bool Prefix() {
			if (menu_key_name_map == null) {
				menu_key_name_map = new Dictionary<int, string>();
				menu_key_name_map[MENU_CRAFT] = 
					menu_key_name_map[MENU_CREDIT] = 
					menu_key_name_map[MENU_BED] = 
					menu_key_name_map[MENU_MARKET] =
					menu_key_name_map[MENU_RESEARCH] = 
					"Interact";
				menu_key_name_map[MENU_BUILD] = "Build Menu";
			}
			if (!(m_enabled.Value && menu_key_name_map.ContainsKey(PlayerGarden.instance.inMenu)) || (m_craft_menu_open_elapsed += Time.deltaTime) < 0.025f) {
				return true;
			}
			if (CharacterInput.instance.GetKeyState_Down(menu_key_name_map[PlayerGarden.instance.inMenu])) {
				m_do_spoof_escape = true;
				m_craft_menu_open_elapsed = 0;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(CharacterInput), "Update")]
	class HarmonyPatch_CharacterInput_Update {
		
		private static void Postfix(CharacterInput __instance) {
			if (!m_do_spoof_escape) {
				return;
			}
			m_do_spoof_escape = false;
			for (int index = 0; index < __instance.actionsList.Length; index++) {
				if (__instance.actionsList[index].actionName == "Pause Menu") {
					__instance.actionsList[index].isActionDown = true;
					return;
				}
			}
		}
	}
}