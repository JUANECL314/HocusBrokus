using UnityEngine;
using System.Collections;
using Photon.Pun;

public class HiddenPathController : MonoBehaviourPun
{
    public float targetHeight = 170f;
    public float speed = 5f;
    private bool activated = false;

    

    // Llamar para activar el path en todos los clientes
    public void ShowPath()
    {
        if (!activated)
        {
            activated = true;
            photonView.RPC(nameof(RPC_ShowPath), RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_ShowPath()
    {
        StartCoroutine(MoveUp());
    }

    private IEnumerator MoveUp()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(startPos.x, targetHeight, startPos.z);

        float distance = Vector3.Distance(startPos, endPos);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * speed / distance;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
    }
}
