using UnityEngine;

public class MazeRunnerRole : IRole
{
    public string message => "Rol: Runner. A correr";
    public string ShowRole()
    {
        return message;
    }
    
    public void Execute()
    {

    }
}

public class MazeViewerRole : IRole
{
    public string message => "Rol: Viewer. A observar.";
    public string ShowRole()
    {
        return message;
    }
    public void Execute()
    {
        Debug.Log("Viewer lo ve todo.");
    }
}

public class MazeChangerRole : IRole
{
    public string message => "Rol: Changer. A cambiar.";
    public string ShowRole()
    {
        return message;
    }
    public void Execute()
    {
        Debug.Log("Tienes el control maestro.");
    }
}

public class MazeDetecterRole : IRole
{
    public string message => "Rol: Detecter. A observar.";
    public string ShowRole()
    {
        return message;
    }
    public void Execute()
    {
        Debug.Log("Detecta las amenazas y los obstáculos.");
    }
}