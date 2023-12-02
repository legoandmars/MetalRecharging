using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MetalRecharging.Patches
{
    // This class is written in a way that should make it so people without the mod don't get killed
    // ChatPatch purporsefully sets mineAudio to null to make vanilla clients throw and not show an explosion
    [HarmonyPatch(typeof(Landmine))]
    internal class LandminePatch
    {
        [HarmonyPatch(nameof(Landmine.Detonate))]
        [HarmonyPrefix]
        static bool Detonate(Landmine __instance)
        {
            Debug.Log("DETONATION!");
            if (__instance.mineAudio != null) return true;
            var audioSource = __instance.GetComponentInChildren<AudioSource>();

            audioSource.pitch = UnityEngine.Random.Range(0.93f, 1.07f);
            audioSource.PlayOneShot(__instance.mineDetonate, 1f);
            Landmine.SpawnExplosion(__instance.transform.position + Vector3.up, false, 5.7f, 6.4f);
            return false;
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool LandmineStart(Landmine __instance)
        {
            return __instance.mineAudio != null;
        }

        [HarmonyPatch("SetOffMineAnimation")]
        [HarmonyPrefix]
        static bool SetOffMineAnimation(Landmine __instance)
        {
            Debug.Log("MINE!");
            if (__instance.mineAudio == null)
            {
                __instance.mineAnimator.SetTrigger("startIdle");
                __instance.mineAudio = __instance.GetComponentInChildren<AudioSource>();
                __instance.mineTrigger = null;
            }
            return true;
        }
    }
}
