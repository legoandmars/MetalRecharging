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
    [HarmonyPatch(typeof(HUDManager))]
    internal class ChatPatch
    {
        private static bool _justExploded = false;
        private static SpawnableMapObject? _landmine = null;
        [HarmonyPatch("AddPlayerChatMessageServerRpc")]
        [HarmonyPatch("AddChatMessage")]
        [HarmonyPostfix]
        private static void OnServerMessageAdded(HUDManager __instance, ref string chatMessage)
        {
            if (_justExploded) return;
            if (__instance.NetworkManager == null) return;
            if (!chatMessage.Contains("iexplode")) return;

            var splitMessage = chatMessage.Split('-');
            if (splitMessage.Length != 3) return;

            var playerId = ulong.Parse(splitMessage[1]);
            var player = __instance.playersManager.allPlayerScripts.FirstOrDefault(x => x.playerClientId == playerId);
            if (_landmine == null) _landmine = __instance.playersManager?.levels?.SelectMany(x => x.spawnableMapObjects).FirstOrDefault(x => x.prefabToSpawn.name == "Landmine");
            if (player == null || _landmine == null) return;

            if (!__instance.NetworkManager.IsHost && !__instance.NetworkManager.IsServer)
            {
                // we are NOT the server but go ahead and set the non-kill patch for the next explosion.
                LandminePatch.LastExplosionWasCharger = true;
                return;
            }

            LandminePatch.LastExplosionWasCharger = true;

            // we're the server and everything is valid. fucking EXPLODE!!!!!!!!!!!!
            Debug.Log("Explisuion time.");
            _justExploded = true;

            var playerPos = player.transform.position - new Vector3(0, 0.25f, 0);
            GameObject landmineObject = UnityEngine.Object.Instantiate(_landmine.prefabToSpawn, playerPos, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            var landmine = landmineObject.GetComponentInChildren<Landmine>();
            landmineObject.GetComponent<NetworkObject>().Spawn(true);
            landmine.ExplodeMineServerRpc();
            _ = Unexplode();
        }

        private static async Task Unexplode()
        {
            await Task.Delay(200);
            _justExploded = false;
        }

        public static void SendExplosionChat()
        {
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null) return;
            var playerId = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
            HUDManager.Instance.AddTextToChatOnServer("<size=0>-"+playerId+"-iexplode</size>", playerId);
        }
    }
}
