using Photon.Pun;
using UnityEngine;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject jugadorPrefab;
    [SerializeField]
    float minX;
    [SerializeField]
    float minY;
    [SerializeField]
    float maxX;
    [SerializeField]
    float maxY;

    private void Start()
    {
        Vector3 randomPosition = new Vector3(Random.Range(minX, maxX), 43, 43);//Random.Range(minY, maxY));
        PhotonNetwork.Instantiate(jugadorPrefab.name, randomPosition, Quaternion.identity);
    }
}
