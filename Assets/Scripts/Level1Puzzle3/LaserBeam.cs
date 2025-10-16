using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    public Transform laserOrigin;
    public int maxReflections = 6;
    public float maxDistance = 100f;
    public LayerMask reflectableLayers;
    private LineRenderer lr;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        DrawLaser();
    }

    void DrawLaser()
    {
        Vector3 direction = laserOrigin.forward;
        Vector3 position = laserOrigin.position;

        lr.positionCount = 1;
        lr.SetPosition(0, position);

        for (int i = 0; i < maxReflections; i++)
        {
            if (Physics.Raycast(position, direction, out RaycastHit hit, maxDistance, reflectableLayers))
            {
                lr.positionCount++;
                lr.SetPosition(lr.positionCount - 1, hit.point);

                 if (hit.collider.CompareTag("Mirror"))
                {
                    KPITracker.Instance?.MarkMirrorLit(hit.collider.transform); // +++
                    direction = Vector3.Reflect(direction, hit.normal);
                    position = hit.point;
                    continue;
                }

                else if (hit.collider.CompareTag("Target"))
                {
                    Debug.Log("TARGET HIT!");
                    hit.collider.GetComponent<LaserTarget>()?.Activate();
                    break;
                }
                else
                {
                    break;
                }
            }
            else
            {
                lr.positionCount++;
                lr.SetPosition(lr.positionCount - 1, position + direction * maxDistance);
                break;
            }
        }
    }
}
