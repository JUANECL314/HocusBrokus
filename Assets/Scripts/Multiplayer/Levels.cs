using Photon.Pun;
using UnityEngine;

public class Levels : MonoBehaviourPun
{
    public GameObject canvas;

    
    private void Start()
    {
        canvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && PhotonNetwork.IsMasterClient)
        {
            canvas.SetActive(true);
        }
    }

   
}
