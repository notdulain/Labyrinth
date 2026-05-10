using UnityEngine;

/// <summary>
/// A single grid cell used by the A* algorithm.
/// </summary>
public class Node
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;
    public int gCost;
    public int hCost;
    public Node parent;

    public int fCost
    {
        get { return gCost + hCost; }
    }

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;

        ResetPathData();
    }

    public void ResetPathData()
    {
        gCost = int.MaxValue;
        hCost = 0;
        parent = null;
    }

    public override string ToString()
    {
        Vector3 roundedPosition = new Vector3(
            Mathf.RoundToInt(worldPosition.x),
            Mathf.RoundToInt(worldPosition.y),
            Mathf.RoundToInt(worldPosition.z));

        return roundedPosition.ToString();
    }
}
