using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrumblingTileBehavior : MonoBehaviour
{
    public float crumbleDelay = 1.0f;
    public float resetDelay = 0.5f;
    public float riseHeight = 0.5f;

    private CubeMovement playerCubeMovement;

    public void TriggerCrumble(CubeMovement playerCube)
    {
        playerCubeMovement = playerCube;
        StartCoroutine(CrumbleAndResetPlayer());
    }

    private IEnumerator CrumbleAndResetPlayer()
    {
        yield return new WaitForSeconds(crumbleDelay);

        // Smoothly move player upward by riseHeight
        float riseDuration = 0.5f; // You can adjust this value for a faster or slower rise animation
        StartCoroutine(playerCubeMovement.MoveUpCrumblingSmoothly(riseDuration, riseHeight));

        // Wait for resetDelay
        yield return new WaitForSeconds(resetDelay);

        // Call the ResetPlayerPositionSmoothly method from CubeMovement
        float resetDuration = 0.5f; // You can adjust this value for a faster or slower reset animation
        StartCoroutine(playerCubeMovement.ResetPlayerPositionSmoothly(resetDuration));
    }
}
