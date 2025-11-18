using UnityEngine;
// La interfaz base con las funciones para cada estado
public interface IMazeState
{
    void Enter();
    void Update();
    void Exit();
}