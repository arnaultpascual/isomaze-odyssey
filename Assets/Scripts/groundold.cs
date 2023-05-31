/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GroundGenerator : MonoBehaviour
{
    [SerializeField] private GameObject squarePrefab;
    [SerializeField] public int gridSize = 20;
    [SerializeField] private float gap = 1.0f;
    [SerializeField] public float gridYPosition = 0.0f;
    [SerializeField] private GameObject player;

    [SerializeField] private Material greenMaterial;
    [SerializeField] public Vector2Int greenSquarePosition;

    [SerializeField] private TextMeshPro[] levelTexts;
    [SerializeField] private float levelTextAnimationDuration = 0.5f;

    [SerializeField] private Color level1Color = Color.green;
    [SerializeField] private Color level50Color = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color level100Color = Color.red;

    [SerializeField] private GameObject crumblingTilePrefab;
    [SerializeField] private GameObject spikeSquarePrefab;
    [SerializeField] public GameObject teleportTilePrefab;
    [SerializeField] private GameObject slipperyTilePrefab;

    private List<GameObject> _levels = new List<GameObject>();

    public int slipperyTileLines = 3;
    public int minLineLength = 3;
    public int maxLineLength = 5;

    private int _currentLevel = 0;

    private Dictionary<Vector2Int, GameObject> squareMap;

    void Start()
    {
        squareMap = new Dictionary<Vector2Int, GameObject>();
        StartCoroutine(GenerateGridSquareBySquare(0f));
        UpdateLevelText();
    }

    // Add the ContextMenu attribute to a new method
    [ContextMenu("Generate Level")]
    public void GenerateLevelInEditor()
    {
        squareMap = new Dictionary<Vector2Int, GameObject>();
        StartCoroutine(GenerateGridSquareBySquare(0f));
        UpdateLevelText();
    }

    public IEnumerator GenerateGridSquareBySquare(float delayBetweenSquares)
    {
        ClearGrid();

        // Create a new parent object for this level
        GameObject levelParent = new GameObject("Level " + _currentLevel);
        levelParent.AddComponent<MeshRenderer>();
        levelParent.AddComponent<MeshFilter>();
        _levels.Add(levelParent);

        // Generate slippery tile lines before other square types
        for (int i = 0; i < slipperyTileLines; i++)
        {
            Vector2Int startPosition = new Vector2Int(Random.Range(0, gridSize), Random.Range(0, gridSize));
            Vector2Int direction = (Random.Range(0, 2) == 0) ? Vector2Int.right : Vector2Int.up;
            int lineLength = Random.Range(minLineLength, maxLineLength + 1);

            GenerateSlipperyTileLine(startPosition, direction, lineLength);
        }

        List<Vector2Int> sortedPositions = GetSquarePositionsSortedByDistanceFromPlayer();

        foreach (Vector2Int gridPosition in sortedPositions)
        {
            // Instantiate other square types only if there's nothing in the dictionary key for that position
            if (!squareMap.ContainsKey(gridPosition))
            {
                float randomYOffset = Random.Range(-1f, 0f);
                Vector3 position = GridToWorldPosition(gridPosition) + new Vector3(0, randomYOffset, 0);

                // Generate a random float between 0 and 1
                float randomValue = Random.Range(0f, 1f);

                if (randomValue < 0.1f)
                {
                    GameObject newSquare = Instantiate(crumblingTilePrefab, position, Quaternion.identity, levelParent.transform);
                    squareMap.Add(gridPosition, newSquare);
                }
                else if (randomValue >= 0.1f && randomValue < 0.2f) // 10% chance
                {
                    GameObject newSquare = Instantiate(teleportTilePrefab, position, Quaternion.identity, levelParent.transform);
                    squareMap.Add(gridPosition, newSquare);
                }
                else if (randomValue >= 0.2f && randomValue < 0.3f) // 10% chance
                {
                    GameObject newSquare = Instantiate(spikeSquarePrefab, position, Quaternion.identity, levelParent.transform);
                    squareMap.Add(gridPosition, newSquare);
                }
                else
                {
                    GameObject newSquare = Instantiate(squarePrefab, position, Quaternion.identity, levelParent.transform);
                    squareMap.Add(gridPosition, newSquare);
                }
            }

            StartCoroutine(MoveSingleSquareToFinalPosition(gridPosition, 1.0f, levelParent.transform));

            yield return new WaitForSeconds(delayBetweenSquares);
        }

        SetRandomSquareGreen();
        UnactiveLastLevel();
    }

    public void UnactiveLastLevel(){ 
        if (_levels.Count > 3 && _levels[3] != null)
        {
            StartCoroutine(FadeOutLevel(_levels[_currentLevel-3], 1f));
        }
        //StartCoroutine(FadeOutLevel(_levels[_currentLevel-1], 1f));  // 1 second fade out
    }

    public IEnumerator FadeOutLevel(GameObject level, float duration)
    {
        float counter = 0;

        // Assuming the level has a MeshRenderer component and you want to fade out the material
        MeshRenderer[] renderers = level.GetComponentsInChildren<MeshRenderer>();

        // Store the initial colors
        Color[] initialColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            initialColors[i] = renderers[i].material.color;
        }

        while (counter < duration)
        {
            counter += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, counter / duration);

            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = initialColors[i];
                color.a = alpha;
                renderers[i].material.color = color;
            }

            yield return null;
        }

        // Deactivate the level after the fade out
        level.SetActive(false);
    }


    public IEnumerator MoveSingleSquareToFinalPosition(Vector2Int gridPosition, float duration, Transform parentTransform)
    {
        if (squareMap.ContainsKey(gridPosition))
        {
            GameObject square = squareMap[gridPosition];
            square.transform.parent = parentTransform;
            Vector3 startPosition = square.transform.position;
            Vector3 targetPosition = new Vector3(startPosition.x, gridYPosition, startPosition.z);
            float elapsedTime = 0;

            while (elapsedTime < duration)
            {
                square.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            square.transform.position = targetPosition;
        }
    }

    private List<Vector2Int> GetSquarePositionsSortedByDistanceFromPlayer()
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                positions.Add(new Vector2Int(x, z));
            }
        }

        positions.Sort((a, b) =>
        {
            Vector3 worldPosA = GridToWorldPosition(a);
            Vector3 worldPosB = GridToWorldPosition(b);

            float distanceA = Vector3.Distance(worldPosA, player.transform.position);
            float distanceB = Vector3.Distance(worldPosB, player.transform.position);
            return distanceA.CompareTo(distanceB);
        });

        return positions;
    }

    private void ClearGrid()
    {
        squareMap.Clear();
    }


    public GameObject GetSquareAtPosition(Vector2Int gridPosition)
    {
        if (squareMap.ContainsKey(gridPosition))
        {
            return squareMap[gridPosition];
        }

        return null;
    }

    public void SetRandomSquareGreen()
    {
        int randomX, randomZ;
        Vector2Int playerGridPosition = WorldToGridPosition(player.transform.position);
        GameObject winningSquare = null;

        do
        {
            randomX = Random.Range(0, gridSize);
            randomZ = Random.Range(0, gridSize);
            Vector2Int potentialGreenSquarePosition = new Vector2Int(randomX, randomZ);

            if (Vector2Int.Distance(potentialGreenSquarePosition, playerGridPosition) < 16)
            {
                continue;
            }

            winningSquare = GetSquareAtPosition(potentialGreenSquarePosition);

            // If the square is not null and it's a regular square
            if (winningSquare != null && winningSquare.tag == "Regular")
            {
                greenSquarePosition = potentialGreenSquarePosition;
                break;
            }
        } while (true);

        if (winningSquare != null)
        {
            Renderer squareRenderer = winningSquare.GetComponent<Renderer>();
            squareRenderer.material = greenMaterial;
        }
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        float roundedX = Mathf.Round(worldPosition.x);
        float roundedZ = Mathf.Round(worldPosition.z);

        int x = Mathf.FloorToInt(roundedX / (squarePrefab.transform.localScale.x + gap));
        int z = Mathf.FloorToInt(roundedZ / (squarePrefab.transform.localScale.z + gap));

       return new Vector2Int(x, z);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * (squarePrefab.transform.localScale.x + gap), gridYPosition, gridPosition.y * (squarePrefab.transform.localScale.z + gap));
    }

    private bool IsValidGridPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridSize && position.y >= 0 && position.y < gridSize;
    }

    private void GenerateSlipperyTileLine(Vector2Int startPosition, Vector2Int direction, int length)
    {
        for (int i = 0; i < length; i++)
        {
            Vector2Int currentPosition = startPosition + direction * i;
            if (IsValidGridPosition(currentPosition))
            {
                Vector3 worldPosition = GridToWorldPosition(currentPosition);
                GameObject slipperyTile = Instantiate(slipperyTilePrefab, worldPosition, Quaternion.identity, transform);
                slipperyTile.GetComponent<SlipperyTileBehavior>().slideDirection = new Vector3(direction.x, 0, direction.y);
                squareMap[currentPosition] = slipperyTile;
            }
        }
    }

    //--------- Level text  Part -------------//

    public int CurrentLevel
    {
        get { return _currentLevel; }
        set
        {
            _currentLevel = value;
            MoveLevelTexts(CurrentLevel);

        }
    }

    private void UpdateLevelText()
    {
        foreach (TextMeshPro levelText in levelTexts)
        {
            if (levelText != null)
            {
                levelText.text = "Level: " + CurrentLevel.ToString();
            }
        }
    }

    public void MoveLevelTexts(int newLevel)
    {
        foreach (TextMeshPro levelText in levelTexts)
        {
            if (levelText != null)
            {
                StartCoroutine(AnimateLevelText(levelText, newLevel));
            }
        }
    }

    private IEnumerator AnimateLevelText(TextMeshPro levelText, int newLevel)
    {
        float elapsedTime = 0f;
        float initialY = levelText.transform.position.y;
        float targetY = initialY + 1f;

        Color initialColor = levelText.color;
        Color targetColor = GetLevelTextColor(newLevel +90);

        while (elapsedTime < levelTextAnimationDuration)
        {
            float newY = Mathf.Lerp(initialY, targetY, elapsedTime / levelTextAnimationDuration);
            levelText.transform.position = new Vector3(levelText.transform.position.x, newY, levelText.transform.position.z);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        levelText.transform.position = new Vector3(levelText.transform.position.x, targetY, levelText.transform.position.z);
        levelText.text = "Level: " + newLevel.ToString();
        levelText.color = targetColor;
    }

    private Color GetLevelTextColor(int level)
    {
        if (level <= 50)
        {
            float t = Mathf.InverseLerp(1, 50, level);
            return Color.Lerp(level1Color, level50Color, t);
        }
        else
        {
            float t = Mathf.InverseLerp(50, 100, level);
            return Color.Lerp(level50Color, level100Color, t);
        }
    }

    //----------------------- end text --------------------//
}*/
