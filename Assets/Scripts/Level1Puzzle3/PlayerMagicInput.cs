using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMagicInput : MonoBehaviourPun
{
    public Magic magic;

    private PhotonView pv;

    // Animator (optional)
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

        // Animator search
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null && magic != null) animator = magic.GetComponent<Animator>();
        if (animator == null && magic != null) animator = magic.GetComponentInChildren<Animator>(true);

        Debug.Log("PlayerMagicInput: Animator = " + (animator != null ? "found on " + animator.gameObject.name : "null"));
    }


    // ----------------------------------------------
    // INPUT
    // ----------------------------------------------
    public void OnCast(InputValue value)
    {
        if (!pv.IsMine) return;
        if (magic == null) return;

        if (value.isPressed)
        {
            if (!isAttacking)
            {
                if (animator != null) 
                    animator.SetTrigger("Attack");

                // Use delay if animator exists
                if (animator != null)
                    StartCoroutine(PerformAttackAfterDelay());
                else
                    PerformImmediateAttack();
            }
        }

        // L key to show element info
        if (Input.GetKeyDown(KeyCode.L)) 
            magic.elementDescription();
    }


    private IEnumerator PerformAttackAfterDelay()
    {
        isAttacking = true;
        
        SoundManager.Instance.Play(SfxKey.MagicCast, transform);


        yield return new WaitForSeconds(attackDelay);

        PerformImmediateAttack();

        isAttacking = false;
    }


    private void PerformImmediateAttack()
    {
        // Play spell sound BEFORE shooting projectile
        PlaySpellSound();

        // Shoot the actual element
        magic.launchElement();
    }


    // ----------------------------------------------
    // SPELL SOUND LOGIC
    // ----------------------------------------------
    private void PlaySpellSound()
    {
        if (magic == null || magic.elementSelected == null)
            return;

        switch (magic.elementSelected.elementType)
        {
            case Elements.ElementType.Fire:
                SoundManager.Instance.Play(SfxKey.FireSpell, transform);
                break;

            case Elements.ElementType.Water:
                SoundManager.Instance.Play(SfxKey.WaterSpell, transform);
                break;

            case Elements.ElementType.Wind:
                SoundManager.Instance.Play(SfxKey.WindSpell, transform);
                break;

            case Elements.ElementType.Earth:
                SoundManager.Instance.Play(SfxKey.EarthSpell, transform);
                break;

            default:
                Debug.LogWarning("No sound assigned for element: " + magic.elementSelected.elementType);
                break;
        }
    }
}
