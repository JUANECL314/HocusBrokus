using UnityEngine;
using Photon.Pun;
public class SceneSpawner : MonoBehaviourPunCallbacks
{
    public GameObject[] prefabs;
    public string path;
    public bool reapacerAlInicio = true;
    public bool soloMaestroAparece = true;

    void Awake()
    {
        if(reapacerAlInicio)
        {
            AparecerLosObjectosEnEscena();
        }
    }

    public void AparecerLosObjectosEnEscena()
    {
        if (soloMaestroAparece && !PhotonNetwork.IsMasterClient)
            return;

        foreach (GameObject prefab in prefabs) {
            if (prefab == null)
                continue;

            Vector3 posicion = prefab.transform.position;
            Quaternion rotacion = prefab.transform.rotation;

            GameObject instance = PhotonNetwork.InstantiateRoomObject(
                path+prefab.name,
                posicion, rotacion);
        }
    }
}
