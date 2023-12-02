using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace MetalRecharging.Patches
{
    [HarmonyPatch(typeof(ItemCharger))]
    internal class ItemChargerPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool ItemChargerUpdate(ItemCharger __instance, ref float ___updateInterval, ref InteractTrigger ___triggerScript)
        {
            if (NetworkManager.Singleton == null) return false;
            if (___updateInterval > 1f)
            {
                ___updateInterval = 0;
                if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
                {
                    if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer == null)
                    {
                        ___triggerScript.interactable = false;
                        ___triggerScript.disabledHoverTip = "(Requires battery-powered or metal item)";
                    }
                    else
                    {
                        ___triggerScript.interactable = true;
                        // ___triggerScript.hoverTip = "Charge item : [LMB]";
                    }
                    // TODO: Check if ship not landed? can use ship pull thing for that
                    var landmines = UnityEngine.Object.FindObjectsOfType<Landmine>();
                    // Debug.Log(landmines.Length);
                    var landmine = StartOfRound.Instance?.levels?.SelectMany(x => x.spawnableMapObjects).FirstOrDefault(x => x.prefabToSpawn.name == "Landmine")?.prefabToSpawn;
                    // Debug.Log("RM");
                    // Debug.Log(landmine);
                    //var landminePrefab = UnityEngine.Object.FindObjectsOfTypeAll(Landmine);
                    ___triggerScript.twoHandedItemAllowed = true; // uuuuuuh this might break things idk. maybe do this in START or something
                    return false;
                }
            }
            ___updateInterval += Time.deltaTime;
            return false;
        }

        [HarmonyPatch("ChargeItem")]
        [HarmonyPostfix]
        static void ItemChargerCharge(ItemCharger __instance)
        {
            // _ = Kaboom((int)((__instance.triggerScript.animationWaitTime + 0.2f) * 1000
            GrabbableObject currentlyHeldObjectServer = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer;
            if (currentlyHeldObjectServer == null || currentlyHeldObjectServer.itemProperties.requiresBattery) return;

            _ = Kaboom(__instance, 500);
        }

        private async static Task Kaboom(ItemCharger __instance, int delay)
        {
            var player = GameNetworkManager.Instance.localPlayerController;
            await Task.Delay(delay);
            ChatPatch.SendExplosionChat();

            __instance.triggerScript.CancelAnimationExternally();
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            var jetpack = terminal.buyableItemsList.FirstOrDefault(x => x.itemName == "Jetpack");

            return;
            var playerPos = player.transform.position + new Vector3(0, 1f, 0);
            Debug.Log(playerPos);
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(jetpack.spawnPrefab, playerPos, Quaternion.identity, null);
            gameObject.transform.localScale = Vector3.zero;
            gameObject.GetComponent<GrabbableObject>().fallTime = 0f;
            gameObject.GetComponent<GrabbableObject>().isBeingUsed = true;
            gameObject.GetComponent<NetworkObject>().Spawn(true);

            var jetpackItem = gameObject.GetComponent<JetpackItem>();
            jetpackItem.ExplodeJetpackServerRpc();
            var method = AccessTools.Method(typeof(GrabbableObject), "DiscardItemServerRpc");
            Debug.Log(method);
            AccessTools.Method(typeof(GrabbableObject), "DiscardItemServerRpc")?.Invoke(jetpackItem, new object[] { });
            gameObject.GetComponent<NetworkObject>().Despawn(true);
            Debug.Log(jetpack == null);
            Debug.Log("KABOOM!!!");
        }
    }
}
