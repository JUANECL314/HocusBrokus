
using UnityEngine;
using TMPro;
public class MovementPOI : MonoBehaviour
{
    public enum MovementState
    {
        Init,
        Down,
        Up
    }

    public MovementState state = MovementState.Init;
    private Vector3 originPos;
    public float limitMov = 2f;
    public float speed = 2f;
    public string text = string.Empty;

    public TMP_Text textElement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originPos = transform.position;
        state = MovementState.Down;
        if (textElement != null )
        {
            textElement.text = text;
        }
        
    }

   void Update()
    {
        StateMachine();
    }

    void StateMachine()
    {
        switch (state)
        {
            case MovementState.Down:
                MoveDown();
                break;
            case MovementState.Up:
                MoveUp();
                break;
        }
    }

    void MoveDown()
    {
        Vector3 target = originPos - new Vector3(0, limitMov, 0);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            state = MovementState.Up;
        }
    }

    void MoveUp()
    {
        Vector3 target = originPos;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            state = MovementState.Down;
        }
    }
}
