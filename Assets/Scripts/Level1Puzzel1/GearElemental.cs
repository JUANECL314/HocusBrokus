using System.Collections;
using System.Collections.Generic;   
using UnityEngine;

public class GearElemental : MonoBehaviour
{
    public enum MagicType { None, Air, Fire, Water, Earth }

    public float rotationSpeed = 100f;
    public float moveSpeed = 0.5f;
    public Color normalColor = Color.gray;
    public Color heatedColor = Color.red;
    public Vector3 initialPosition;

    private Renderer rend;
    private bool rotating = false;
    private float heatProgress = 0f;
    private string LoopId => $"gear_loop_{GetInstanceID()}";

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material.color = normalColor;
        initialPosition = transform.position;
    }

    void Update()
    {
        if (rotating)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);

            if (heatProgress < 1f)
            {
                heatProgress += Time.deltaTime * 0.1f;
                rend.material.color = Color.Lerp(normalColor, heatedColor, heatProgress);
                if (heatProgress >= 1f)
                    GearManager.Instance.StartOverheatCountdown();
            }
        }
    }

    public void StartRotation()
    {
        if (!rotating)
        {
            rotating = true;

            SoundManager.Instance?.Play(SfxKey.GearStart, transform);
            SoundManager.Instance?.StartLoop(LoopId, SfxKey.GearLoop, transform);
        }
    }

    public void StopRotation()
    {
        if (rotating)
        {
            rotating = false;

            SoundManager.Instance?.StopLoop(LoopId);
            SoundManager.Instance?.Play(SfxKey.GearStop, transform);
        }
    }

    public void CoolDown()
    {
        heatProgress = 0f;
        rend.material.color = normalColor;
        rotating = true;

        SoundManager.Instance?.Play(SfxKey.GearCoolHiss, transform);

        SoundManager.Instance?.StartLoop(LoopId, SfxKey.GearLoop, transform);
    }

    public void ResetGear()
    {
        transform.position = initialPosition;
        heatProgress = 0f;
        rend.material.color = normalColor;
        rotating = false;
        SoundManager.Instance?.StopLoop(LoopId);
    }

    /*private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Magic"))
        {
            MagicProjectile magic = other.GetComponent<MagicProjectile>();

            if (magic.type == MagicType.Air || magic.type == MagicType.Fire)
            {
                if (magic.type == MagicType.Air && magic.otherType == MagicType.Fire ||
                    magic.type == MagicType.Fire && magic.otherType == MagicType.Air)
                {
                    GearManager.Instance.ActivateGears();
                }
            }
            else if (magic.type == MagicType.Water)
            {
                GearManager.Instance.CoolDownGears();
            }
            else if (magic.type == MagicType.Earth)
            {
                GearManager.Instance.ResetAllGears();
            }
        }
    }*/
}
