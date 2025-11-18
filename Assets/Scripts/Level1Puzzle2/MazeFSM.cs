using UnityEngine;

public class MazeFSM : MonoBehaviour 
{
    // Se establece la variable para determinar el estado.
    private IMazeState actualMazeState;

    //Función para cambiar el estado.
    //      Primero, sale del estado actual si es que está
    //      en uno. Después, se cambia al otro estado y lo
    //      inicializa.
    public void ChangeState(IMazeState newState)
    {
        actualMazeState?.Exit();
        actualMazeState = newState;
        actualMazeState.Enter();
    }

    private void Update()
    {
        actualMazeState?.Update();
    }
}
