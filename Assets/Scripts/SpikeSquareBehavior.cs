using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeSquareBehavior : MonoBehaviour
{
    public Material greenMaterial;
    public Material redMaterial;
    public float changeColorDuration = 1.0f;

    private bool isRed = false;

    private void Start()
    {
        StartCoroutine(ChangeColor());
    }

    private IEnumerator ChangeColor()
    {
        while (true)
        {
            isRed = !isRed;
            GetComponent<Renderer>().material = isRed ? redMaterial : greenMaterial;
            yield return new WaitForSeconds(changeColorDuration);
        }
    }

    public bool IsRed()
    {
        return isRed;
    }
}