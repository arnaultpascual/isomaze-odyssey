using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlipperyTileBehavior : MonoBehaviour
{
    public Vector3 slideDirection;

    public void SetSlideDirection(Vector3 direction)
    {
        slideDirection = direction;
    }
}