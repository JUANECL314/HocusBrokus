using UnityEngine;

public class EnemyBehabior : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 10f;
    public float moveSpeed = 3f;

    [Header("Random Walk")]
    public float randomWalkSpeed = 2f;
    public float changeDirectionTime = 3f;

    [Header("Stun Settings")]
    public float stunDuration = 2f;

    private bool isStunned = false;
    private float stunTimer = 0f;

    private Transform targetPlayer;
    private Vector3 randomDirection;
    private float directionTimer;

    void Start()
    {
        SetRandomDirection();
    }

    void Update()
    {
        HandleStun();

        if (isStunned)
            return;

        DetectPlayersByTag();

        if (targetPlayer != null)
        {
            FollowTarget();
        }
        else
        {
            RandomWalk();
        }
    }

    // ------------------ STUN SYSTEM ------------------

    void HandleStun()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;

            if (stunTimer <= 0)
            {
                isStunned = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Earth") ||
            other.CompareTag("Wind") ||
            other.CompareTag("Water") ||
            other.CompareTag("Fire"))
        {
            Stun();
        }
    }

    void Stun()
    {
        isStunned = true;
        stunTimer = stunDuration;
    }

    // ------------------ DETECTACIÓN ------------------

    void DetectPlayersByTag()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float closestDist = Mathf.Infinity;
        Transform closestPlayer = null;

        foreach (GameObject p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);

            if (dist < detectionRadius && dist < closestDist)
            {
                closestDist = dist;
                closestPlayer = p.transform;
            }
        }

        targetPlayer = closestPlayer;
    }

    // ------------------ SIGUE AL JUGADOR ------------------

    void FollowTarget()
    {
        Vector3 dir = (targetPlayer.position - transform.position).normalized;

        transform.position += dir * moveSpeed * Time.deltaTime;

        Vector3 lookDir = targetPlayer.position - transform.position;
        lookDir.y = 0;
        transform.rotation = Quaternion.LookRotation(lookDir);
    }

    // ------------------ CAMINATA RANDOM ------------------

    void RandomWalk()
    {
        directionTimer -= Time.deltaTime;

        if (directionTimer <= 0)
        {
            SetRandomDirection();
        }

        Vector3 move = randomDirection * randomWalkSpeed * Time.deltaTime;
        transform.position += move;

        if (randomDirection != Vector3.zero)
        {
            Vector3 lookDir = randomDirection;
            lookDir.y = 0;
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    void SetRandomDirection()
    {
        randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(-1f, 1f)
        ).normalized;

        directionTimer = changeDirectionTime;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
