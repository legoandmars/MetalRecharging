using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace MetalRecharging.Patches
{
    // This class is written in a way that should make it so people without the mod don't get killed
    // ChatPatch purporsefully sets mineAudio to null to make vanilla clients throw and not show an explosion
    [HarmonyPatch(typeof(Landmine))]
    internal class LandminePatch
    {
        public static bool LastExplosionWasLocalPlayer = false;
        public static bool LastExplosionWasCharger = false;

        [HarmonyPatch(nameof(Landmine.SpawnExplosion))]
        [HarmonyPrefix]
        static bool SpawnExplosion(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f)
        {
            Debug.Log("Spawning explosion");
            Debug.Log("Charger:");
            Debug.Log(LastExplosionWasCharger);
            Debug.Log(LastExplosionWasLocalPlayer);

            if (!LastExplosionWasCharger) return true;
            var player = GameNetworkManager.Instance.localPlayerController;
            Vector3 bodyVelocity = (player.gameplayCamera.transform.position - explosionPosition) * 80f / Vector3.Distance(player.gameplayCamera.transform.position, explosionPosition);
            if (LastExplosionWasLocalPlayer)
            {
                player.KillPlayer(bodyVelocity, true, CauseOfDeath.Blast, 0);
            }

            LastExplosionWasCharger = false;
            LastExplosionWasLocalPlayer = false;
            int layerMask = ~LayerMask.GetMask(new string[]
            {
                "Room"
            });
            layerMask = ~LayerMask.GetMask(new string[]
            {
                "Colliders"
            });
            var colliders = Physics.OverlapSphere(explosionPosition, 10f, layerMask);
            for (int j = 0; j < colliders.Length; j++)
            {
                Rigidbody component2 = colliders[j].GetComponent<Rigidbody>();
                if (component2 != null)
                {
                    component2.AddExplosionForce(70f, explosionPosition, 10f);
                }
            }

            // TODO: Hit enemies to avoid desync?
            return false;
        }
    }
}
