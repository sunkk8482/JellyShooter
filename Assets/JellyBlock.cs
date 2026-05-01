using System.Collections.Generic;
using UnityEngine;

public class JellyBlock : MonoBehaviour
{
    public enum BlockColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple
    }

    public enum BlockShape
    {
        Single,
        Vertical2,
        Horizontal2,
        Square2,
        LShape3,
        Vertical3,
        Horizontal3,
        LShape4,
        JShape4,
        TShape4
    }

    public BlockColor blockColor;
    public BlockShape blockShape;

    public int rotationIndex = 0; // 0,1,2,3
    public List<BlockColor> pieceColors = new List<BlockColor>();

    private Rigidbody2D rb;
    private bool isStopped = false;
    private Board board;
    private SpriteRenderer sr;

    private readonly List<Transform> pieces = new List<Transform>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        board = FindObjectOfType<Board>();
        sr = GetComponent<SpriteRenderer>();

        BuildShape();
        EnsurePieceColors();
        ApplyColorVisual();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isStopped) return;

        bool hitGround = collision.gameObject.CompareTag("Ground");
        bool hitBlock = collision.gameObject.CompareTag("Block");
        bool hitWall = IsWallCollision(collision);

        if (hitGround || hitBlock || hitWall)
        {
            StopBlock(collision);
        }
    }

    public void SetPreviewMode(bool isPreview)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (isPreview)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
        }

        foreach (Transform piece in pieces)
        {
            Collider2D col = piece.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = !isPreview;
            }
        }
    }

    public void RotateClockwise()
    {
        if (blockShape == BlockShape.Single || blockShape == BlockShape.Square2)
            return;

        rotationIndex = (rotationIndex + 1) % 4;
        RefreshShapeVisual();
    }

    void RefreshShapeVisual()
    {
        foreach (Transform piece in pieces)
        {
            if (piece != null)
                Destroy(piece.gameObject);
        }

        pieces.Clear();
        BuildShape();
        ApplyColorVisual();
    }

    public void ApplyColorVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (sr != null)
            sr.enabled = false;

        EnsurePieceColors();

        for (int i = 0; i < pieces.Count; i++)
        {
            SpriteRenderer pieceRenderer = pieces[i].GetComponent<SpriteRenderer>();
            if (pieceRenderer != null)
            {
                pieceRenderer.color = GetColorValue(GetPieceColor(i));
            }
        }
    }

    public void RandomizePieceColors()
    {
        RandomizePieceColors(System.Enum.GetValues(typeof(BlockColor)).Length);
    }

    public void RandomizePieceColors(int colorCount)
    {
        pieceColors.Clear();

        int availableColorCount = System.Enum.GetValues(typeof(BlockColor)).Length;
        int clampedColorCount = Mathf.Clamp(colorCount, 1, availableColorCount);
        int count = GetShapeOffsets().Count;
        for (int i = 0; i < count; i++)
        {
            pieceColors.Add((BlockColor)Random.Range(0, clampedColorCount));
        }

        if (pieceColors.Count > 0)
            blockColor = pieceColors[0];
    }

    void EnsurePieceColors()
    {
        int count = GetShapeOffsets().Count;

        while (pieceColors.Count < count)
        {
            pieceColors.Add(blockColor);
        }

        if (pieceColors.Count > count)
        {
            pieceColors.RemoveRange(count, pieceColors.Count - count);
        }

        if (pieceColors.Count > 0)
            blockColor = pieceColors[0];
    }

    BlockColor GetPieceColor(int index)
    {
        EnsurePieceColors();

        if (index < 0 || index >= pieceColors.Count)
            return blockColor;

        return pieceColors[index];
    }

    Color GetColorValue(BlockColor color)
    {
        switch (color)
        {
            case BlockColor.Red:
                return Color.red;
            case BlockColor.Blue:
                return Color.blue;
            case BlockColor.Green:
                return Color.green;
            case BlockColor.Yellow:
                return Color.yellow;
            case BlockColor.Purple:
                return new Color(0.75f, 0.25f, 1f);
            default:
                return Color.white;
        }
    }

    public List<Vector2> GetShapeOffsets()
    {
        return GetRotatedOffsets(GetBaseShapeOffsets(blockShape), rotationIndex);
    }

    public List<Vector2Int> PredictLandingCellsFromAnchor(Vector2 anchorWorldPos)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        foreach (Vector2 offset in GetShapeOffsets())
        {
            Vector2 worldPos = anchorWorldPos + offset;
            Vector2Int gridPos = board.GetGridPosition(worldPos);
            gridPos.x = Mathf.Clamp(gridPos.x, 0, board.width - 1);
            gridPos.y = Mathf.Clamp(gridPos.y, 0, board.height - 1);
            cells.Add(gridPos);
        }

        ResolveOverlapUpward(cells);
        DropCellsDown(cells);

        return cells;
    }

    void BuildShape()
    {
        if (pieces.Count > 0) return;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = false;

        BoxCollider2D parentCollider = GetComponent<BoxCollider2D>();
        if (parentCollider != null)
        {
            parentCollider.enabled = false;
        }

        List<Vector2> offsets = GetShapeOffsets();

        foreach (Vector2 offset in offsets)
        {
            GameObject piece = new GameObject("Piece");
            piece.transform.SetParent(transform);
            piece.transform.localPosition = offset;

            SpriteRenderer pieceRenderer = piece.AddComponent<SpriteRenderer>();
            if (sr != null)
            {
                pieceRenderer.sprite = sr.sprite;
            }
            pieceRenderer.sortingOrder = 2;

            BoxCollider2D pieceCollider = piece.AddComponent<BoxCollider2D>();
            pieceCollider.isTrigger = false;

            pieces.Add(piece.transform);
        }
    }

    List<Vector2> GetBaseShapeOffsets(BlockShape shape)
    {
        List<Vector2> offsets = new List<Vector2>();

        switch (shape)
        {
            case BlockShape.Single:
                offsets.Add(new Vector2(0f, 0f));
                break;

            case BlockShape.Vertical2:
                offsets.Add(new Vector2(0f, 0f));
                offsets.Add(new Vector2(0f, 1f));
                break;

            case BlockShape.Horizontal2:
                offsets.Add(new Vector2(0f, 0f));
                offsets.Add(new Vector2(1f, 0f));
                break;

            case BlockShape.Square2:
                offsets.Add(new Vector2(0f, 0f));
                offsets.Add(new Vector2(1f, 0f));
                offsets.Add(new Vector2(0f, 1f));
                offsets.Add(new Vector2(1f, 1f));
                break;

            case BlockShape.LShape3:
                offsets.Add(new Vector2(0f, 0f));
                offsets.Add(new Vector2(0f, 1f));
                offsets.Add(new Vector2(1f, 0f));
                break;

            case BlockShape.Vertical3:
                offsets.Add(new Vector2(0f, 0f));
                offsets.Add(new Vector2(0f, 1f));
                offsets.Add(new Vector2(0f, 2f));
                break;

            case BlockShape.Horizontal3:
                offsets.Add(new Vector2(0f, 0f));
                offsets.Add(new Vector2(1f, 0f));
                offsets.Add(new Vector2(2f, 0f));
                break;

            case BlockShape.LShape4:
                offsets.Add(new Vector2(0f, 0f));
                offsets.Add(new Vector2(0f, 1f));
                offsets.Add(new Vector2(0f, 2f));
                offsets.Add(new Vector2(1f, 0f));
                break;

            case BlockShape.JShape4:
                offsets.Add(new Vector2(1f, 0f));
                offsets.Add(new Vector2(1f, 1f));
                offsets.Add(new Vector2(1f, 2f));
                offsets.Add(new Vector2(0f, 0f));
                break;

            case BlockShape.TShape4:
                offsets.Add(new Vector2(0f, 0f));
                offsets.Add(new Vector2(1f, 0f));
                offsets.Add(new Vector2(2f, 0f));
                offsets.Add(new Vector2(1f, 1f));
                break;
        }

        return offsets;
    }

    List<Vector2> GetRotatedOffsets(List<Vector2> baseOffsets, int rot)
    {
        List<Vector2> rotated = new List<Vector2>();

        foreach (Vector2 p in baseOffsets)
        {
            Vector2 r = p;

            for (int i = 0; i < rot; i++)
            {
                r = new Vector2(r.y, -r.x);
            }

            rotated.Add(r);
        }

        NormalizeOffsets(rotated);
        return rotated;
    }

    void NormalizeOffsets(List<Vector2> offsets)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;

        foreach (Vector2 o in offsets)
        {
            if (o.x < minX) minX = o.x;
            if (o.y < minY) minY = o.y;
        }

        for (int i = 0; i < offsets.Count; i++)
        {
            offsets[i] = new Vector2(offsets[i].x - minX, offsets[i].y - minY);
        }
    }

    void DisablePieceColliders()
    {
        foreach (Transform piece in pieces)
        {
            Collider2D col = piece.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    bool IsWallCollision(Collision2D collision)
    {
        if (collision == null || collision.gameObject == null)
            return false;

        if (collision.gameObject.name.Contains("Wall"))
            return true;

        Transform parent = collision.transform.parent;
        return parent != null && parent.name == "BoardWalls";
    }

    public void PlaceAtCells(List<Vector2Int> finalCells)
    {
        if (isStopped) return;
        isStopped = true;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        DisablePieceColliders();

        if (finalCells != null)
        {
            for (int i = 0; i < finalCells.Count; i++)
            {
                Vector2Int finalCell = finalCells[i];
                if (!board.IsInside(finalCell.x, finalCell.y)) continue;

                GameObject placedPiece = CreatePlacedPiece(finalCell, GetPieceColor(i));
                board.SetBlock(finalCell.x, finalCell.y, placedPiece);
            }
        }

        board.ResolveBoard();
        Destroy(gameObject);
    }

    void StopBlock(Collision2D collision)
    {
        if (isStopped) return;
        isStopped = true;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = false;

        DisablePieceColliders();

        List<Vector2Int> finalCells = GetCellsFromCurrentWorldPosition(collision);

        for (int i = 0; i < finalCells.Count; i++)
        {
            Vector2Int finalCell = finalCells[i];
            GameObject placedPiece = CreatePlacedPiece(finalCell, GetPieceColor(i));
            board.SetBlock(finalCell.x, finalCell.y, placedPiece);
        }

        board.ResolveBoard();

        Destroy(gameObject);
    }

    List<Vector2Int> GetCellsFromCurrentWorldPosition(Collision2D collision)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        foreach (Transform piece in pieces)
        {
            Vector2Int gridPos = board.GetGridPosition(piece.position);
            gridPos.y = Mathf.Clamp(gridPos.y, 0, board.height - 1);
            cells.Add(gridPos);
        }

        ClampCellsInsideHorizontalBounds(cells);
        ResolveOverlapFromCollision(cells, collision);

        return cells;
    }

    void ResolveOverlapFromCollision(List<Vector2Int> cells, Collision2D collision)
    {
        Vector2Int pushDir = GetCollisionPushDirection(collision);

        if (AreCellsPlaceable(cells))
            return;

        for (int i = 0; i < board.width + board.height; i++)
        {
            MoveCells(cells, pushDir);

            if (AreCellsPlaceable(cells))
                return;
        }

        ResolveOverlapUpward(cells);
    }

    void ClampCellsInsideHorizontalBounds(List<Vector2Int> cells)
    {
        int minX = int.MaxValue;
        int maxX = int.MinValue;

        foreach (Vector2Int cell in cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
        }

        int shiftX = 0;
        if (minX < 0)
            shiftX = -minX;
        else if (maxX >= board.width)
            shiftX = board.width - 1 - maxX;

        if (shiftX == 0) return;

        for (int i = 0; i < cells.Count; i++)
        {
            cells[i] = new Vector2Int(cells[i].x + shiftX, cells[i].y);
        }
    }

    Vector2Int GetCollisionPushDirection(Collision2D collision)
    {
        if (collision == null || collision.contactCount == 0)
            return Vector2Int.up;

        Vector2 normal = collision.GetContact(0).normal;

        if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
        {
            return normal.x > 0f ? Vector2Int.right : Vector2Int.left;
        }

        return normal.y > 0f ? Vector2Int.up : Vector2Int.down;
    }

    bool AreCellsPlaceable(List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];

            if (!board.IsInside(cell.x, cell.y))
                return false;

            if (!board.IsEmpty(cell.x, cell.y))
                return false;

            for (int j = i + 1; j < cells.Count; j++)
            {
                if (cells[j] == cell)
                    return false;
            }
        }

        return true;
    }

    void MoveCells(List<Vector2Int> cells, Vector2Int direction)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            int x = Mathf.Clamp(cells[i].x + direction.x, 0, board.width - 1);
            int y = Mathf.Clamp(cells[i].y + direction.y, 0, board.height - 1);
            cells[i] = new Vector2Int(x, y);
        }
    }

    void ResolveOverlapUpward(List<Vector2Int> cells)
    {
        bool hasOverlap = true;
        int safeCount = 0;

        while (hasOverlap && safeCount < board.height)
        {
            hasOverlap = false;

            foreach (Vector2Int cell in cells)
            {
                if (!board.IsEmpty(cell.x, cell.y))
                {
                    hasOverlap = true;
                    break;
                }
            }

            if (hasOverlap)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    int newY = Mathf.Clamp(cells[i].y + 1, 0, board.height - 1);
                    cells[i] = new Vector2Int(cells[i].x, newY);
                }
            }

            safeCount++;
        }
    }

    void DropCellsDown(List<Vector2Int> cells)
    {
        bool canMoveDown = true;
        int safeCount = 0;

        while (canMoveDown && safeCount < board.height)
        {
            foreach (Vector2Int currentCell in cells)
            {
                int belowY = currentCell.y - 1;

                if (belowY < 0)
                {
                    canMoveDown = false;
                    break;
                }

                if (ContainsCell(cells, currentCell.x, belowY))
                {
                    continue;
                }

                if (!board.IsEmpty(currentCell.x, belowY))
                {
                    canMoveDown = false;
                    break;
                }
            }

            if (canMoveDown)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    cells[i] = new Vector2Int(cells[i].x, cells[i].y - 1);
                }
            }

            safeCount++;
        }
    }

    bool ContainsCell(List<Vector2Int> cells, int x, int y)
    {
        foreach (Vector2Int checkCell in cells)
        {
            if (checkCell.x == x && checkCell.y == y)
            {
                return true;
            }
        }

        return false;
    }

    GameObject CreatePlacedPiece(Vector2Int gridCell, BlockColor pieceColor)
    {
        GameObject placed = new GameObject("PlacedPiece");
        placed.tag = "Block";
        placed.transform.position = board.GetWorldPosition(gridCell.x, gridCell.y);

        SpriteRenderer placedRenderer = placed.AddComponent<SpriteRenderer>();
        if (sr != null)
        {
            placedRenderer.sprite = sr.sprite;
        }
        placedRenderer.sortingOrder = 2;

        placed.AddComponent<BoxCollider2D>();

        Rigidbody2D placedRb = placed.AddComponent<Rigidbody2D>();
        placedRb.bodyType = RigidbodyType2D.Kinematic;
        placedRb.simulated = false;

        JellyPlacedPiece pieceData = placed.AddComponent<JellyPlacedPiece>();
        pieceData.blockColor = pieceColor;
        pieceData.gridX = gridCell.x;
        pieceData.gridY = gridCell.y;

        placedRenderer.color = GetColorValue(pieceColor);

        return placed;
    }
}
