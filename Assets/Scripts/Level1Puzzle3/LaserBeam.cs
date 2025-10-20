using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    public Transform laserOrigin;
    public int maxReflections = 6;
    public float maxDistance = 100f;
    public LayerMask reflectableLayers;
    private LineRenderer lr;

    private LaserTarget lastHitTarget;
    private int beamId;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        beamId = gameObject.GetInstanceID();
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

        bool hitTargetThisFrame = false;
        LaserTarget currentTarget = null;

        for (int i = 0; i < maxReflections; i++)
        {
            if (Physics.Raycast(position, direction, out RaycastHit hit, maxDistance))
            {
                lr.positionCount++;
                lr.SetPosition(lr.positionCount - 1, hit.point);

                // Rebote en espejo (SIN SONIDO)
                if (hit.collider.CompareTag("Mirror"))
                {
                    direction = Vector3.Reflect(direction, hit.normal);
                    position = hit.point;
                    continue;
                }
                // Impacta un Target (prisma final)
                else if (hit.collider.CompareTag("Target"))
                {
                    currentTarget = hit.collider.GetComponent<LaserTarget>();
                    if (currentTarget != null)
                    {
                        if (lastHitTarget != currentTarget)
                        {
                            lastHitTarget?.Deactivate(beamId);
                            currentTarget.Activate(beamId);
                            lastHitTarget = currentTarget;
                        }
                        hitTargetThisFrame = true;
                    }

                    break;
                }
                // Impacta un Pilar
                else if (hit.collider.CompareTag("Pillar"))
                {
                    if (lastHitTarget != null)
                    {
                        lastHitTarget.Deactivate(beamId);
                        lastHitTarget = null;
                    }
                    break;
                }
                // Otro obstáculo
                else
                {
                    if (lastHitTarget != null)
                    {
                        lastHitTarget.Deactivate(beamId);
                        lastHitTarget = null;
                    }
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

        if (!hitTargetThisFrame && lastHitTarget != null)
        {
            lastHitTarget.Deactivate(beamId);
            lastHitTarget = null;
        }
    }
}
