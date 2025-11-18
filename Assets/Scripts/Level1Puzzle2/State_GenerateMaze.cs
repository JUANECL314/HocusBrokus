using UnityEngine;

public class State_GenerateMaze : IMazeState
{
    private MazeFSM fsm;
    private IMazeGenerator mazeGenerator;

    public State_GenerateMaze(MazeFSM fsm, IMazeGenerator mazeGenerator)
    {
        this.fsm = fsm;
        this.mazeGenerator = mazeGenerator;
    }
    public void Enter() {
        mazeGenerator.Generate();
        //fsm.ChangeState(new State_PlaceElements(fsm));
    }
    public void Update() { }
    public void Exit() { }
}
