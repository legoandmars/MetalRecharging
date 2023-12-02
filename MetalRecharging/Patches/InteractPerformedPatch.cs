using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MetalRecharging.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class InteractPerformedPatch
    {
        private static bool _swappingTwoHandedValue = false;

        // TODO transpile this shit
        [HarmonyPatch("Interact_performed")]
        [HarmonyPrefix]
        internal static void InteractPerformedPrefix(PlayerControllerB __instance)
        {
            if (__instance.hoveringOverTrigger == null) return;
            if (!__instance.twoHanded) return;
            var triggerParent = __instance.hoveringOverTrigger.transform.parent;
            if (triggerParent == null || triggerParent.gameObject.name != "ChargeStationTrigger") return; // what

            // swapping time
            _swappingTwoHandedValue = true;
            __instance.twoHanded = false;
        }

        [HarmonyPatch("Interact_performed")]
        [HarmonyPostfix]
        internal static void InteractPerformedPostfix(PlayerControllerB __instance)
        {
            if (!_swappingTwoHandedValue) return;
            _swappingTwoHandedValue = false;
            __instance.twoHanded = true;
        }
    }
}
