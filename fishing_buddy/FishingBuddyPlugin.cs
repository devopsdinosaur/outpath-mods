using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using MyPooler;


[BepInPlugin("devopsdinosaur.outpath.fishing_buddy", "Fishing Buddy", "0.0.1")]
public class FishingBuddyPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.outpath.fishing_buddy");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_skip_minigame;
	private static ConfigEntry<float> m_catch_speed_multiplier;
	private static ConfigEntry<bool> m_line_tension;
	private static ConfigEntry<float> m_treasure_chance;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_skip_minigame = this.Config.Bind<bool>("General", "Skip Fishing Minigame", false, "(bool) Set to true to skip minigame and catch treasure (if present) and fish immediately.");
			m_catch_speed_multiplier = this.Config.Bind<float>("General", "Catch Speed Multiplier", 1f, "(float) Speed multiplier for how long a click must be applied to fish in order to catch [0 == instant catch when clicked, < 1 == easier to catch (ex: 0.5 is 2x easier), > 1 == harder to catch (ex: 2 is 2x harder)].");
			m_line_tension = this.Config.Bind<bool>("General", "Line Tension", true, "(bool) Set to false to disable the line tension logic, i.e. the line will never break.");
			m_treasure_chance = this.Config.Bind<float>("General", "Treasure Chance", 0f, "(float) If this value is non-zero then it will override bait's treasure chance [0 == disabled, >= 1 == 100% chance, < 1 == % chance].");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.outpath.fishing_buddy v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(PlayerFishingManager), "Update")]
	class HarmonyPatch_PlayerFishingManager_Update {
		
		private static bool Prefix(PlayerFishingManager __instance) {	
			try {
				if (!(m_enabled.Value && __instance.fishingCanvasGroup.gameObject.activeSelf)) {
					return true;
				}
				if (!m_line_tension.Value) {
					__instance.currTension = -99999;
				}
				//logger.LogInfo($"__instance.tresureTrans.gameObject.activeSelf: {__instance.tresureTrans.gameObject.activeSelf}, __instance.treasurePos: {__instance.tresurePos}");
				if (!m_skip_minigame.Value) {
					return true;
				}
				if (__instance.tresureTrans.gameObject.activeSelf) {
					__instance.tresureTrans.gameObject.SetActive(value: false);
					AudioManager2.instance.PlaySound("Tool_FishingRod_CatchTresure");
					ObjectPooler.Instance.GetFromPool("Particle_HitProp", __instance.fishingRodBait.transform.position, Quaternion.identity);
					__instance.Invoke("CatchTresure", 0.5f);
					__instance.StartCoroutine((string) __instance.GetType().GetMethod("CatchTresure", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {__instance.fishingRodBait.transform.position}));
					return false;
				}
				if (__instance.fishTrans.gameObject.activeSelf) {
					__instance.fishTrans.gameObject.SetActive(value: false);
					AudioManager2.instance.PlaySound("Tool_FishingRod_CatchFish");
					__instance.GetType().GetMethod("CatchFish", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
					__instance.DestroyBait();
					return false;
				}
				return true;
			} catch (Exception e) {
				logger.LogError($"** HarmonyPatch_PlayerFishingManager_Update ERROR - {e}");
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PlayerFishingManager), "StartFishingMinigame")]
	class HarmonyPatch_PlayerFishingManager_StartFishingMinigame {
		
		private static bool Prefix(PlayerFishingManager __instance, float ___extraTresureProb) {
			try{
				if (!m_enabled.Value) {
					return true;
				}
				float chance = Math.Max(0, m_treasure_chance.Value);
				if (chance > 0.001) {
					___extraTresureProb = chance;
				}
			} catch (Exception e) {
				logger.LogError($"** HarmonyPatch_PlayerFishingManager_StartFishingMinigame.Prefix ERROR - {e}");
			}
			return true;
		}

		/*
		private static bool Prefix(
			PlayerFishingManager __instance,
			bool ___treasureCollected,
			float ___speed,
			float ___extraTensionMult,
			ItemInfo ____baitInfo,
			float ___tensionMult,
			float ___timeToStartGame,
			float ___extraAddTresureProb,
			float ___extraTresureProb,
			float ___maxSpeed,
			float ___rangeBarSize,
			float ___catchSpeed_Fish,
			float ___catchSpeed_Treasure
		) {	
			try {
				AudioManager2.instance.PlaySound("Tool_FishingRod_StartMiniGame");
				ObjectPooler.Instance.GetFromPool("Particle_HitProp", __instance.fishingRodBait.transform.position, Quaternion.identity);
				___treasureCollected = false;
				___speed = 0f;
				__instance.currTension = 0f;
				___extraTensionMult = 1f;
				if (____baitInfo != null) {
					if (____baitInfo.itemID == 193) {
						___extraTensionMult = 0.6f;
					} else if (____baitInfo.itemID == 60) {
						___extraTensionMult = 0.8f;
					}
				}
				___tensionMult = UnityEngine.Random.Range(0.8f, 1.5f) * ___extraTensionMult;
				___timeToStartGame = 0f;
				__instance.fishPos = UnityEngine.Random.Range(20, 340);
				__instance.fishPos %= 360f;
				if (__instance.fishPos < 0f) {
					__instance.fishPos += 360f;
				}
				__instance.fishPos = Mathf.Repeat(__instance.fishPos, 360f);
				BiomeFishingInfo biomeFishingInfo = ((!MineManager.instance.inMine) ? IslandsManager.instance.currIsleParentGenerator.biomeInfo.biomeFishingInfo : MineManager.instance.currRoomInWorld.mineRoomInfo.biomeFishingInfo);
				__instance.maxFishCatch = UnityEngine.Random.Range(biomeFishingInfo.minMaxFishing_MaxFishCatch.x, biomeFishingInfo.minMaxFishing_MaxFishCatch.y);
				__instance.fishTrans.gameObject.SetActive(value: true);
				__instance.fishAnimationCurve = biomeFishingInfo.fishAnimationCurves[UnityEngine.Random.Range(0, biomeFishingInfo.fishAnimationCurves.Length)];
				__instance.currFishCatch = 0f;
				__instance.currTresureCatch = 0f;
				if (UnityEngine.Random.value < 0.15f + ___extraAddTresureProb + ___extraTresureProb) {
					__instance.tresurePos = __instance.fishPos + (float) (180 + UnityEngine.Random.Range(-45, 45));
					__instance.tresurePos %= 360f;
					if (__instance.tresurePos < 0f) {
						__instance.tresurePos += 360f;
					}
					__instance.tresurePos = Mathf.Repeat(__instance.tresurePos, 360f);
					__instance.maxTresureCatch = UnityEngine.Random.Range(biomeFishingInfo.minMaxFishing_MaxTreasureCatch.x, biomeFishingInfo.minMaxFishing_MaxTreasureCatch.y);
					__instance.tresureTrans.gameObject.SetActive(value: true);
				} else {
					__instance.tresureTrans.gameObject.SetActive(value: false);
					___extraAddTresureProb += 0.04f;
				}
				logger.LogInfo($"__instance.itemInfo.fishingRod_acceleration: {__instance.itemInfo.fishingRod_acceleration}\n__instance.itemInfo.fishingRod_forcePull: {__instance.itemInfo.fishingRod_forcePull}\n__instance.itemInfo.fishingRod_maxSpeed: {__instance.itemInfo.fishingRod_maxSpeed}\n__instance.itemInfo.fishingRod_sizeBar: {__instance.itemInfo.fishingRod_sizeBar}\n__instance.itemInfo.fishingRod_catchSpeed_Fish: {__instance.itemInfo.fishingRod_catchSpeed_Fish}\n__instance.itemInfo.fishingRod_catchSpeed_Treasure: {__instance.itemInfo.fishingRod_catchSpeed_Treasure}\n__instance.itemInfo.fishingRod_minMaxItems_Fish: {__instance.itemInfo.fishingRod_minMaxItems_Fish}\n__instance.itemInfo.fishingRod_minMaxItems_Treasure: {__instance.itemInfo.fishingRod_minMaxItems_Treasure}");
				__instance.acceleration = __instance.itemInfo.fishingRod_acceleration;
				__instance.forcePull = __instance.itemInfo.fishingRod_forcePull;
				___maxSpeed = __instance.itemInfo.fishingRod_maxSpeed;
				__instance.currBarImage.fillAmount = __instance.itemInfo.fishingRod_sizeBar;
				___rangeBarSize = 360f * __instance.itemInfo.fishingRod_sizeBar / 2f;
				__instance.currBarImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 0f - ___rangeBarSize + 2.5f);
				___catchSpeed_Fish = __instance.itemInfo.fishingRod_catchSpeed_Fish;
				___catchSpeed_Treasure = __instance.itemInfo.fishingRod_catchSpeed_Treasure;
				__instance.fishingRod_minMaxItems_Fish = __instance.itemInfo.fishingRod_minMaxItems_Fish;
				__instance.fishingRod_minMaxItems_Treasure = __instance.itemInfo.fishingRod_minMaxItems_Treasure;
				__instance.fishingCanvasGroup.transform.position = __instance.fishingRodBait.transform.position + new Vector3(0f, 1.5f, 0f);
				__instance.fishingCanvasGroup.gameObject.SetActive(value: true);
				return false;
			} catch (Exception e) {
				logger.LogError($"** HarmonyPatch_PlayerFishingManager_StartFishingMinigame ERROR - {e}");
			}
			return true;
		}
		*/

		private static void Postfix(
			PlayerFishingManager __instance,
			bool ___treasureCollected,
			float ___speed,
			float ___extraTensionMult,
			ItemInfo ____baitInfo,
			float ___tensionMult,
			float ___timeToStartGame,
			float ___extraAddTresureProb,
			float ___extraTresureProb,
			float ___maxSpeed,
			float ___rangeBarSize,
			float ___catchSpeed_Fish,
			float ___catchSpeed_Treasure
		) {	
			try {
				/*
				 
_instance.itemInfo.fishingRod_forcePull: 60
__instance.itemInfo.fishingRod_maxSpeed: 1.5
__instance.itemInfo.fishingRod_sizeBar: 0.125
__instance.itemInfo.fishingRod_catchSpeed_Fish: 1
__instance.itemInfo.fishingRod_catchSpeed_Treasure: 1
__instance.itemInfo.fishingRod_minMaxItems_Fish: (0.00, 0.00)
__instance.itemInfo.fishingRod_minMaxItems_Treasure: (0.00, 0.00)

				__instance.acceleration = __instance.itemInfo.fishingRod_acceleration;
				__instance.forcePull = __instance.itemInfo.fishingRod_forcePull;
				___maxSpeed = __instance.itemInfo.fishingRod_maxSpeed;
				__instance.currBarImage.fillAmount = __instance.itemInfo.fishingRod_sizeBar;
				___rangeBarSize = 360f * __instance.itemInfo.fishingRod_sizeBar / 2f;
				__instance.currBarImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 0f - ___rangeBarSize + 2.5f);
				___catchSpeed_Fish = __instance.itemInfo.fishingRod_catchSpeed_Fish;
				___catchSpeed_Treasure = __instance.itemInfo.fishingRod_catchSpeed_Treasure;
				__instance.fishingRod_minMaxItems_Fish = __instance.itemInfo.fishingRod_minMaxItems_Fish;
				__instance.fishingRod_minMaxItems_Treasure = __instance.itemInfo.fishingRod_minMaxItems_Treasure;
				__instance.fishingCanvasGroup.transform.position = __instance.fishingRodBait.transform.position + new Vector3(0f, 1.5f, 0f);
				__instance.fishingCanvasGroup.gameObject.SetActive(value: true);
				*/
				//__instance.forcePull /= 4;
				//___maxSpeed /= 4;
				if (!m_enabled.Value) {
					return;
				}
				logger.LogInfo($"___extraTresureProb: {___extraTresureProb}");
				logger.LogInfo($"__instance.tresureTrans.gameObject.activeSelf: {__instance.tresureTrans.gameObject.activeSelf}, __instance.treasurePos: {__instance.tresurePos}");
				__instance.maxFishCatch *= m_catch_speed_multiplier.Value;
			} catch (Exception e) {
				logger.LogError($"** HarmonyPatch_PlayerFishingManager_StartFishingMinigame.Postfix ERROR - {e}");
			}
		}
	}
}