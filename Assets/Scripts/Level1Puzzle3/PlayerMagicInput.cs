using Photon.Pun;
using UnityEngine;
using System.Collections;

public class PlayerMagicInput : MonoBehaviour
{
    // Single Magic reference (assign in Inspector)
    public Magic magic;
    PhotonView vista;

    // Animator for attack animation (optional)
    public Animator animator;

    // Delay (seconds) from animation start to actual launch
    public float attackDelay = 1.25f;

    // Prevent overlapping attack coroutines
    bool isAttacking = false;

    private void Reset()
    {
        // editor convenience: autofill from same GameObject
        if (magic == null) magic = GetComponent<Magic>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        vista = GetComponent<PhotonView>();

        // runtime autofill: same object -> children -> any Magic in scene
        if (magic == null) magic = GetComponent<Magic>();
        if (magic == null) magic = GetComponentInChildren<Magic>();
        if (magic == null) magic = FindObjectOfType<Magic>();
        if (magic == null)
            Debug.LogWarning("PlayerMagicInput: no Magic assigned. Assign it in the Inspector or add a Magic component to this GameObject or its children.");

        // try to find Animator first on this GameObject, then in its children,
        // then try the Magic object's animator as a last resort (matches FreeFlyCameraMulti approach)
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null && magic != null) animator = magic.GetComponent<Animator>();
        if (animator == null && magic != null) animator = magic.GetComponentInChildren<Animator>(true);

        Debug.Log("PlayerMagicInput: Animator = " + (animator != null ? "found on " + animator.gameObject.name : "null"));
    }

    void Update()
    {
        if (magic == null) return;
        if (!vista.IsMine) return;

        // Left click to launch (plays animation first, then launches after delay)
        if (Input.GetMouseButtonDown(0))
        {
            if (!isAttacking)
            {
                if (animator != null) animator.SetTrigger("Attack");
                // if we have an animator we delay the actual launch, otherwise launch immediately
                if (animator != null)
                    StartCoroutine(PerformAttackAfterDelay());
                else
                    magic.launchElement();
            }
        }

        // Press L to print element description
        if (Input.GetKeyDown(KeyCode.L)) magic.elementDescription();
    }

    IEnumerator PerformAttackAfterDelay()
    {
        isAttacking = true;
        yield return new WaitForSeconds(attackDelay);
        // Double-check references/state before launching
        if (magic != null) magic.launchElement();
        isAttacking = false;
    }
}