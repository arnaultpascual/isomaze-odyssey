using System.Collections;
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
    [SerializeField] public Vector2Int winningSquarePosition;

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

    private int _currentLevel = 1;

    private Dictionary<Vector2Int, GameObject> squareMap;

    void Start()
    {
        squareMap = new Dictionary<Vector2Int, GameObject>();
        StartCoroutine(GenerateMaze(0f));
        UpdateLevelText();
    }

    // Add the ContextMenu attribute to a new method
    [ContextMenu("Generate Level")]
    public void GenerateLevelInEditor()
    {
        squareMap = new Dictionary<Vector2Int, GameObject>();
        StartCoroutine(GenerateMaze(0f));
        UpdateLevelText();
    }

    public IEnumerator GenerateMaze(float delayBetweenSquares)
    {
        ClearGrid();

        // Create a new parent object for this level
        GameObject levelParent = new GameObject("Level " + _currentLevel);
        levelParent.AddComponent<MeshRenderer>();
        levelParent.AddComponent<MeshFilter>();
        _levels.Add(levelParent);

        // Determine the starting point based on the current level
        Vector2Int startPoint;
        if (_currentLevel == 1)
        {
            startPoint = new Vector2Int(1, 1); // assuming your player starts at 1:1 on the grid
        }
        else
        {
            // assuming winningSquare is a Vector2Int variable that stores the winning square position
            startPoint = winningSquarePosition;
        }

        // Initialize the grid with CrumblingTile
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                Vector3 worldPosition = GridToWorldPosition(gridPosition);
                // Instantiate a tile
                GameObject newTile = Instantiate(crumblingTilePrefab, worldPosition, Quaternion.identity, levelParent.transform);
                newTile.SetActive(false);
                squareMap[gridPosition] = newTile;
            }
        }

        // Generate the maze using recursive backtracking
        bool[,] maze = GenerateMazeUsingRecursiveBacktracking(gridSize, gridSize, startPoint);

        // Generate maze path and replace CrumblingTile with SquarePrefab in the path
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                if (maze[x, y])
                {
                    // If it's a path in the maze, replace the CrumblingTile with SquarePrefab
                    Destroy(squareMap[gridPosition]); // Destroy the CrumblingTile
                    Vector3 worldPosition = GridToWorldPosition(gridPosition);
                    GameObject newSquare = Instantiate(squarePrefab, worldPosition, Quaternion.identity, levelParent.transform);
                    newSquare.SetActive(false);
                    squareMap[gridPosition] = newSquare;
                }
            }
        }

        // Animate the maze after generation
        yield return StartCoroutine(AnimateMaze(delayBetweenSquares));
        
        // After generating the maze, generate traps
        GenerateTraps(levelParent);
        
        SetRandomSquareGreen();
        UnactiveLastLevel();
    }

    public IEnumerator AnimateMaze(float delayBetweenSquares)
    {
        // Get the grid positions sorted by their distance from the player
        List<Vector2Int> sortedPositions = GetSquarePositionsSortedByDistanceFromPlayer();

        foreach (Vector2Int gridPosition in sortedPositions)
        {
            GameObject tile = squareMap[gridPosition];
            tile.SetActive(true);
            StartCoroutine(MoveSingleSquareToFinalPosition(gridPosition, 1.0f, _levels[_currentLevel - 1].transform));
            yield return new WaitForSeconds(delayBetweenSquares);
        }
    }

    private List<Vector2Int> GetSquarePositionsSortedByDistanceFromPlayer()
    {

        Vector2Int playerPos;
        if (_currentLevel == 1)
        {
            playerPos = new Vector2Int(1, 1); // assuming your player starts at 1:1 on the grid
        }
        else
        {
            playerPos = winningSquarePosition;
        }

        List<Vector2Int> squarePositions = new List<Vector2Int>(squareMap.Keys);

        // Sort positions by their distance from the player, in ascending order
        squarePositions.Sort((pos1, pos2) => Vector2Int.Distance(playerPos, pos1).CompareTo(Vector2Int.Distance(playerPos, pos2)));

        return squarePositions;
    }

    private void GenerateTraps(GameObject levelParent)
    {
        // A list of all the potential trap positions in the maze
        List<Vector2Int> potentialTrapPositions = new List<Vector2Int>(squareMap.Keys);

        // Shuffle the list of potential trap positions to randomize trap placement
        potentialTrapPositions = Shuffle(potentialTrapPositions);

        // Determine the number of traps based on the current level (increase the difficulty)
        int numberOfTraps = Mathf.Min(_currentLevel, potentialTrapPositions.Count);

        // Place the traps
        for (int i = 0; i < numberOfTraps; i++)
        {
            Vector2Int trapPosition = potentialTrapPositions[i];
            Vector3 worldPosition = GridToWorldPosition(trapPosition);
            GameObject trapSquare = Instantiate(spikeSquarePrefab, worldPosition, Quaternion.identity, levelParent.transform);
            // Update the squareMap with the trap square
            squareMap[trapPosition] = trapSquare;
        }
    }

    private List<Vector2Int> Shuffle(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Vector2Int temp = list[i];
            int randomIndex = UnityEngine.Random.Range(0, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }

        return list;
    }

    private bool[,] GenerateMazeUsingRecursiveBacktracking(int width, int height, Vector2Int startPoint)
    {
        // Initialize the maze filled with walls
        bool[,] maze = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                maze[x, y] = false;
            }
        }

        // Use the provided startPoint
        maze[startPoint.x, startPoint.y] = true;

        // Recursive backtracking algorithm
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(startPoint);

        // Use recursive backtracking to generate the maze
        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(current, maze, 2);

            if (unvisitedNeighbors.Count > 0)
            {
                Vector2Int chosenNeighbor = unvisitedNeighbors[UnityEngine.Random.Range(0, unvisitedNeighbors.Count)];
                maze[chosenNeighbor.x, chosenNeighbor.y] = true;

                Vector2Int middle = current + (chosenNeighbor - current) / 2;
                maze[middle.x, middle.y] = true;

                stack.Push(chosenNeighbor);
            }
            else
            {
                stack.Pop();
            }
        }

        return maze;
    }

    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell, bool[,] maze, int distance)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        List<Vector2Int> possibleNeighbors = new List<Vector2Int>
        {
            new Vector2Int(cell.x - distance, cell.y),
            new Vector2Int(cell.x + distance, cell.y),
            new Vector2Int(cell.x, cell.y - distance),
            new Vector2Int(cell.x, cell.y + distance)
        };

        foreach (Vector2Int possibleNeighbor in possibleNeighbors)
        {
            if (possibleNeighbor.x >= 0 && possibleNeighbor.x < maze.GetLength(0) &&
                possibleNeighbor.y >= 0 && possibleNeighbor.y < maze.GetLength(1) &&
                !maze[possibleNeighbor.x, possibleNeighbor.y])
            {
                result.Add(possibleNeighbor);
            }
        }

        return result;
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
            Vector3 startPosition = square.transform.position + new Vector3(0f, -1f, 0f);
            Vector3 targetPosition = new Vector3(startPosition.x, gridYPosition, startPosition.z);
            Color startColor = new Color(1f, 1f, 1f, 0f);
            Color targetColor = new Color(1f, 1f, 1f, 1f);
            float elapsedTime = 0;

            while (elapsedTime < duration)
            {
                square.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
                square.GetComponent<Renderer>().material.color = Color.Lerp(startColor, targetColor, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            square.transform.position = targetPosition;
            square.GetComponent<Renderer>().material.color = targetColor;
        }
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
                winningSquarePosition = potentialGreenSquarePosition;
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
        Color targetColor = GetLevelTextColor(newLevel);

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
}
