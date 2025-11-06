using System.Collections;
using UnityEngine;

public class SearchObject : MonoBehaviour
{
    [SerializeField] private string gameObjectToUse = null;
    [SerializeField] private GameObject sceneObject = null;
    [SerializeField] private MirrorController mirrorController = null;
    [SerializeField] private GameObject[] lista;

    void Start()
    {
        StartCoroutine(BuscarYAsignar());
    }

    IEnumerator BuscarYAsignar()
    {
        // Esperar hasta encontrar el objeto
        while (sceneObject == null)
        {
            sceneObject = GameObject.Find(gameObjectToUse + "(Clone)");
            if (sceneObject == null)
                yield return new WaitForSeconds(0.5f);
        }

        // Asignar el componente MirrorController cuando el objeto aparece
        mirrorController = sceneObject.GetComponent<MirrorController>();

        if (mirrorController == null)
        {
            Debug.LogWarning("El objeto encontrado no tiene MirrorController.");
            yield break;
        }

        // Asignar controller a cada botón de la lista
        foreach (GameObject obj in lista)
        {
            MirrorButton boton = obj.GetComponent<MirrorButton>();
            if (boton != null)
            {
                boton.mirrorController = mirrorController;
            }
            else
            {
                Debug.LogWarning($" {obj.name} no tiene componente MirrorButton.");
            }
        }

        Debug.Log("MirrorController asignado a todos los botones.");
    }
}