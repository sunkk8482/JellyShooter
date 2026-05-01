using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    public GameObject blockPrefab;
    public Transform nextPreviewPoint;

    public float shootPower = 4.5f;
    public float maxDragDistance = 2f;
    public float touchRadius = 1f;
    public float maxPreviewDistance = 8f;
    public float shootMoveSpeed = 14f;
    public bool useDropPuzzleMode = true;
    public float dropMoveSpeed = 16f;
    public bool createMobileControls = true;
    public int spawnTopPaddingCells = 0;
    public int colorCount = 5;

    private bool isDragging = false;
    private bool isShooting = false;
    private bool isDropping = false;
    private int currentBaseX = 0;
    private Vector2 currentDrag;
    private LineRenderer lineRenderer;

    private GameObject currentBlock;
    private GameObject nextBlock;

    private GameObject ghostRoot;
    private Board board;

    public struct LandingResult
    {
        public List<Vector2Int> cells;
        public Vector2 direction;
        public float travelDistance;
        public Vector2 visualTargetWorldPosition;

        public LandingResult(List<Vector2Int> cells, Vector2 direction, float travelDistance, Vector2 visualTargetWorldPosition)
        {
            this.cells = cells;
            this.direction = direction;
            this.travelDistance = travelDistance;
            this.visualTargetWorldPosition = visualTargetWorldPosition;
        }
    }

    void Start()
    {
        board = FindObjectOfType<Board>();

        if (board != null)
        {
            transform.position = board.GetBoardTopCenter(0f);

            if (nextPreviewPoint != null)
                nextPreviewPoint.position = board.GetBoardRightCenter(1.8f);
        }

        SpriteRenderer launcherRenderer = GetComponent<SpriteRenderer>();
        if (launcherRenderer != null)
            launcherRenderer.enabled = false;

        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }

        PrepareBlocks();
        CreateGhostRoot();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        if (isShooting || isDropping || (board != null && board.IsResolving))
        {
            isDragging = false;

            if (lineRenderer != null)
                lineRenderer.enabled = false;

            if (ghostRoot != null)
                ghostRoot.SetActive(false);

            return;
        }

        if (useDropPuzzleMode)
        {
            UpdateDropPuzzleMode();
            HideNextBlockWorldPreview();
            return;
        }

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (currentBlock != null && !isDragging)
        {
            currentBlock.transform.position = transform.position + new Vector3(0f, 0.6f, 0f);
        }

        if (nextBlock != null && nextPreviewPoint != null)
        {
            nextBlock.transform.position = nextPreviewPoint.position;
        }

        // 회전 테스트: PC에서는 R 키
        if (!isDragging && currentBlock != null && Input.GetKeyDown(KeyCode.R))
        {
            JellyBlock jelly = currentBlock.GetComponent<JellyBlock>();
            jelly.RotateClockwise();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Vector2.Distance(mouseWorld, transform.position) <= touchRadius)
            {
                isDragging = true;

                if (lineRenderer != null)
                    lineRenderer.enabled = true;

                if (ghostRoot != null)
                    ghostRoot.SetActive(false);
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            currentDrag = mouseWorld;

            Vector2 dragVector = currentDrag - (Vector2)transform.position;

            if (dragVector.magnitude > maxDragDistance)
            {
                dragVector = dragVector.normalized * maxDragDistance;
                currentDrag = (Vector2)transform.position + dragVector;
            }

            Vector2 direction = (Vector2)transform.position - currentDrag;

            DrawAimLine(currentDrag);
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            Vector2 direction = (Vector2)transform.position - mouseWorld;

            if (direction.magnitude > maxDragDistance)
                direction = direction.normalized * maxDragDistance;

            ShootCurrentBlock(direction);

            isDragging = false;

            if (lineRenderer != null)
                lineRenderer.enabled = false;

            if (ghostRoot != null)
                ghostRoot.SetActive(false);
        }
    }

    void PrepareBlocks()
    {
        currentBaseX = board != null ? board.width / 2 : 0;
        currentBlock = CreatePreviewBlock(transform.position + new Vector3(0f, 0.6f, 0f));
        UpdateCurrentBlockDropPosition();

        if (nextPreviewPoint != null)
        {
            nextBlock = CreatePreviewBlock(nextPreviewPoint.position);
            HideNextBlockWorldPreview();
        }
    }

    GameObject CreatePreviewBlock(Vector3 pos)
    {
        GameObject block = Instantiate(blockPrefab, pos, Quaternion.identity);

        JellyBlock jelly = block.GetComponent<JellyBlock>();
        jelly.blockColor = (JellyBlock.BlockColor)Random.Range(0, GetCurrentColorCount());
        jelly.blockShape = (JellyBlock.BlockShape)Random.Range(0, System.Enum.GetValues(typeof(JellyBlock.BlockShape)).Length);
        jelly.rotationIndex = 0;
        jelly.RandomizePieceColors(GetCurrentColorCount());
        jelly.ApplyColorVisual();
        jelly.SetPreviewMode(true);

        return block;
    }

    int GetCurrentColorCount()
    {
        int availableColorCount = System.Enum.GetValues(typeof(JellyBlock.BlockColor)).Length;
        int requestedColorCount = colorCount <= 0 ? 5 : colorCount;
        return Mathf.Clamp(requestedColorCount, 1, availableColorCount);
    }

    void UpdateDropPuzzleMode()
    {
        if (currentBlock == null || board == null) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveBaseX(-1);
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveBaseX(1);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            DropCurrentBlock();
            return;
        }

        // Rotation test on PC: R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            JellyBlock jelly = currentBlock.GetComponent<JellyBlock>();
            jelly.RotateClockwise();
            ClampBaseXToShape(jelly);
            UpdateCurrentBlockDropPosition();
        }

        UpdateCurrentBlockDropPosition();
    }

    void MoveBaseX(int delta)
    {
        JellyBlock jelly = currentBlock.GetComponent<JellyBlock>();
        currentBaseX += delta;
        ClampBaseXToShape(jelly);
        UpdateCurrentBlockDropPosition();
    }

    public void MoveLeftFromButton()
    {
        if (!CanUseDropInput()) return;
        MoveBaseX(-1);
    }

    public void MoveRightFromButton()
    {
        if (!CanUseDropInput()) return;
        MoveBaseX(1);
    }

    bool CanUseDropInput()
    {
        if (!useDropPuzzleMode) return false;
        if (currentBlock == null || board == null) return false;
        if (isDropping || isShooting || board.IsResolving) return false;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return false;
        return true;
    }

    void ClampBaseXToShape(JellyBlock jelly)
    {
        if (jelly == null || board == null) return;

        List<Vector2Int> offsets = GetShapeCellOffsets(jelly);
        int minOffsetX = 0;
        int maxOffsetX = 0;

        foreach (Vector2Int offset in offsets)
        {
            if (offset.x < minOffsetX) minOffsetX = offset.x;
            if (offset.x > maxOffsetX) maxOffsetX = offset.x;
        }

        currentBaseX = Mathf.Clamp(currentBaseX, -minOffsetX, board.width - 1 - maxOffsetX);
    }

    void UpdateCurrentBlockDropPosition()
    {
        if (currentBlock == null || board == null) return;

        JellyBlock jelly = currentBlock.GetComponent<JellyBlock>();
        int spawnY = GetVisibleSpawnAnchorY(jelly);
        currentBlock.transform.position = board.GetWorldPosition(currentBaseX, spawnY);
    }

    int GetVisibleSpawnAnchorY(JellyBlock jelly)
    {
        if (jelly == null || board == null)
            return 0;

        int maxOffsetY = 0;
        foreach (Vector2Int offset in GetShapeCellOffsets(jelly))
        {
            if (offset.y > maxOffsetY)
                maxOffsetY = offset.y;
        }

        int topVisibleCell = Mathf.Max(0, board.height - 1 - Mathf.Max(0, spawnTopPaddingCells));
        return Mathf.Clamp(topVisibleCell - maxOffsetY, 0, board.height - 1);
    }

    void DropCurrentBlock()
    {
        if (currentBlock == null || board == null) return;

        JellyBlock currentJelly = currentBlock.GetComponent<JellyBlock>();
        List<Vector2Int> landingCells = CalculateVerticalLandingCells(currentJelly, currentBaseX);

        if (landingCells.Count == 0)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();

            return;
        }

        Rigidbody2D rb = currentBlock.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        GameObject droppingBlock = currentBlock;
        currentBlock = nextBlock;
        currentBaseX = board.width / 2;

        if (currentBlock != null)
        {
            SetBlockRenderersVisible(currentBlock, true);
            ClampBaseXToShape(currentBlock.GetComponent<JellyBlock>());
            UpdateCurrentBlockDropPosition();
        }

        if (nextPreviewPoint != null)
        {
            nextBlock = CreatePreviewBlock(nextPreviewPoint.position);
            HideNextBlockWorldPreview();
        }

        StartCoroutine(MoveBlockDownToLanding(droppingBlock, currentJelly, landingCells));
    }

    public void DropCurrentBlockFromButton()
    {
        if (!CanUseDropInput()) return;

        DropCurrentBlock();
    }

    void HideNextBlockWorldPreview()
    {
        if (nextBlock == null)
            return;

        nextBlock.transform.position = new Vector3(1000f, 1000f, 0f);
        SetBlockRenderersVisible(nextBlock, false);
    }

    void SetBlockRenderersVisible(GameObject block, bool isVisible)
    {
        if (block == null)
            return;

        SpriteRenderer[] renderers = block.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.enabled = isVisible;
        }
    }

    public List<Vector2Int> CalculateVerticalLandingCells(JellyBlock jelly, int baseX)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        if (jelly == null || board == null) return result;

        List<Vector2Int> offsets = GetShapeCellOffsets(jelly);

        for (int anchorY = 0; anchorY < board.height; anchorY++)
        {
            if (!CanPlaceAt(baseX, anchorY, offsets)) continue;

            foreach (Vector2Int offset in offsets)
            {
                result.Add(new Vector2Int(baseX + offset.x, anchorY + offset.y));
            }

            return result;
        }

        return result;
    }

    bool CanPlaceAt(int baseX, int anchorY, List<Vector2Int> offsets)
    {
        foreach (Vector2Int offset in offsets)
        {
            int x = baseX + offset.x;
            int y = anchorY + offset.y;

            if (!board.IsInside(x, y))
                return false;

            if (!board.IsEmpty(x, y))
                return false;
        }

        return true;
    }

    List<Vector2Int> GetShapeCellOffsets(JellyBlock jelly)
    {
        List<Vector2Int> offsets = new List<Vector2Int>();

        foreach (Vector2 offset in jelly.GetShapeOffsets())
        {
            offsets.Add(new Vector2Int(Mathf.RoundToInt(offset.x), Mathf.RoundToInt(offset.y)));
        }

        return offsets;
    }

    IEnumerator MoveBlockDownToLanding(GameObject droppingBlock, JellyBlock jelly, List<Vector2Int> landingCells)
    {
        isDropping = true;

        Vector2 startPosition = droppingBlock.transform.position;
        List<Vector2Int> offsets = GetShapeCellOffsets(jelly);
        Vector2 firstCellWorld = board.GetWorldPosition(landingCells[0].x, landingCells[0].y);
        Vector2 endPosition = firstCellWorld - (Vector2)offsets[0] * board.cellSize;
        float distance = Mathf.Abs(startPosition.y - endPosition.y);
        float duration = Mathf.Max(0.05f, distance / Mathf.Max(0.01f, dropMoveSpeed));
        float elapsed = 0f;

        while (elapsed < duration && droppingBlock != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            droppingBlock.transform.position = Vector2.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        if (droppingBlock != null)
        {
            droppingBlock.transform.position = endPosition;
            jelly.PlaceAtCells(landingCells);
        }

        isDropping = false;
    }

    void ShootCurrentBlock(Vector2 direction)
    {
        if (currentBlock == null) return;

        JellyBlock currentJelly = currentBlock.GetComponent<JellyBlock>();
        LandingResult landing = CalculateLandingCells(direction);

        Rigidbody2D rb = currentBlock.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        GameObject shotBlock = currentBlock;

        currentBlock = nextBlock;

        if (currentBlock != null)
        {
            currentBlock.transform.position = transform.position + new Vector3(0f, 0.6f, 0f);
        }

        if (nextPreviewPoint != null)
        {
            nextBlock = CreatePreviewBlock(nextPreviewPoint.position);
        }

        StartCoroutine(MoveBlockToLanding(shotBlock, currentJelly, landing));
    }

    public LandingResult CalculateLandingCells(Vector2 direction)
    {
        List<Vector2Int> fallbackCells = new List<Vector2Int>();
        Vector2 fallbackPosition = currentBlock != null ? currentBlock.transform.position : transform.position;
        Vector2 fallbackDirection = direction.sqrMagnitude < 0.001f ? Vector2.down : direction.normalized;

        if (currentBlock == null || board == null)
            return new LandingResult(fallbackCells, fallbackDirection, 0f, fallbackPosition);

        JellyBlock jelly = currentBlock.GetComponent<JellyBlock>();
        if (jelly == null)
            return new LandingResult(fallbackCells, fallbackDirection, 0f, fallbackPosition);

        Vector2 dir = fallbackDirection;
        Vector2 start = currentBlock.transform.position;
        float step = Mathf.Max(0.05f, board.cellSize * 0.1f);

        List<Vector2Int> lastValidCells = null;
        Vector2 lastValidPosition = start;
        float lastValidDistance = 0f;

        for (float distance = 0f; distance <= maxPreviewDistance; distance += step)
        {
            Vector2 anchor = start + dir * distance;
            List<Vector2Int> cells = GetCellsFromAnchor(jelly, anchor);

            if (CanPlaceCells(cells))
            {
                lastValidCells = cells;
                lastValidPosition = anchor;
                lastValidDistance = distance;
                fallbackCells = cells;
                fallbackPosition = anchor;
                continue;
            }

            if (lastValidCells != null)
                return new LandingResult(lastValidCells, dir, lastValidDistance, lastValidPosition);
        }

        if (lastValidCells != null)
            return new LandingResult(lastValidCells, dir, lastValidDistance, lastValidPosition);

        return new LandingResult(fallbackCells, dir, 0f, fallbackPosition);
    }

    List<Vector2Int> GetCellsFromAnchor(JellyBlock jelly, Vector2 anchorWorldPos)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        foreach (Vector2 offset in jelly.GetShapeOffsets())
        {
            Vector2 worldPos = anchorWorldPos + offset * board.cellSize;
            cells.Add(board.GetGridPosition(worldPos));
        }

        return cells;
    }

    bool CanPlaceCells(List<Vector2Int> cells)
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

    IEnumerator MoveBlockToLanding(GameObject shotBlock, JellyBlock jelly, LandingResult landing)
    {
        isShooting = true;

        Vector2 startPosition = shotBlock.transform.position;
        float duration = Mathf.Max(0.05f, landing.travelDistance / Mathf.Max(0.01f, shootMoveSpeed));
        float elapsed = 0f;

        while (elapsed < duration && shotBlock != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentDistance = landing.travelDistance * t;
            shotBlock.transform.position = startPosition + landing.direction * currentDistance;
            yield return null;
        }

        if (shotBlock != null)
        {
            shotBlock.transform.position = startPosition + landing.direction * landing.travelDistance;
            jelly.PlaceAtCells(landing.cells);
        }

        isShooting = false;
    }

    void DrawAimLine(Vector2 dragPoint)
    {
        if (lineRenderer == null) return;

        lineRenderer.SetPosition(0, transform.position);

        Vector2 shootDir = (Vector2)transform.position - dragPoint;
        Vector2 endPoint = (Vector2)transform.position + shootDir.normalized * maxPreviewDistance;

        lineRenderer.SetPosition(1, endPoint);
    }

    void CreateGhostRoot()
    {
        if (ghostRoot != null)
            Destroy(ghostRoot);

        ghostRoot = new GameObject("GhostRoot");
        ghostRoot.SetActive(false);
    }

    void UpdateGhostPreview(Vector2 direction)
    {
        if (currentBlock == null || board == null || ghostRoot == null)
            return;

        for (int i = ghostRoot.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(ghostRoot.transform.GetChild(i).gameObject);
        }

        if (direction.sqrMagnitude < 0.001f)
            return;

        Vector2 start = transform.position;
        Vector2 dir = direction.normalized;

        RaycastHit2D hit = Physics2D.Raycast(start, dir, maxPreviewDistance);

        Vector2 predictedAnchor;

        if (hit.collider != null && !hit.collider.transform.IsChildOf(currentBlock.transform))
        {
            predictedAnchor = hit.point - dir * 0.1f;
        }
        else
        {
            predictedAnchor = start + dir * maxPreviewDistance;
        }

        JellyBlock jelly = currentBlock.GetComponent<JellyBlock>();
        List<Vector2Int> landingCells = jelly.PredictLandingCellsFromAnchor(predictedAnchor);

        Color ghostColor = GetGhostColor(jelly.blockColor);

        foreach (Vector2Int cell in landingCells)
        {
            GameObject ghostPiece = new GameObject("GhostPiece");
            ghostPiece.transform.SetParent(ghostRoot.transform);
            ghostPiece.transform.position = board.GetWorldPosition(cell.x, cell.y);

            SpriteRenderer ghostRenderer = ghostPiece.AddComponent<SpriteRenderer>();

            SpriteRenderer src = currentBlock.GetComponent<SpriteRenderer>();
            if (src != null)
            {
                ghostRenderer.sprite = src.sprite;
            }

            ghostRenderer.sortingOrder = 1;
            ghostRenderer.color = ghostColor;
        }
    }

    Color GetGhostColor(JellyBlock.BlockColor blockColor)
    {
        switch (blockColor)
        {
            case JellyBlock.BlockColor.Red:
                return new Color(1f, 0f, 0f, 0.35f);
            case JellyBlock.BlockColor.Blue:
                return new Color(0f, 0f, 1f, 0.35f);
            case JellyBlock.BlockColor.Green:
                return new Color(0f, 1f, 0f, 0.35f);
            case JellyBlock.BlockColor.Yellow:
                return new Color(1f, 1f, 0f, 0.35f);
            case JellyBlock.BlockColor.Purple:
                return new Color(0.75f, 0.25f, 1f, 0.35f);
            default:
                return new Color(1f, 1f, 1f, 0.35f);
        }
    }
}
