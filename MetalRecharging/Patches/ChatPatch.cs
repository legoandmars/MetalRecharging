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
        [HarmonyPostfix]
        private static void OnServerMessageAdded(HUDManager __instance, ref string chatMessage)
        {
            Debug.Log("IDK");
            Debug.Log(chatMessage.Contains("iexplode"));
            Debug.Log(chatMessage.Split('-').Length);
            if (__instance.NetworkManager == null) return;
            if (!__instance.NetworkManager.IsHost && !__instance.NetworkManager.IsServer) return; //might need to be both host and server idk
            if (!chatMessage.Contains("iexplode")) return;
            if (_justExploded) return;

            var splitMessage = chatMessage.Split('-');
            if (splitMessage.Length != 3) return;

            var playerId = ulong.Parse(splitMessage[1]);
            var player = __instance.playersManager.allPlayerScripts.FirstOrDefault(x => x.playerClientId == playerId);
            if (_landmine == null) _landmine = __instance.playersManager?.levels?.SelectMany(x => x.spawnableMapObjects).FirstOrDefault(x => x.prefabToSpawn.name == "Landmine");
            if (player == null || _landmine == null) return;

            // we're the server and everything is valid. fucking EXPLODE!!!!!!!!!!!!
            Debug.Log("Explisuion time.");
            _justExploded = true;

            var playerPos = player.transform.position + new Vector3(0, 1f, 0);
            GameObject landmineObject = UnityEngine.Object.Instantiate(_landmine.prefabToSpawn, playerPos, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);

            // disable the landmine changing player position
            foreach (var collider in landmineObject.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            var landmine = landmineObject.GetComponentInChildren<Landmine>();
            UnityEngine.Object.Destroy(landmine.GetComponent<MeshRenderer>()); // destroy renderer, can't just be disabled because it's used in animations
            landmine.mineAudio = null; // force audio thihng to null to break kill behaviour for unmodded clients
            landmineObject.GetComponent<NetworkObject>().Spawn(true);
            //landmine.TriggerMineOnLocalClientByExiting();
            Debug.Log("explode2");
            Debug.Log(landmine);
            landmine.ExplodeMineServerRpc();
            //AccessTools.Method(typeof(Landmine), "TriggerMineOnLocalClientByExiting").Invoke(landmine, new object[] { });
            _ = Unexplode();
        }

        private static async Task Unexplode()
        {
            await Task.Delay(200);

            Debug.Log("UNexplode");
            _justExploded = false;
        }

        public static void SendExplosionChat()
        {
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null) return;
            var playerId = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
            HUDManager.Instance.AddTextToChatOnServer("-"+playerId+"-iexplode", playerId);
        }
    }
}
