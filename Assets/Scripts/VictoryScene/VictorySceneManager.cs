using System.Collections;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class VictorySceneManager : MonoBehaviourPunCallbacks
{
    [Header("Spots para cada jugador")]
    public Transform spotP1;
    public Transform spotP2;
    public Transform spotP3;
    public Transform spotP4;

    [Header("Flujo")]
    [Tooltip("Segundos que dura la escena de victoria antes de ir a endorsements")]
    public float victoryDuration = 8f;

    [Tooltip("Nombre de la escena de endorsements")]
    public string endorsementsSceneName = "Endorsments";

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(CoSetupAndGo());
        }
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

            // 👉 Usa la posición/rotación ACTUAL del player (ya está en su spot)
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
