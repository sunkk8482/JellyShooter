using UnityEngine;

public class JellyPlacedPiece : MonoBehaviour
{
    public JellyBlock.BlockColor blockColor;
    public int gridX;
    public int gridY;

    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
    }
}