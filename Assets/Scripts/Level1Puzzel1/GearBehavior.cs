using System.Collections;
using UnityEngine;

public class GearBehavior : MonoBehaviour
{
    private Renderer rend;
    private Rigidbody rb;
    [Header("Reactivation")]
    [SerializeField] private bool autoReactivateOnLand = true; // Tierra sí reactiva; Overheat la desactiva
    public void SetAutoReactivateOnLand(bool v) => autoReactivateOnLand = v;
    [Header("State")]
    [SerializeField] private bool isRotating = false;
    public bool isFalling = false;
    private bool destroyedDoors = false;
    private Vector3 initialPosition;
    public bool IsRotating => isRotating;

    [Header("Tuning")]
    public float rotationSpeed = 150f;
    public float timeToDestroyDoors = 20f;

    [Tooltip("Si NO se enfría antes de este tiempo, se sobrecalienta y se PAUSA la puerta.")]
    public float overheatSeconds = 15f;

    [Tooltip("Tras enfriarse, cuánto tarda en rearmarse el contador de sobrecalentamiento.")]
    public float overheatRearmSeconds = 6f;

    [Tooltip("Tras este tiempo girando, el engranaje cae (aleatorio lo gestiona el puzzle).")]
    public float fallSpeed = 2f; // velocidad de caida controlada

    private string LoopId => $"gear_loop_{GetInstanceID()}";

    // Coroutines
    private Coroutine rotateFlowCo;
    private Coroutine destroyDoorsCo;
    private Coroutine overheatCo;

    // Marca si se enfrio en la ventana actual
    private bool cooledDuringWindow = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"{name} no tiene Rigidbody");
            return;
        }

        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        initialPosition = transform.position;
        rend.material.color = Color.gray;
    }

    void Update()
    {
        if (isRotating) transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        if (isFalling)
        {
            rb.isKinematic = false;
            rb.linearVelocity = new Vector3(0, -fallSpeed, 0);
        }
    }

    public void StartRotation()
    {
        if (isRotating) return;

        isRotating = true;
        cooledDuringWindow = false;

        SoundManager.Instance?.Play(SfxKey.GearStart, transform);
        SoundManager.Instance?.StartLoop(LoopId, SfxKey.GearLoop, transform);

        if (rotateFlowCo != null) StopCoroutine(rotateFlowCo);
        rotateFlowCo = StartCoroutine(RotateAndChangeColorFlow());

        if (overheatCo != null) StopCoroutine(overheatCo);
        overheatCo = StartCoroutine(OverheatCountdown());
    }

    public void StopRotation()
    {
        if (!isRotating) return;

        isRotating = false;

        SoundManager.Instance?.StopLoop(LoopId);
        SoundManager.Instance?.Play(SfxKey.GearStop, transform);

        if (rotateFlowCo != null) { StopCoroutine(rotateFlowCo); rotateFlowCo = null; }
        if (overheatCo != null) { StopCoroutine(overheatCo); overheatCo = null; }
    }

    private IEnumerator RotateAndChangeColorFlow()
    {
        rend.material.color = Color.gray;
        yield return new WaitForSeconds(0.5f);
        if (!isRotating) yield break;

        rend.material.color = new Color(1f, 0.5f, 0f);
        yield return new WaitForSeconds(0.5f);
        if (!isRotating) yield break;

        rend.material.color = Color.red;

    }


    public void CoolDown()
    {
        // Visual
        rend.material.color = Color.gray;

        SoundManager.Instance?.Play(SfxKey.GearCoolHiss, transform);

        // Marca enfriado y rearma contador tras una espera
        cooledDuringWindow = true;
        if (overheatCo != null) StopCoroutine(overheatCo);
        StartCoroutine(RearmOverheatAfterDelay());
    }
    public void ResetToInitialPosition(bool smooth = true)
    {
        // corta caida y físicas
        isFalling = false;
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (!smooth)
        {
            transform.position = initialPosition;
        }
        else
        {
            StartCoroutine(ReturnToInitialPosition());
        }
    }
    private IEnumerator OverheatCountdown()
    {
        cooledDuringWindow = false;
        float t = 0f;
        while (t < overheatSeconds)
        {
            if (!isRotating) yield break;        
            if (cooledDuringWindow) yield break; 
            t += Time.deltaTime;
            yield return null;
        }
        // No se enfrió a tiempo
        Overheat();
    }

    private IEnumerator RearmOverheatAfterDelay()
    {
        float t = 0f;
        while (t < overheatRearmSeconds)
        {
            if (!isRotating) yield break;
            t += Time.deltaTime;
            yield return null;
        }
        if (isRotating) overheatCo = StartCoroutine(OverheatCountdown());
    }

    private void Overheat()
    {
        StopRotation();

        var puzzle = FindObjectOfType<ElementalPuzzle>();
        if (puzzle != null)
        {
            // Pausa el progreso de la puerta y resetea TODO a posicion inicial
            puzzle.ResetFromOverheatAndReturnAll();
        }
    }
    public void ReactivateAfterLand()
    {
        if (!isRotating && !isFalling)
        {
            // Cambia el color a caliente otra vez
            rend.material.color = Color.red;

            // Reinicia la rotación
            isRotating = true;
            SoundManager.Instance?.Play(SfxKey.GearStart, transform);
            SoundManager.Instance?.StartLoop(LoopId, SfxKey.GearLoop, transform);
        }
    }


    public void MakeFall()
    {
        if (isFalling) return;

        isFalling = true;

        // Marcar que NO esta girando
        isRotating = false;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        SoundManager.Instance?.StopLoop(LoopId);
        SoundManager.Instance?.Play(SfxKey.GearFall, transform);

        var puzzle = FindObjectOfType<ElementalPuzzle>();
        if (puzzle != null) puzzle.DoorPause(true);
    }


    void OnCollisionEnter(Collision collision)
    {
        // Agua => ENFRIAR 
        if (collision.gameObject.CompareTag("Water") && isRotating)
            CoolDown();

        // Tierra => volver al inicio si estaba cayendo
        if (collision.gameObject.CompareTag("Earth") && isFalling)
            StartCoroutine(ReturnToInitialPosition());

        // Suelo => detener caida
        if (collision.gameObject.CompareTag("Ground") && isFalling)
        {
            isFalling = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            SoundManager.Instance?.Play(SfxKey.GearStop, transform);
        }
    }

    IEnumerator ReturnToInitialPosition()
    {
        isFalling = false;
        rb.isKinematic = true;

        Vector3 start = transform.position;
        float elapsed = 0f;
        float duration = 2f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, initialPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = initialPosition;

        if (autoReactivateOnLand)
            ReactivateAfterLand();

    }


    void OnDisable() { SoundManager.Instance?.StopLoop(LoopId); }
    void OnDestroy() { SoundManager.Instance?.StopLoop(LoopId); }
}
