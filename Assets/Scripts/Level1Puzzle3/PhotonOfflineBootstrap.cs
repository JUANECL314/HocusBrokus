using Photon.Pun;
using UnityEngine;

public static class PhotonOfflineBootstrap
{
    /*
    // 1) Antes de cargar la primera escena: activar OfflineMode
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnableOffline()
    {
        PhotonNetwork.OfflineMode = true;                 // RPC/Instantiate funcionan local
        PhotonNetwork.AutomaticallySyncScene = false;     // opcional
        Debug.Log("[PUN] OfflineMode habilitado.");
    }

    // 2) Después de cargar la escena: si no estás en sala, créala (offline entra al instante)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureOfflineRoom()
    {
        if (!PhotonNetwork.InRoom)
        {
            // Nombre arbitrario; en OfflineMode se crea y entra de inmediato
            PhotonNetwork.CreateRoom("__offline_room__");
            Debug.Log("[PUN] Sala offline creada/ingresada (__offline_room__).");
        }
    }*/
}
