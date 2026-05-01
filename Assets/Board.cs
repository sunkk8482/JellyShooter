using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int width = 8;
    public int height = 12;
    public float cellSize = 1f;

    public Transform groundTransform;
    public int gameOverY = 10;
    public bool IsResolving { get; private set; } = false;
    public float removeAnimationDuration = 0.15f;
    public float fallAnimationDuration = 0.18f;
    public bool showBoardGrid = true;
    public bool showInternalGrid = false;
    public Color boardBorderColor = new Color(1f, 1f, 1f, 0.9f);
    public Color boardGridColor = new Color(1f, 1f, 1f, 0.08f);
    public Color gameOverLineColor = new Color(1f, 0.28f, 0f, 0.65f);
    public Color landingHighlightColor = new Color(1f, 1f, 1f, 0.18f);
    public float boardBorderLineWidth = 0.05f;
    public float boardGridLineWidth = 0.025f;
    public float gameOverLineWidth = 0.08f;
    public bool createSideWalls = true;
    public float sideWallThickness = 0.5f;
    public bool fitGroundToBoard = true;
    public float groundExtraWidth = 2f;
    public bool fitCameraToBoard = true;
    public float cameraVerticalPadding = 1.5f;
    public float cameraHorizontalPadding = 3f;

    private Vector2 origin;
    private GameObject[,] grid;
    private Transform boardGridRoot;
    private Transform boardWallRoot;
    private Transform gameOverLineRoot;
    private Transform landingHighlightRoot;
    private Sprite gridLineSprite;

    void Awake()
    {
        width = Mathf.Max(width, 8);
        grid = new GameObject[width, height];
        SetupOriginFromGround();
    }

    void Start()
    {
        FitGroundToBoard();
        CreateBoardGridVisual();
        CreateGameOverLineVisual();
        CreateSideWalls();
        FitCameraToBoard();
    }

    void SetupOriginFromGround()
    {
        if (groundTransform == null)
        {
            Debug.LogError("Board: groundTransform is not assigned");
            origin = new Vector2(-2.5f, -3f);
            return;
        }

        SpriteRenderer sr = groundTransform.GetComponent<SpriteRenderer>();

        float groundTopY;
        if (sr != null)
            groundTopY = sr.bounds.max.y;
        else
            groundTopY = groundTransform.position.y + 0.5f;

        float leftX = -(width / 2f) * cellSize + (cellSize / 2f);
        float firstCellCenterY = groundTopY + (cellSize / 2f);

        origin = new Vector2(leftX, firstCellCenterY);

        Debug.Log($"Board origin set to: {origin}");
    }

    void CreateBoardGridVisual()
    {
        if (!showBoardGrid) return;

        if (boardGridRoot != null)
            Destroy(boardGridRoot.gameObject);

        GameObject root = new GameObject("BoardGrid");
        root.transform.SetParent(transform);
        boardGridRoot = root.transform;

        float left = origin.x - cellSize * 0.5f;
        float right = origin.x + (width - 0.5f) * cellSize;
        float bottom = origin.y - cellSize * 0.5f;
        float top = origin.y + (height - 0.5f) * cellSize;

        for (int x = 0; x <= width; x++)
        {
            float lineX = left + x * cellSize;
            bool isBorder = x == 0 || x == width;
            if (!isBorder && !showInternalGrid) continue;

            CreateGridLine(lineX, (bottom + top) * 0.5f, isBorder ? boardBorderLineWidth : boardGridLineWidth, top - bottom, isBorder);
        }

        for (int y = 0; y <= height; y++)
        {
            float lineY = bottom + y * cellSize;
            bool isBorder = y == 0 || y == height;
            if (!isBorder && !showInternalGrid) continue;

            CreateGridLine((left + right) * 0.5f, lineY, right - left, isBorder ? boardBorderLineWidth : boardGridLineWidth, isBorder);
        }
    }

    void CreateGridLine(float centerX, float centerY, float lineWidth, float lineHeight, bool isBorder)
    {
        GameObject lineObject = new GameObject(isBorder ? "BoardBorderLine" : "BoardGridLine");
        lineObject.transform.SetParent(boardGridRoot);
        lineObject.transform.position = new Vector3(centerX, centerY, -0.1f);

        SpriteRenderer renderer = lineObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateWhiteSprite();
        renderer.color = isBorder ? boardBorderColor : boardGridColor;
        renderer.sortingOrder = isBorder ? 5 : 4;

        lineObject.transform.localScale = new Vector3(lineWidth, lineHeight, 1f);
    }

    void CreateGameOverLineVisual()
    {
        if (gameOverLineRoot != null)
            Destroy(gameOverLineRoot.gameObject);

        GameObject root = new GameObject("GameOverLine");
        root.transform.SetParent(transform);
        gameOverLineRoot = root.transform;

        int clampedGameOverY = Mathf.Clamp(gameOverY, 0, height);
        float left = origin.x - cellSize * 0.5f;
        float right = origin.x + (width - 0.5f) * cellSize;
        float lineY = origin.y + (clampedGameOverY - 0.5f) * cellSize;

        GameObject lineObject = new GameObject("GameOverThresholdLine");
        lineObject.transform.SetParent(gameOverLineRoot);
        lineObject.transform.position = new Vector3((left + right) * 0.5f, lineY, -0.2f);
        lineObject.transform.localScale = new Vector3(right - left, gameOverLineWidth, 1f);

        SpriteRenderer renderer = lineObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateWhiteSprite();
        renderer.color = gameOverLineColor;
        renderer.sortingOrder = 20;
    }

    Sprite CreateWhiteSprite()
    {
        if (gridLineSprite != null)
            return gridLineSprite;

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        gridLineSprite = Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        return gridLineSprite;
    }

    void CreateSideWalls()
    {
        if (!createSideWalls) return;

        if (boardWallRoot != null)
            Destroy(boardWallRoot.gameObject);

        GameObject root = new GameObject("BoardWalls");
        root.transform.SetParent(transform);
        boardWallRoot = root.transform;

        float left = origin.x - cellSize * 0.5f;
        float right = origin.x + (width - 0.5f) * cellSize;
        float bottom = origin.y - cellSize * 0.5f;
        float boardHeight = height * cellSize;
        float centerY = bottom + boardHeight * 0.5f;

        CreateSideWall("LeftWall", left - sideWallThickness * 0.5f, centerY, boardHeight);
        CreateSideWall("RightWall", right + sideWallThickness * 0.5f, centerY, boardHeight);
    }

    void CreateSideWall(string wallName, float x, float centerY, float wallHeight)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(boardWallRoot);
        wall.transform.position = new Vector3(x, centerY, 0f);

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(sideWallThickness, wallHeight);
    }

    void FitGroundToBoard()
    {
        if (!fitGroundToBoard || groundTransform == null) return;

        Vector3 scale = groundTransform.localScale;
        scale.x = Mathf.Max(scale.x, width * cellSize + groundExtraWidth);
        groundTransform.localScale = scale;
    }

    public Vector2 GetWorldPosition(int x, int y)
    {
        return origin + new Vector2(x * cellSize, y * cellSize);
    }

    public Vector2 GetBoardBottomCenter(float yOffset)
    {
        float left = origin.x - cellSize * 0.5f;
        float right = origin.x + (width - 0.5f) * cellSize;
        float bottom = origin.y - cellSize * 0.5f;
        return new Vector2((left + right) * 0.5f, bottom + yOffset);
    }

    public Vector2 GetBoardTopCenter(float yOffset)
    {
        float left = origin.x - cellSize * 0.5f;
        float right = origin.x + (width - 0.5f) * cellSize;
        float top = origin.y + (height - 0.5f) * cellSize;
        return new Vector2((left + right) * 0.5f, top + yOffset);
    }

    public Vector2 GetBoardRightCenter(float xOffset)
    {
        float right = origin.x + (width - 0.5f) * cellSize;
        float bottom = origin.y - cellSize * 0.5f;
        float top = origin.y + (height - 0.5f) * cellSize;
        return new Vector2(right + xOffset, (bottom + top) * 0.5f);
    }

    void FitCameraToBoard()
    {
        if (!fitCameraToBoard || Camera.main == null) return;

        float left = origin.x - cellSize * 0.5f;
        float right = origin.x + (width - 0.5f) * cellSize;
        float bottom = origin.y - cellSize * 0.5f;
        float top = origin.y + (height - 0.5f) * cellSize;

        Vector3 cameraPosition = Camera.main.transform.position;
        cameraPosition.x = (left + right) * 0.5f;
        cameraPosition.y = (bottom + top) * 0.5f;
        Camera.main.transform.position = cameraPosition;

        float verticalSize = (top - bottom) * 0.5f + cameraVerticalPadding;
        float horizontalSize = ((right - left) + cameraHorizontalPadding) * 0.5f / Camera.main.aspect;
        Camera.main.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }

    public Vector2Int GetGridPosition(Vector2 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - origin.x) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - origin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsEmpty(int x, int y)
    {
        if (!IsInside(x, y)) return false;
        return grid[x, y] == null;
    }

    public void SetBlock(int x, int y, GameObject block)
    {
        if (!IsInside(x, y)) return;

        grid[x, y] = block;

        if (block != null)
            SnapBlockToCell(block, x, y);
    }

    public GameObject GetBlock(int x, int y)
    {
        if (!IsInside(x, y)) return null;
        return grid[x, y];
    }

    public void ShowLandingHighlight(List<Vector2Int> cells)
    {
        ClearLandingHighlight();

        if (cells == null || cells.Count == 0) return;

        GameObject root = new GameObject("LandingHighlight");
        root.transform.SetParent(transform);
        landingHighlightRoot = root.transform;

        foreach (Vector2Int cell in cells)
        {
            if (!IsInside(cell.x, cell.y)) continue;

            GameObject highlight = new GameObject("LandingCellHighlight");
            highlight.transform.SetParent(landingHighlightRoot);
            highlight.transform.position = new Vector3(GetWorldPosition(cell.x, cell.y).x, GetWorldPosition(cell.x, cell.y).y, -0.2f);
            highlight.transform.localScale = new Vector3(cellSize * 0.88f, cellSize * 0.88f, 1f);

            SpriteRenderer renderer = highlight.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateWhiteSprite();
            renderer.color = landingHighlightColor;
            renderer.sortingOrder = 1;
        }
    }

    public void ClearLandingHighlight()
    {
        if (landingHighlightRoot != null)
        {
            Destroy(landingHighlightRoot.gameObject);
            landingHighlightRoot = null;
        }
    }

    public void RemoveBlock(int x, int y)
    {
        if (IsInside(x, y))
            grid[x, y] = null;
    }

    public void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            int writeY = 0;

            for (int readY = 0; readY < height; readY++)
            {
                GameObject block = grid[x, readY];
                if (block == null) continue;

                if (writeY != readY)
                {
                    grid[x, writeY] = block;
                    grid[x, readY] = null;
                }

                SnapBlockToCell(block, x, writeY);
                writeY++;
            }

            for (int y = writeY; y < height; y++)
            {
                grid[x, y] = null;
            }
        }
    }

    public bool CheckGameOver()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = gameOverY; y < height; y++)
            {
                if (grid[x, y] != null)
                    return true;
            }
        }

        return false;
    }

    public List<Vector2Int> FindConnectedBlocks(int startX, int startY)
    {
        List<Vector2Int> connected = new List<Vector2Int>();

        if (!IsInside(startX, startY)) return connected;
        if (grid[startX, startY] == null) return connected;

        if (!TryGetBlockColor(startX, startY, out JellyBlock.BlockColor targetColor))
            return connected;

        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        Vector2Int[] dirs =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            connected.Add(cur);

            foreach (var dir in dirs)
            {
                int nx = cur.x + dir.x;
                int ny = cur.y + dir.y;

                if (!IsInside(nx, ny)) continue;
                if (visited[nx, ny]) continue;
                if (grid[nx, ny] == null) continue;

                if (!TryGetBlockColor(nx, ny, out JellyBlock.BlockColor nextColor)) continue;
                if (nextColor != targetColor) continue;

                visited[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        return connected;
    }

    private List<Vector2Int> FindConnectedBlocksFromVisited(int startX, int startY, bool[,] visited)
    {
        List<Vector2Int> connected = new List<Vector2Int>();

        if (!IsInside(startX, startY)) return connected;
        if (grid[startX, startY] == null) return connected;

        if (!TryGetBlockColor(startX, startY, out JellyBlock.BlockColor targetColor))
            return connected;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        Vector2Int[] dirs =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            connected.Add(cur);

            foreach (var dir in dirs)
            {
                int nx = cur.x + dir.x;
                int ny = cur.y + dir.y;

                if (!IsInside(nx, ny)) continue;
                if (visited[nx, ny]) continue;
                if (grid[nx, ny] == null) continue;

                if (!TryGetBlockColor(nx, ny, out JellyBlock.BlockColor nextColor)) continue;
                if (nextColor != targetColor) continue;

                visited[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        return connected;
    }

    public List<List<Vector2Int>> FindAllMatches()
    {
        List<List<Vector2Int>> allMatches = new List<List<Vector2Int>>();
        bool[,] visited = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (visited[x, y]) continue;
                if (grid[x, y] == null) continue;

                if (!TryGetBlockColor(x, y, out _)) continue;

                List<Vector2Int> group = FindConnectedBlocksFromVisited(x, y, visited);

                if (group.Count >= 3)
                {
                    allMatches.Add(group);
                }
            }
        }

        return allMatches;
    }

    public void ResolveBoard()
    {
        if (IsResolving) return;

        StartCoroutine(ResolveBoardRoutine());
    }

    private IEnumerator ResolveBoardRoutine()
    {
        IsResolving = true;
        int chainCount = 0;

        yield return ApplyGravityAnimated();

        while (true)
        {
            List<List<Vector2Int>> allMatches = FindAllMatches();

            if (allMatches.Count == 0)
                break;

            chainCount++;
            HashSet<Vector2Int> cellsToRemove = new HashSet<Vector2Int>();

            foreach (List<Vector2Int> matchGroup in allMatches)
            {
                foreach (Vector2Int pos in matchGroup)
                {
                    if (IsInside(pos.x, pos.y))
                        cellsToRemove.Add(pos);
                }
            }

            int removedCount = 0;
            List<GameObject> blocksToRemove = new List<GameObject>();

            foreach (Vector2Int pos in cellsToRemove)
            {
                GameObject block = grid[pos.x, pos.y];
                if (block == null) continue;

                blocksToRemove.Add(block);
                removedCount++;
            }

            if (removedCount == 0)
                break;

            yield return AnimateRemoveBlocks(blocksToRemove);

            foreach (Vector2Int pos in cellsToRemove)
            {
                if (IsInside(pos.x, pos.y))
                    grid[pos.x, pos.y] = null;
            }

            foreach (GameObject block in blocksToRemove)
            {
                if (block != null)
                    Destroy(block);
            }

            if (removedCount > 0 && GameManager.Instance != null)
            {
                int scoreToAdd = removedCount * 10 * chainCount;
                GameManager.Instance.AddScore(scoreToAdd);
            }

            yield return ApplyGravityAnimated();
        }

        if (CheckGameOver() && GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }

        IsResolving = false;
    }

    private IEnumerator AnimateRemoveBlocks(List<GameObject> blocks)
    {
        List<Vector3> startScales = new List<Vector3>();

        foreach (GameObject block in blocks)
        {
            if (block == null) continue;
            startScales.Add(block.transform.localScale);
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, removeAnimationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null)
                    blocks[i].transform.localScale = startScales[i] * scale;
            }

            yield return null;
        }
    }

    private IEnumerator ApplyGravityAnimated()
    {
        List<BlockMove> moves = new List<BlockMove>();
        bool hasMovingBlock = false;

        for (int x = 0; x < width; x++)
        {
            int writeY = 0;

            for (int readY = 0; readY < height; readY++)
            {
                GameObject block = grid[x, readY];
                if (block == null) continue;

                Vector3 startPosition = block.transform.position;

                if (writeY != readY)
                {
                    grid[x, writeY] = block;
                    grid[x, readY] = null;
                }

                SetBlockGridPosition(block, x, writeY);

                Vector3 endPosition = GetWorldPosition(x, writeY);
                if ((startPosition - endPosition).sqrMagnitude > 0.0001f)
                    hasMovingBlock = true;

                moves.Add(new BlockMove(block, startPosition, endPosition, x, writeY));
                writeY++;
            }

            for (int y = writeY; y < height; y++)
            {
                grid[x, y] = null;
            }
        }

        if (moves.Count == 0)
            yield break;

        if (!hasMovingBlock)
        {
            foreach (BlockMove move in moves)
            {
                if (move.block != null)
                    SnapBlockToCell(move.block, move.x, move.y);
            }

            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fallAnimationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            foreach (BlockMove move in moves)
            {
                if (move.block != null)
                    move.block.transform.position = Vector3.Lerp(move.startPosition, move.endPosition, easedT);
            }

            yield return null;
        }

        foreach (BlockMove move in moves)
        {
            if (move.block != null)
                SnapBlockToCell(move.block, move.x, move.y);
        }
    }

    private bool TryGetBlockColor(int x, int y, out JellyBlock.BlockColor blockColor)
    {
        blockColor = default;

        if (!IsInside(x, y)) return false;
        if (grid[x, y] == null) return false;

        JellyPlacedPiece piece = grid[x, y].GetComponent<JellyPlacedPiece>();
        if (piece == null) return false;

        blockColor = piece.blockColor;
        return true;
    }

    private void SnapBlockToCell(GameObject block, int x, int y)
    {
        block.transform.position = GetWorldPosition(x, y);
        SetBlockGridPosition(block, x, y);
    }

    private void SetBlockGridPosition(GameObject block, int x, int y)
    {
        JellyPlacedPiece piece = block.GetComponent<JellyPlacedPiece>();
        if (piece != null)
        {
            piece.SetGridPosition(x, y);
        }
    }

    private struct BlockMove
    {
        public GameObject block;
        public Vector3 startPosition;
        public Vector3 endPosition;
        public int x;
        public int y;

        public BlockMove(GameObject block, Vector3 startPosition, Vector3 endPosition, int x, int y)
        {
            this.block = block;
            this.startPosition = startPosition;
            this.endPosition = endPosition;
            this.x = x;
            this.y = y;
        }
    }
}
