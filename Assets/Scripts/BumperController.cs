using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BumperController : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger detected with: " + other.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Trigger with Player");

            CubeMovement cubeMovement = other.GetComponent<CubeMovement>();
            if (cubeMovement != null)
            {
                //cubeMovement.M();
            }
        }
    }

}