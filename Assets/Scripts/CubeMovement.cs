using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CubeMovement : MonoBehaviour
{
    [SerializeField] private float rollSpeed = 5;
    [SerializeField] private GroundGenerator groundGenerator;
    private Vector3 initialPosition;

    private bool isMoving;

    void Start(){
        initialPosition = transform.position;
    }

    private void Update()
    {
        if (isMoving) return;

        if (Input.GetKey(KeyCode.Q)) Assemble(Vector3.left);
        else if (Input.GetKey(KeyCode.D)) Assemble(Vector3.right);
        else if (Input.GetKey(KeyCode.Z)) Assemble(Vector3.forward);
        else if (Input.GetKey(KeyCode.S)) Assemble(Vector3.back);

        if (Input.GetKey(KeyCode.LeftArrow)) Assemble(Vector3.left);
        else if (Input.GetKey(KeyCode.RightArrow)) Assemble(Vector3.right);
        else if (Input.GetKey(KeyCode.UpArrow)) Assemble(Vector3.forward);
        else if (Input.GetKey(KeyCode.DownArrow)) Assemble(Vector3.back);

        void Assemble(Vector3 dir)
        {
            Vector3 anchor = transform.position + (Vector3.down + dir) * 0.5f;
            Vector3 axis = Vector3.Cross(Vector3.up, dir);
            StartCoroutine(Roll(anchor, axis));
        }
    }

    private IEnumerator Roll(Vector3 anchor, Vector3 axis)
    {
        if (isMoving) yield break;

        isMoving = true;
        for (var i = 0; i < 90 / rollSpeed; i++)
        {
            transform.RotateAround(anchor, axis, rollSpeed);
            yield return new WaitForSeconds(0.01f);
        }
        isMoving = false;

        CheckCrumblingTile();
        CheckSpikeSquare();
        CheckTeleportTile();
        CheckSlipperyTile();

        if (groundGenerator.WorldToGridPosition(transform.position) == groundGenerator.winningSquarePosition)
        {
            float animationDuration = 0.5f;
            StartCoroutine(MoveUpSmoothly(animationDuration));
            groundGenerator.gridYPosition += 1f;
            groundGenerator.CurrentLevel++;
            StartCoroutine(groundGenerator.GenerateMaze(0f));
        }
    }

    private IEnumerator MoveUpSmoothly(float duration)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = new Vector3(startPosition.x, startPosition.y + 1f, startPosition.z);
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        initialPosition = targetPosition;
    }

    public IEnumerator MoveUpCrumblingSmoothly(float duration, float height)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = new Vector3(startPosition.x, startPosition.y + height, startPosition.z);
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
    }

    public void ResetPlayerPosition()
    {
        transform.position = initialPosition;
    }

    public IEnumerator ResetPlayerPositionSmoothly(float duration)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = initialPosition;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
    }

    private Vector2Int GetCurrentGridPosition()
    {
        return groundGenerator.WorldToGridPosition(transform.position);
    }

    private void CheckCrumblingTile()
    {
        Vector2Int currentGridPosition = GetCurrentGridPosition();
        GameObject square = groundGenerator.GetSquareAtPosition(currentGridPosition);
        if (square != null)
        {
            CrumblingTileBehavior crumblingTile = square.GetComponent<CrumblingTileBehavior>();
            if (crumblingTile != null)
            {
                crumblingTile.TriggerCrumble(this);
            }
        }
    }

    private void CheckSpikeSquare()
    {
        Vector2Int currentGridPosition = GetCurrentGridPosition();
        GameObject square = groundGenerator.GetSquareAtPosition(currentGridPosition);
        if (square != null)
        {
            SpikeSquareBehavior spikeSquare = square.GetComponent<SpikeSquareBehavior>();
            if (spikeSquare != null && spikeSquare.IsRed())
            {
                ReloadScene();
            }
        }
    }

    private void CheckTeleportTile()
    {
        Vector2Int currentGridPosition = GetCurrentGridPosition();
        GameObject square = groundGenerator.GetSquareAtPosition(currentGridPosition);
        if (square != null && square.GetComponent<TeleportTileBehavior>() != null)
        {
            TeleportToRandomPosition();
        }
    }

    private void TeleportToRandomPosition()
    {
        int gridSize = groundGenerator.gridSize;
        int randomX = Random.Range(0, gridSize);
        int randomZ = Random.Range(0, gridSize);
        Vector2Int randomGridPosition = new Vector2Int(randomX, randomZ);
        Vector3 newPosition = groundGenerator.GridToWorldPosition(randomGridPosition);
        newPosition.y = transform.position.y;
        transform.position = newPosition;

        CheckCrumblingTile();
        CheckSpikeSquare();
        CheckTeleportTile();
        CheckSlipperyTile();
    }

    private void CheckSlipperyTile()
    {
        Vector2Int currentGridPosition = GetCurrentGridPosition();
        GameObject square = groundGenerator.GetSquareAtPosition(currentGridPosition);
        if (square != null)
        {
            SlipperyTileBehavior slipperyTile = square.GetComponent<SlipperyTileBehavior>();
            if (slipperyTile != null)
            {
                StartCoroutine(SlideOnSlipperyTiles(slipperyTile.slideDirection));
            }
        }
    }

    private IEnumerator SlideOnSlipperyTiles(Vector3 slideDirection)
    {
        while (true)
        {
            Vector3 nextPosition = transform.position + slideDirection;
            Vector2Int nextGridPosition = groundGenerator.WorldToGridPosition(nextPosition);
            GameObject nextSquare = groundGenerator.GetSquareAtPosition(nextGridPosition);
            SlipperyTileBehavior nextSlipperyTile = null;

            if (nextSquare != null)
            {
                nextSlipperyTile = nextSquare.GetComponent<SlipperyTileBehavior>();
            }

            if (nextSlipperyTile != null && nextSlipperyTile.slideDirection == slideDirection)
            {
                StartCoroutine(Glide(transform.position, nextPosition, 0.3f));
                yield return new WaitForSeconds(0.3f);
            }
            else
            {
                // Slide one more time after leaving slippery tiles
                StartCoroutine(Glide(transform.position, transform.position + slideDirection, 0.3f));
                yield return new WaitForSeconds(0.3f);
                break;
            }
        }
    }

    private IEnumerator Glide(Vector3 startPosition, Vector3 targetPosition, float duration)
    {
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        CheckCrumblingTile();
        CheckSpikeSquare();
        CheckTeleportTile();
        CheckSlipperyTile();
    }

    private void ReloadScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}
