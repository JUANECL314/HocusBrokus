using UnityEngine;
using TMPro;


public abstract class MovementPOIBase : MonoBehaviour
{
    public enum MovementState { Init, Down, Up }
    public MovementState state = MovementState.Init;

    protected Vector3 originPos;
    public float limitMov = 2f;
    public float speed = 2f;

    void Start()
    {
        originPos = transform.position;
        state = MovementState.Down;
        OnStart(); // Hook para que las subclases agreguen comportamiento
    }

    void Update()
    {
        StateMachine();
    }

    void StateMachine()
    {
        switch (state)
        {
            case MovementState.Down: MoveDown(); break;
            case MovementState.Up: MoveUp(); break;
        }
    }

    void MoveDown()
    {
        Vector3 target = originPos - new Vector3(0, limitMov, 0);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.01f)
            state = MovementState.Up;

        OnMoveDown(); // Hook
    }

    void MoveUp()
    {
        Vector3 target = originPos;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.01f)
            state = MovementState.Down;

        OnMoveUp(); // Hook
    }

    
    protected virtual void OnStart() { }
    protected virtual void OnMoveDown() { }
    protected virtual void OnMoveUp() { }
}