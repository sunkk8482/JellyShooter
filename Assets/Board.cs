using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int width = 6;
    public int height = 12;
    public float cellSize = 1f;

    public Transform groundTransform;
    public int gameOverY = 10;

    private Vector2 origin;
    private GameObject[,] grid;

    void Awake()
    {
        grid = new GameObject[width, height];
    }

    void Start()
    {
        SetupOriginFromGround();
    }

    void SetupOriginFromGround()
    {
        if (groundTransform == null)
        {
            Debug.LogError("Board: groundTransform이 연결되지 않았음");
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

    public Vector2 GetWorldPosition(int x, int y)
    {
        return origin + new Vector2(x * cellSize, y * cellSize);
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
        if (IsInside(x, y))
            grid[x, y] = block;
    }

    public GameObject GetBlock(int x, int y)
    {
        if (!IsInside(x, y)) return null;
        return grid[x, y];
    }

    public void RemoveBlock(int x, int y)
    {
        if (IsInside(x, y))
            grid[x, y] = null;
    }

    public void ApplyGravity()
    {
        bool moved;

        do
        {
            moved = false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 1; y < height; y++)
                {
                    if (grid[x, y] != null && grid[x, y - 1] == null)
                    {
                        GameObject block = grid[x, y];
                        grid[x, y] = null;
                        grid[x, y - 1] = block;

                        block.transform.position = GetWorldPosition(x, y - 1);

                        JellyPlacedPiece piece = block.GetComponent<JellyPlacedPiece>();
                        if (piece != null)
                        {
                            piece.SetGridPosition(x, y - 1);
                        }

                        moved = true;
                    }
                }
            }
        }
        while (moved);
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

        JellyPlacedPiece startBlock = grid[startX, startY].GetComponent<JellyPlacedPiece>();
        if (startBlock == null) return connected;

        JellyBlock.BlockColor targetColor = startBlock.blockColor;

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

                JellyPlacedPiece next = grid[nx, ny].GetComponent<JellyPlacedPiece>();
                if (next == null) continue;
                if (next.blockColor != targetColor) continue;

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

        JellyPlacedPiece startBlock = grid[startX, startY].GetComponent<JellyPlacedPiece>();
        if (startBlock == null) return connected;

        JellyBlock.BlockColor targetColor = startBlock.blockColor;

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

                JellyPlacedPiece next = grid[nx, ny].GetComponent<JellyPlacedPiece>();
                if (next == null) continue;
                if (next.blockColor != targetColor) continue;

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

                JellyPlacedPiece block = grid[x, y].GetComponent<JellyPlacedPiece>();
                if (block == null) continue;

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
        int chainCount = 0;

        while (true)
        {
            List<List<Vector2Int>> allMatches = FindAllMatches();

            if (allMatches.Count == 0)
                break;

            chainCount++;
            int removedCount = 0;

            foreach (List<Vector2Int> matchGroup in allMatches)
            {
                foreach (Vector2Int pos in matchGroup)
                {
                    if (grid[pos.x, pos.y] != null)
                    {
                        GameObject block = grid[pos.x, pos.y];
                        grid[pos.x, pos.y] = null;
                        Destroy(block);
                        removedCount++;
                    }
                }
            }

            if (removedCount > 0 && GameManager.Instance != null)
            {
                int scoreToAdd = removedCount * 10 * chainCount;
                GameManager.Instance.AddScore(scoreToAdd);
            }

            ApplyGravity();
        }
    }
}