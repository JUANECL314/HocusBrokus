using System.Collections;
using System.Text;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Collider))]
public class FinalPuzzleController : MonoBehaviourPun
{
    [Header("Escena de victoria")]
    public string victorySceneName = "VictoryScene";

    [Header("Recompensa (wallet)")]
    [Tooltip("Monedas que gana cada jugador al completar todos los puzzles")]
    public int rewardCoins = 500;

    [Tooltip("URL completa al endpoint /api/wallet/grant")]
    public string walletGrantUrl = "https://hokusbackend-production.up.railway.app/wallet/grant";

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
        if (alreadyTriggered) return;
        Debug.Log("Detecta 2");
        if (!other.CompareTag("Player")) return;
        Debug.Log("Detecta 1");
        alreadyTriggered = true;

        // Solo el master dispara el RPC global
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RpcOnFinalPuzzleCompleted), RpcTarget.All);
        }
    }

    /// <summary>
    /// Se ejecuta en TODOS los clientes cuando se completa el puzzle final.
    /// </summary>
    [PunRPC]
    private void RpcOnFinalPuzzleCompleted()
    {
        Debug.Log("[FinalPuzzleController] ¡Puzzle final completado! Dando recompensa y yendo a VictoryScene...");

        // Cada cliente manda sus propias monedas
        StartCoroutine(GrantCoinsToLocalPlayer());

        // Solo el master cambia de escena (Photon sincroniza a los demás)
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(victorySceneName);
        }
    }

    /// <summary>
    /// Llama al backend para dar rewardCoins al usuario logueado en este cliente.
    /// </summary>
    private IEnumerator GrantCoinsToLocalPlayer()
    {
        // Aquí asumimos que guardaste el id del usuario al hacer login
        int userId = AuthState.UserId;
        if (userId <= 0)
        {
            // fallback por si entraste por autologin antes de inicializar AuthState
            userId = PlayerPrefs.GetInt("auth.userId", 0);
        }

        if (userId <= 0)
        {
            Debug.LogWarning("[FinalPuzzleController] No hay user_id en AuthState/PlayerPrefs, no se mandan monedas.");
            yield break;
        }


        var payload = new WalletGrantPayload
        {
            user_id = userId,
            amount = rewardCoins,
            reason = "puzzle_clear",
            source = "puzzle_victory",
            match_id = "" // opcional, puedes rellenar tu matchId si lo tienes
        };

        string json = JsonUtility.ToJson(payload);
        Debug.Log("[FinalPuzzleController] Enviando grant al backend: " + json);

        using (var req = new UnityWebRequest(walletGrantUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[FinalPuzzleController] Error al hacer grant de monedas: {req.error} | {req.downloadHandler.text}");
            }
            else
            {
                Debug.Log("[FinalPuzzleController] Grant OK: " + req.downloadHandler.text);
            }
        }
    }
}

[System.Serializable]
public class WalletGrantPayload
{
    public int user_id;
    public int amount;
    public string reason;
    public string source;
    public string match_id;
}
