using UnityEngine;
using System.Collections;

public class MoveDownOnTouch : MonoBehaviour
{
    public float moveDistance = 1f;
    public float moveSpeed = 0f;
    public string triggerTag = "Water";

    // optional counterweight/platform that moves up when parent moves down
    public Transform counterweight;

    // maximum times this component can move its parent
    public int maxMoves = 6;

    // optional pulley ropes: one shortens, one lengthens
    public Transform ropeShorten;               // becomes shorter when platform goes down
    public float ropeShortenUnitLength = 1f;    // world units per local-scale unit for this rope
    public bool ropeShortenAnchorAtTop = true;  // true => keep +Y end fixed

    public Transform ropeLengthen;              // becomes longer when platform goes down
    public float ropeLengthenUnitLength = 1f;
    public bool ropeLengthenAnchorAtTop = true;

    private int movesPerformed = 0;
    private Coroutine moveCoroutine;

    void Start()
    {
        Debug.Log($"MoveDownOnTouch started on '{gameObject.name}'. Parent = {(transform.parent?transform.parent.name:"<none>")}. maxMoves={maxMoves}. counterweight={(counterweight?counterweight.name:"<none>")}");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
        TryMove();
    }

    void OnCollisionEnter(Collision collision)
    {
        var col = collision.collider;
        if (!string.IsNullOrEmpty(triggerTag) && !col.CompareTag(triggerTag)) return;
        TryMove();
    }

    void TryMove()
    {
        if (movesPerformed >= maxMoves)
        {
            Debug.Log($"MoveDownOnTouch: move limit reached ({movesPerformed}/{maxMoves}). Ignoring.");
            return;
        }

        movesPerformed++;
        bool disableAfterThis = movesPerformed >= maxMoves;

        if (moveSpeed > 0f)
        {
            moveCoroutine = StartCoroutine(MoveBothSmooth(disableAfterThis));
        }
        else
        {
            MoveBothInstant();
            if (disableAfterThis)
            {
                Debug.Log($"MoveDownOnTouch: reached max moves ({movesPerformed}/{maxMoves}). Disabling component.");
                enabled = false;
            }
        }
    }

    void MoveBothInstant()
    {
        Transform parent = transform.parent;
        if (parent == null)
        {
            Debug.Log("MoveDownOnTouch: MoveBothInstant aborted - parent is null.");
            return;
        }

        // move parent down
        parent.position += Vector3.down * moveDistance;

        // move counterweight up
        if (counterweight != null)
        {
            counterweight.position += Vector3.up * moveDistance;
        }

        // update ropes
        if (ropeShorten != null) UpdateRopeInstant(ropeShorten, -moveDistance, ropeShortenUnitLength, ropeShortenAnchorAtTop);
        if (ropeLengthen != null) UpdateRopeInstant(ropeLengthen, +moveDistance, ropeLengthenUnitLength, ropeLengthenAnchorAtTop);
    }

    IEnumerator MoveBothSmooth(bool disableAfter)
    {
        Transform parent = transform.parent;
        if (parent == null) yield break;

        Vector3 startParent = parent.position;
        Vector3 targetParent = startParent + Vector3.down * moveDistance;

        bool hasCounter = counterweight != null;
        Vector3 startCounter = Vector3.zero, targetCounter = Vector3.zero;
        if (hasCounter)
        {
            startCounter = counterweight.position;
            targetCounter = startCounter + Vector3.up * moveDistance;
        }

        bool hasShort = ropeShorten != null;
        float startShortLen = 0f, targetShortLen = 0f;
        Vector3 startShortPos = Vector3.zero, targetShortPos = Vector3.zero;
        if (hasShort)
        {
            startShortLen = ropeShorten.localScale.y * ropeShortenUnitLength;
            targetShortLen = Mathf.Max(0.01f, startShortLen - moveDistance);
            startShortPos = ropeShorten.position;
            Vector3 d = -ropeShorten.up * ((targetShortLen - startShortLen) * 0.5f) * (ropeShortenAnchorAtTop ? 1f : -1f);
            targetShortPos = startShortPos + d;
        }

        bool hasLong = ropeLengthen != null;
        float startLongLen = 0f, targetLongLen = 0f;
        Vector3 startLongPos = Vector3.zero, targetLongPos = Vector3.zero;
        if (hasLong)
        {
            startLongLen = ropeLengthen.localScale.y * ropeLengthenUnitLength;
            targetLongLen = Mathf.Max(0.01f, startLongLen + moveDistance);
            startLongPos = ropeLengthen.position;
            Vector3 d = -ropeLengthen.up * ((targetLongLen - startLongLen) * 0.5f) * (ropeLengthenAnchorAtTop ? 1f : -1f);
            targetLongPos = startLongPos + d;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            float lerp = Mathf.Clamp01(t);

            parent.position = Vector3.Lerp(startParent, targetParent, lerp);
            if (hasCounter) counterweight.position = Vector3.Lerp(startCounter, targetCounter, lerp);

            if (hasShort)
            {
                float curLen = Mathf.Lerp(startShortLen, targetShortLen, lerp);
                Vector3 ls = ropeShorten.localScale;
                ropeShorten.localScale = new Vector3(ls.x, curLen / ropeShortenUnitLength, ls.z);
                ropeShorten.position = Vector3.Lerp(startShortPos, targetShortPos, lerp);
            }

            if (hasLong)
            {
                float curLen = Mathf.Lerp(startLongLen, targetLongLen, lerp);
                Vector3 ls = ropeLengthen.localScale;
                ropeLengthen.localScale = new Vector3(ls.x, curLen / ropeLengthenUnitLength, ls.z);
                ropeLengthen.position = Vector3.Lerp(startLongPos, targetLongPos, lerp);
            }

            yield return null;
        }

        parent.position = targetParent;
        if (hasCounter) counterweight.position = targetCounter;

        if (hasShort)
        {
            Vector3 ls = ropeShorten.localScale;
            ropeShorten.localScale = new Vector3(ls.x, targetShortLen / ropeShortenUnitLength, ls.z);
            ropeShorten.position = targetShortPos;
        }

        if (hasLong)
        {
            Vector3 ls = ropeLengthen.localScale;
            ropeLengthen.localScale = new Vector3(ls.x, targetLongLen / ropeLengthenUnitLength, ls.z);
            ropeLengthen.position = targetLongPos;
        }

        if (disableAfter)
        {
            Debug.Log($"MoveDownOnTouch: reached max moves ({movesPerformed}/{maxMoves}). Disabling component.");
            enabled = false;
        }

        moveCoroutine = null;
    }

    void UpdateRopeInstant(Transform rope, float deltaWorldLength, float unitLength, bool anchorAtTop)
    {
        if (rope == null) return;

        float currentWorldLen = rope.localScale.y * unitLength;
        float newWorldLen = Mathf.Max(0.01f, currentWorldLen + deltaWorldLength);

        Vector3 ls = rope.localScale;
        rope.localScale = new Vector3(ls.x, newWorldLen / unitLength, ls.z);

        float delta = newWorldLen - currentWorldLen;
        float anchorSign = anchorAtTop ? 1f : -1f;
        Vector3 worldShift = -rope.up * (delta * 0.5f) * anchorSign;
        rope.position += worldShift;
    }

    void OnDisable()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        StopAllCoroutines();
    }
}