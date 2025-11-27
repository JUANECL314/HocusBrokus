using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody))]
public class VictoryCutsceneController : MonoBehaviourPun
{
    [Header("Escena de victoria")]
    [Tooltip("Nombre de la escena de victoria donde se debe desactivar la cámara del player.")]
    [SerializeField] private string victorySceneName = "VictoryScene";

    [Header("Referencias para desactivar control")]
    [Tooltip("Script de movimiento principal del jugador (ej: FreeFlyCameraMulti).")]
    public MonoBehaviour movementScript;

    [Tooltip("Script que maneja la entrada de magia (PlayerMagicInput).")]
    public MonoBehaviour magicInputScript;

    [Tooltip("Otros scripts que quieras apagar durante la cinemática (UI, etc.).")]
    public MonoBehaviour[] extraScriptsToDisable;

    [Header("Animación")]
    public Animator animator;
    public string danceTriggerName = "idle";

    [Header("Modelo")]
    [Tooltip("Nodo raíz visual del personaje (donde están malla y Animator).")]
    public Transform modelRoot;

    [Header("Cámara local")]
    [Tooltip("Cámara del jugador local que debe apagarse en la escena de victoria.")]
    public Camera localCamera;

    [Tooltip("AudioListener asociado a la cámara local.")]
    public AudioListener localAudioListener;

    // Estado interno
    private bool inVictory = false;

    void Start()
    {
        // Si estamos en la escena de victoria, apagamos la cámara del jugador local
        // desde el inicio para que sólo se vea la cámara global de la escena.
        if (photonView.IsMine && SceneManager.GetActiveScene().name == victorySceneName)
        {
            if (localCamera) localCamera.enabled = false;
            if (localAudioListener) localAudioListener.enabled = false;
        }
    }

    /// <summary>
    /// Llamado vía RPC desde VictorySceneManager para poner al jugador
    /// en su pose de victoria.
    /// </summary>
    [PunRPC]
    public void RpcEnterVictoryPose(Vector3 pos, Quaternion rot)
    {
        if (inVictory) return;

        // --- Desactivar control de jugador ---
        if (movementScript) movementScript.enabled = false;
        if (magicInputScript) magicInputScript.enabled = false;

        if (extraScriptsToDisable != null)
        {
            foreach (var s in extraScriptsToDisable)
            {
                if (s) s.enabled = false;
            }
        }

        // 🔹 Apagar cámara y audio del jugador local (por si aún seguían activos)
        if (photonView.IsMine)
        {
            if (localCamera) localCamera.enabled = false;
            if (localAudioListener) localAudioListener.enabled = false;
        }

        // --- Congelar física para que no se caiga ni se deslice ---
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // --- Colocar posición y orientación final ---
        // Normalmente ya spawneamos en el spot correcto, pero esto asegura consistencia.
        float yaw = rot.eulerAngles.y;

        if (modelRoot != null)
        {
            modelRoot.position = pos;
            modelRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
        else
        {
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // --- Disparar animación de baile ---
        if (animator != null && !string.IsNullOrEmpty(danceTriggerName))
        {
            animator.SetTrigger(danceTriggerName);
        }

        inVictory = true;
    }
}
