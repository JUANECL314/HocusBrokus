using System.Collections;
using UnityEngine;

public class SearchObject : MonoBehaviour
{
    [SerializeField] private string _gameObjectToUse = null;
    [SerializeField] private GameObject _sceneObject = null;
    [SerializeField] private MirrorController _mirrorController = null;
    [SerializeField] private GameObject[] _lista;

    void Start()
    {
        StartCoroutine(BuscarYAsignar());
    }

    IEnumerator BuscarYAsignar()
    {
        // Esperar hasta encontrar el objeto
        while (_sceneObject == null)
        {
            _sceneObject = GameObject.Find(_gameObjectToUse + "(Clone)");
            if (_sceneObject == null)
                yield return new WaitForSeconds(0.5f);
        }

        // Asignar el componente MirrorController cuando el objeto aparece
        _mirrorController = _sceneObject.GetComponent<MirrorController>();

        if (_mirrorController != null)
        {
            Debug.LogWarning("El objeto encontrado no tiene MirrorController.");
            foreach (GameObject obj in _lista)
            {
                MirrorButton boton = obj.GetComponent<MirrorButton>();
                if (boton != null)
                {
                    boton.mirrorController = _mirrorController;
                }
                else
                {
                    Debug.LogWarning($" {obj.name} no tiene componente MirrorButton.");
                }
            }
            yield break;
        }
        

        
    }
}