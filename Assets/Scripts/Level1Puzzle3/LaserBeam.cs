using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviourPun
{
    public Transform laserOrigin;
    public int maxReflections = 6;
    public float maxDistance = 100f;
    public LayerMask reflectableLayers;
    private LineRenderer lr;

    private LaserTarget lastHitTarget;
    private int beamId;

    private bool isActive = false; // Track whether the laser is currently active

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        beamId = gameObject.GetInstanceID();
        lr.enabled = isActive; // start inactive by default
    }

    void Update()
    {
        if (isActive)
            DrawLaser();
        else
            lr.enabled = false;
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
            if (Physics.Raycast(position, direction, out RaycastHit hit, maxDistance, reflectableLayers))
            {
                lr.positionCount++;
                lr.SetPosition(lr.positionCount - 1, hit.point);

                // Rebote en espejo (SIN SONIDO)
                if (hit.collider.CompareTag("Mirror"))
                {
                    KPITracker.Instance?.MarkMirrorLit(hit.collider.transform); // +++
                    direction = Vector3.Reflect(direction, hit.normal);
                    position = hit.point;
                    continue;
                }
                else if (hit.collider.CompareTag("Target"))
                {
                    currentTarget = hit.collider.GetComponent<LaserTarget>();
                    if (currentTarget != null && lastHitTarget != currentTarget)
                    {
                        lastHitTarget?.Deactivate(beamId);
                        currentTarget.Activate(beamId);
                        lastHitTarget = currentTarget;
                    }
                    hitTargetThisFrame = true;
                    break;
                }
                else if (hit.collider.CompareTag("Pillar"))
                {
                    lastHitTarget?.Deactivate(beamId);
                    lastHitTarget = null;
                    break;
                }
                else
                {
                    lastHitTarget?.Deactivate(beamId);
                    lastHitTarget = null;
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

    // ────────────────────────────────────────────────
    // Photon RPC for activating/deactivating the laser for all clients
    // ────────────────────────────────────────────────
    public void SetLaserActive(bool active)
    {
        if (photonView != null)
            photonView.RPC(nameof(RPC_SetActive), RpcTarget.AllBuffered, active);
        else
            RPC_SetActive(active); // fallback if no PhotonView
    }

    [PunRPC]
    private void RPC_SetActive(bool active)
    {
        isActive = active;
        if (lr != null)
            lr.enabled = active;

        // Turn off the last hit target when deactivating
        if (!active && lastHitTarget != null)
        {
            lastHitTarget.Deactivate(beamId);
            lastHitTarget = null;
        }
    }
}
