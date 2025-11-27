using System.Collections;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class VictorySceneManager : MonoBehaviourPunCallbacks
{
    [Header("Spawn de jugadores")]
    [Tooltip("Puntos de spawn para cada jugador (en orden por ActorNumber)")]
    public Transform[] spawnPoints;
    [Tooltip("Prefab del jugador que se usará en la escena de victoria")]
    public GameObject playerPrefab;

    private bool playerSpawned = false;

    [Header("Flujo")]
    [Tooltip("Segundos que dura la escena de victoria antes de ir a endorsements")]
    public float victoryDuration = 8f;

    [Tooltip("Nombre de la escena de endorsements")]
    public string endorsementsSceneName = "Endorsments";

    void Start()
    {
        // Si ya estamos en room, spawneamos al jugador local
        if (PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }

        // Solo el master controla la transición de la escena
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(CoSetupAndGo());
        }
    }

    // Por si este script se usa en una escena a la que se entra después de hacer JoinRoom
    public override void OnJoinedRoom()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (playerSpawned)
        {
            Debug.LogWarning("[VictorySceneManager] Jugador ya instanciado. Ignorando duplicado.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[VictorySceneManager] No hay spawnPoints asignados en la escena.");
            return;
        }

        int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        if (index < 0) index += spawnPoints.Length;

        Transform spawn = spawnPoints[index];

        Debug.Log($"[VictorySceneManager] Spawneando jugador {PhotonNetwork.LocalPlayer.NickName} en {spawn.name}");
        GameObject playerInstance = PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawn.position,
            spawn.rotation
        );

        // Guardar referencia en el TagObject del jugador (útil si lo usas en otras partes)
        PhotonNetwork.LocalPlayer.TagObject = playerInstance.transform;

        // 🔒 Bloquear controles inmediatamente al spawnear
        LockPlayerControls(playerInstance);

        playerSpawned = true;
    }

    /// <summary>
    /// Desactiva los componentes de control del jugador para que no pueda moverse ni castear.
    /// Ajusta esta lista a tus componentes reales.
    /// </summary>
    private void LockPlayerControls(GameObject player)
    {
        // Ejemplos basados en tu script de AparicionLobbyIndividual :contentReference[oaicite:2]{index=2}
        var magicInput = player.GetComponent<PlayerMagicInput>();
        if (magicInput != null) magicInput.enabled = false;

        var magic = player.GetComponent<Magic>();
        if (magic != null) magic.enabled = false;

        var elements = player.GetComponent<Elements>();
        if (elements != null) elements.enabled = false;

        // Si tienes algún script de movimiento propio, desactívalo aquí.
        // Por ejemplo:
        var freeFly = player.GetComponent<FreeFlyCameraMulti>();
        if (freeFly != null) freeFly.enabled = false;

        // Opcional: si tu movimiento está en otro script (PlayerMovement, CharacterController custom, etc.)
        // var movement = player.GetComponent<PlayerMovement>();
        // if (movement != null) movement.enabled = false;
    }

    IEnumerator CoSetupAndGo()
    {
        // Esperar un poquito a que todos los players se hayan spawneado
        yield return new WaitForSeconds(1.0f);

        PlacePlayersOnSpots();

        yield return new WaitForSeconds(victoryDuration);

        if (PhotonNetwork.IsMasterClient && !string.IsNullOrEmpty(endorsementsSceneName))
        {
            PhotonNetwork.LoadLevel(endorsementsSceneName);
        }
    }

    void PlacePlayersOnSpots()
    {
        var all = FindObjectsOfType<VictoryCutsceneController>();
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("[VictorySceneManager] No se encontraron players con VictoryCutsceneController.");
            return;
        }

        var ordered = all
            .Select(c => new { ctrl = c, view = c.GetComponent<PhotonView>() })
            .Where(x => x.view != null && x.view.Owner != null)
            .OrderBy(x => x.view.Owner.ActorNumber)
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            var item = ordered[i];
            var view = item.view;
            var ctrl = item.ctrl;

            // Usa la posición/rotación actual del player
            Vector3 pos = ctrl.transform.position;
            Quaternion rot = ctrl.transform.rotation;

            view.RPC(
                "RpcEnterVictoryPose",
                RpcTarget.All,
                pos,
                rot
            );
        }
    }
}
