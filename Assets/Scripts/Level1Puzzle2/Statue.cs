using UnityEngine;
using System.Collections;

public class Statue : MonoBehaviour
{
    // distance to move the parent along world +Z when hit by Earth magic
    public float moveDistance = 1f;

    // if >0, use smooth movement; higher = faster
    public float moveSpeed = 0f;

    // set true in the Inspector to move in world -Z instead of +Z
    public bool invertZ = false;

    void Start()
    {
        Debug.Log($"Statue started (child collider) on GameObject '{gameObject.name}'. Parent = {(transform.parent?transform.parent.name:"<none>")}");
    }

    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Statue OnTriggerEnter: hit '{other.name}' tag='{other.tag}'");
        if (other.CompareTag("Earth"))
        {
            Debug.Log("Statue: Earth hit detected (trigger). Performing move.");
            if (moveSpeed > 0f) StartCoroutine(MoveParentSmooth());
            else MoveParentInstant();
            Debug.Log("Statue: move performed (trigger).");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Statue OnCollisionEnter: hit '{collision.collider.name}' tag='{collision.collider.tag}'");
        if (collision.collider.CompareTag("Earth"))
        {
            Debug.Log("Statue: Earth hit detected (collision). Performing move.");
            if (moveSpeed > 0f) StartCoroutine(MoveParentSmooth());
            else MoveParentInstant();
            Debug.Log("Statue: move performed (collision).");
        }
    }

    void MoveParentInstant()
    {
        if (transform.parent == null)
        {
            Debug.Log("Statue: MoveParentInstant aborted - parent is null.");
            return;
        }
        Vector3 dir = invertZ ? Vector3.back : Vector3.forward;
        Debug.Log($"Statue: MoveParentInstant moving parent '{transform.parent.name}' by {dir} * {moveDistance}.");
        transform.parent.position += dir * moveDistance;
        Debug.Log($"Statue: parent new position = {transform.parent.position}.");
    }

    IEnumerator MoveParentSmooth()
    {
        if (transform.parent == null)
        {
            Debug.Log("Statue: MoveParentSmooth aborted - parent is null.");
            yield break;
        }
        Transform parent = transform.parent;
        Vector3 start = parent.position;
        Vector3 dir = invertZ ? Vector3.back : Vector3.forward;
        Vector3 target = start + dir * moveDistance;
        float t = 0f;
        Debug.Log($"Statue: MoveParentSmooth starting from {start} to {target} with speed {moveSpeed}.");
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            parent.position = Vector3.Lerp(start, target, Mathf.Clamp01(t));
            yield return null;
        }
        parent.position = target;
        Debug.Log($"Statue: MoveParentSmooth finished. parent position = {parent.position}.");
    }
}