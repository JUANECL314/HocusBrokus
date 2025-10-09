using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GearElemental : MonoBehaviour
{
    public enum MagicType { None, Air, Fire, Water, Earth }

    [Header("Gear Settings")]
    public float rotationSpeed = 100f;
    public Color normalColor = Color.gray;
    public Color hotColor = Color.red;
    public Color coolColor = Color.cyan;
    public Transform wallPosition;

    private bool hasAir = false;
    private bool hasFire = false;
    private bool isRotating = false;
    private bool isHot = false;
    private bool isFalling = false;
    private bool isAttached = false;
    private bool hasDestroyedDoor = false;
    private bool isLocked = false;

    private Renderer rend;
    private Rigidbody rb;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        rend.material.color = normalColor;
    }

    void Update()
    {
        if (isRotating && isAttached)
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

        if (isHot && !isFalling)
            StartCoroutine(StartFalling());
    }

    IEnumerator StartFalling()
    {
        isFalling = true;
        yield return new WaitForSeconds(3f);
        if (isHot)
        {
            rb.isKinematic = false;
            isAttached = false;
        }
    }

    public void ReceiveMagic(MagicType type)
    {
        switch (type)
        {
            case MagicType.Air:
                hasAir = true;
                TryActivateRotation();
                break;

            case MagicType.Fire:
                hasFire = true;
                HeatUp();
                TryActivateRotation();
                break;

            case MagicType.Water:
                if (isHot)
                    CoolDown();
                break;

            case MagicType.Earth:
                if (!isAttached)
                    RestoreToWall();
                break;
        }
    }

    void TryActivateRotation()
    {
        if (hasAir && hasFire && isAttached && !isHot && !isRotating)
        {
            isRotating = true;
            StartCoroutine(RotationTimer());
        }
    }

    IEnumerator RotationTimer()
    {
        yield return new WaitForSeconds(3f);
        if (isRotating && !hasDestroyedDoor)
        {
            DestroyDoors();
            hasDestroyedDoor = true;
        }
    }

    void DestroyDoors()
    {
        GameObject[] doors = GameObject.FindGameObjectsWithTag("puertasEGD");
        foreach (GameObject door in doors)
        {
            Destroy(door);
        }
    }

    void HeatUp()
    {
        isHot = true;
        rend.material.color = hotColor;
    }

    void CoolDown()
    {
        isHot = false;
        hasFire = false;
        rend.material.color = coolColor;
        StopAllCoroutines();
        isFalling = false;
        isRotating = hasAir;
    }

    void RestoreToWall()
    {
        if (wallPosition == null)
            return;

        rb.isKinematic = true;
        transform.position = wallPosition.position;
        transform.rotation = wallPosition.rotation;
        rend.material.color = normalColor;
        isAttached = true;
        isFalling = false;
        isHot = false;
        hasAir = false;
        hasFire = false;
        isRotating = false;
        hasDestroyedDoor = false;
        isLocked = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isLocked) return;

        if (collision.gameObject.CompareTag("Pared") && !isAttached)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 hitPoint = contact.point;
            Vector3 normal = contact.normal;

            AttachToWall(hitPoint, normal);
        }
    }

    public void AttachToWall(Vector3 hitPoint, Vector3 normal)
    {
        if (isLocked) return;

        rb.isKinematic = true;
        isAttached = true;
        transform.position = hitPoint;
        transform.rotation = Quaternion.LookRotation(-normal);
    }

    public void Detach()
    {
        if (isLocked) return;

        isAttached = false;
        rb.isKinematic = false;
    }
}
