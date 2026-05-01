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

    private bool isDragging = false;
    private Vector2 currentDrag;
    private LineRenderer lineRenderer;

    private GameObject currentBlock;
    private GameObject nextBlock;

    private GameObject ghostRoot;
    private Board board;

    void Start()
    {
        board = FindObjectOfType<Board>();

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
                    ghostRoot.SetActive(true);
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
            UpdateGhostPreview(direction);
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
        currentBlock = CreatePreviewBlock(transform.position + new Vector3(0f, 0.6f, 0f));

        if (nextPreviewPoint != null)
            nextBlock = CreatePreviewBlock(nextPreviewPoint.position);
    }

    GameObject CreatePreviewBlock(Vector3 pos)
    {
        GameObject block = Instantiate(blockPrefab, pos, Quaternion.identity);

        JellyBlock jelly = block.GetComponent<JellyBlock>();
        jelly.blockColor = (JellyBlock.BlockColor)Random.Range(0, 4);
        jelly.blockShape = (JellyBlock.BlockShape)Random.Range(0, 5);
        jelly.rotationIndex = 0;
        jelly.ApplyColorVisual();
        jelly.SetPreviewMode(true);

        return block;
    }

    void ShootCurrentBlock(Vector2 direction)
    {
        if (currentBlock == null) return;

        JellyBlock currentJelly = currentBlock.GetComponent<JellyBlock>();
        currentJelly.SetPreviewMode(false);

        Rigidbody2D rb = currentBlock.GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.AddForce(direction * shootPower, ForceMode2D.Impulse);

        currentBlock = nextBlock;

        if (currentBlock != null)
        {
            currentBlock.transform.position = transform.position + new Vector3(0f, 0.6f, 0f);
        }

        if (nextPreviewPoint != null)
        {
            nextBlock = CreatePreviewBlock(nextPreviewPoint.position);
        }
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
            default:
                return new Color(1f, 1f, 1f, 0.35f);
        }
    }
}