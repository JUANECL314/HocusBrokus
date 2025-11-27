using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinalPuzzleController : MonoBehaviour
{
    [Header("Escena de victoria")]
    public string victorySceneName = "VictoryScene";

    private bool alreadyTriggered = false;

    private void Awake()
    {
        // Asegurarnos que el collider sea trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    // 👉 Se llama automáticamente cuando alguien entra al portal
    private void OnTriggerEnter(Collider other)
    {
        // Solo reaccionar una vez
        if (alreadyTriggered) return;

        // Filtramos solo jugadores (asegúrate que tu Player tenga tag "Player")
        if (!other.CompareTag("Player")) return;

        alreadyTriggered = true;
        OnFinalPuzzleCompleted();
    }

    // 👉 Lógica de victoria (por si quieres llamarla también desde otro lado)
    public void OnFinalPuzzleCompleted()
    {
        Debug.Log("[FinalPuzzleController] ¡Puzzle final completado! Cargando VictoryScene...");

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(victorySceneName);
        }
    }
}
