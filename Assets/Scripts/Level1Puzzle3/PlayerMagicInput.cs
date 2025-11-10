using Photon.Pun;
using UnityEngine;

using UnityEngine.InputSystem;

using System.Collections;


public class PlayerMagicInput : MonoBehaviourPun
{
    public Magic magic;

    private PhotonView pv;


    // Animator for attack animation (optional)
    public Animator animator;

    // Delay (seconds) from animation start to actual launch
    public float attackDelay = 1.25f;

    // Prevent overlapping attack coroutines
    bool isAttacking = false;


    private void Reset()
    {
        if (magic == null) magic = GetComponent<Magic>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Awake()
    {

        pv = GetComponent<PhotonView>();

        if (magic == null) magic = GetComponent<Magic>();
        if (magic == null) magic = GetComponentInChildren<Magic>();
        if (magic == null) Debug.LogWarning("PlayerMagicInput: Magic no asignado.");


        // try to find Animator first on this GameObject, then in its children,
        // then try the Magic object's animator as a last resort (matches FreeFlyCameraMulti approach)
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null && magic != null) animator = magic.GetComponent<Animator>();
        if (animator == null && magic != null) animator = magic.GetComponentInChildren<Animator>(true);

        Debug.Log("PlayerMagicInput: Animator = " + (animator != null ? "found on " + animator.gameObject.name : "null"));

    }

    public void OnCast(InputValue value)
    {
        if (!pv.IsMine) return;
        if (magic == null) return;

        // Left click to launch (plays animation first, then launches after delay)
        if (value.isPressed)
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