using UnityEngine;

public class EyeFollowTarget : MonoBehaviour
{
    public Transform target;
    public float followAmount = 0.05f; // small offset range

    private Vector3 baseLocalPos;

    void Start()
    {
        baseLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (target == null || Camera.main == null) return;

        Vector3 camDir = (target.position - transform.position).normalized;
        transform.localPosition = baseLocalPos + camDir * followAmount;
    }
}
