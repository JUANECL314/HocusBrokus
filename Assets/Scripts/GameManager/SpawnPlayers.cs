using Photon.Pun;
using UnityEngine;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject jugadorPrefab;
    

    private void Start()
    {
        float posicionX = gameObject.transform.position.x;
        Vector3 randomPosition = new Vector3(Random.Range(posicionX-5, posicionX+5), gameObject.transform.position.y, gameObject.transform.position.z);//Random.Range(minY, maxY));

        jugadorPrefab.GetComponent<FreeFlyCamera>().enableFlying = false;
        PhotonNetwork.Instantiate(jugadorPrefab.name, randomPosition, Quaternion.identity);

    }
}
