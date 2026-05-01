using System.Collections.Generic;
using UnityEngine;

public class JellyBlock : MonoBehaviour
{
    public enum BlockColor
    {
        Red,
        Blue,
        Green,
        Yellow
    }

    public enum BlockShape
    {
        Single,
        Vertical2,
        Horizontal2,
        Square2,
        LShape3
    }

    public BlockColor blockColor;
    public BlockShape blockShape;

    public int rotationIndex = 0; // 0,1,2,3

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
        ApplyColorVisual();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isStopped) return;

        bool hitGround = collision.gameObject.CompareTag("Ground");
        bool hitBlock = collision.gameObject.CompareTag("Block");

        if (hitGround || hitBlock)
        {
            StopBlock();
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

        Color colorValue = Color.red;

        switch (blockColor)
        {
            case BlockColor.Red:
                colorValue = Color.red;
                break;
            case BlockColor.Blue:
                colorValue = Color.blue;
                break;
            case BlockColor.Green:
                colorValue = Color.green;
                break;
            case BlockColor.Yellow:
                colorValue = Color.yellow;
                break;
        }

        foreach (Transform piece in pieces)
        {
            SpriteRenderer pieceRenderer = piece.GetComponent<SpriteRenderer>();
            if (pieceRenderer != null)
            {
                pieceRenderer.color = colorValue;
            }
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

    void StopBlock()
    {
        if (isStopped) return;
        isStopped = true;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = false;

        DisablePieceColliders();

        List<Vector2Int> finalCells = GetCellsFromCurrentWorldPosition();

        foreach (Vector2Int finalCell in finalCells)
        {
            GameObject placedPiece = CreatePlacedPiece(finalCell);
            board.SetBlock(finalCell.x, finalCell.y, placedPiece);
        }

        board.ApplyGravity();
        board.ResolveBoard();

        if (board.CheckGameOver())
        {
            GameManager.Instance.GameOver();
        }

        Destroy(gameObject);
    }

    List<Vector2Int> GetCellsFromCurrentWorldPosition()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        foreach (Transform piece in pieces)
        {
            Vector2Int gridPos = board.GetGridPosition(piece.position);
            gridPos.x = Mathf.Clamp(gridPos.x, 0, board.width - 1);
            gridPos.y = Mathf.Clamp(gridPos.y, 0, board.height - 1);
            cells.Add(gridPos);
        }

        ResolveOverlapUpward(cells);
        DropCellsDown(cells);

        return cells;
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

    GameObject CreatePlacedPiece(Vector2Int gridCell)
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
        pieceData.blockColor = blockColor;
        pieceData.gridX = gridCell.x;
        pieceData.gridY = gridCell.y;

        switch (blockColor)
        {
            case BlockColor.Red:
                placedRenderer.color = Color.red;
                break;
            case BlockColor.Blue:
                placedRenderer.color = Color.blue;
                break;
            case BlockColor.Green:
                placedRenderer.color = Color.green;
                break;
            case BlockColor.Yellow:
                placedRenderer.color = Color.yellow;
                break;
        }

        return placed;
    }
}