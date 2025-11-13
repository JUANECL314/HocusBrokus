using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMagicInput : MonoBehaviourPun
{
    [Header("Refs")]
    public Magic magic;
    public Animator animator;

    [Header("Attack Timing")]
    [Tooltip("Retraso entre disparar la animación y ejecutar el launch real.")]
    public float attackDelay = 1.25f;

    // Nombre del parámetro en el Animator (configura en PhotonAnimatorView como Trigger → Discrete)
    [SerializeField] private string attackTriggerParam = "Attack";

    // Estado para no solapar ataques
    bool isAttacking = false;

    PhotonView pv;

    void Reset()
    {
        if (magic == null) magic = GetComponent<Magic>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (magic == null) magic = GetComponent<Magic>();
        if (magic == null) magic = GetComponentInChildren<Magic>();

        // Buscar Animator en este GO, hijos, o en el objeto Magic
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null && magic != null) animator = magic.GetComponent<Animator>();
        if (animator == null && magic != null) animator = magic.GetComponentInChildren<Animator>(true);

        if (animator == null)
            Debug.LogWarning($"PlayerMagicInput: Animator no encontrado en {name} ni en Magic.");
    }

    // Input System (PlayerInput → Send Messages) binding a la acción "Cast"
    
    public void OnCast(InputValue value)
    {
        if (!pv.IsMine) return;           // Solo el dueño procesa entrada
        if (magic == null) return;

        if (value.isPressed && !isAttacking)
        {
            // Dispara la anim (solo local); PhotonAnimatorView replicará el Trigger a todos.
            if (animator != null && !string.IsNullOrEmpty(attackTriggerParam))
                animator.SetTrigger(attackTriggerParam);

            // Si hay anim, esperamos; si no, lanzamos inmediato
            if (animator != null && attackDelay > 0f)
                StartCoroutine(PerformAttackAfterDelay());
            else
                magic.launchElement();
        }
    }

    IEnumerator PerformAttackAfterDelay()
    {
        isAttacking = true;
        yield return new WaitForSeconds(attackDelay);

        // Seguridad por si se destruye o desactiva
        if (magic != null)
            magic.launchElement();

        isAttacking = false;
    }


}
