using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Overlook Hotel Hedge Maze - Procedural Builder
/// Attach to an empty GameObject and click "Build Maze" in the Inspector.
/// All walls are created as children of the DungeonMaze parent.
/// 
/// Maze bounds:  X: -26.2 to 26.2  |  Z: -35.8 to 35.8
/// Wall height:  4.0 units
/// Wall thick:   0.45 units
/// Grid unit:    2.4 units (wall segment = 2.85 = 2.4 + 0.45 overlap)
/// </summary>
public class OverlookMazeBuilder : MonoBehaviour
{
    [Header("Maze Settings")]
    public Material floorMaterial;
    public Material wallMaterial;
    public bool buildOnStart = false;

    // -------------------------------------------------------
    // Grid helpers
    // -------------------------------------------------------
    // The maze image maps to a ~19 x 26 grid at 2.4u spacing.
    // Grid origin (cell 0,0) = world (-26.2, 0, -35.8) top-left corner.
    // Cell centre X = -26.2 + 0.225 + col * 2.85   (0.225 = half wall)
    // Cell centre Z = -35.8 + 0.225 + row * 2.85

    private const float WALL_H      = 4.0f;
    private const float WALL_T      = 0.45f;
    private const float CELL        = 2.85f;   // wall segment unit (includes thickness)
    private const float FLOOR_W     = 52.4f;
    private const float FLOOR_D     = 71.6f;
    private const float FLOOR_THICK = 0.2f;
    private const float OX          = -26.2f;  // world X origin
    private const float OZ          = -35.8f;  // world Z origin

    // Wall centre Y
    private const float WY = 2.0f;

    private GameObject mazeRoot;

    // -------------------------------------------------------
    // Entry points
    // -------------------------------------------------------
    void Start() { if (buildOnStart) BuildMaze(); }

    [ContextMenu("Build Maze")]
    public void BuildMaze()
    {
        ClearMaze();
        mazeRoot = new GameObject("DungeonMaze");
        mazeRoot.transform.position = Vector3.zero;

        BuildFloor();
        BuildBoundaryWalls();
        BuildInteriorWalls();

        Debug.Log("[OverlookMaze] Build complete. Total children: " + mazeRoot.transform.childCount);
    }

    [ContextMenu("Clear Maze")]
    public void ClearMaze()
    {
        var existing = GameObject.Find("DungeonMaze");
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing);
#else
            Destroy(existing);
#endif
        }
    }

    // -------------------------------------------------------
    // Floor
    // -------------------------------------------------------
    void BuildFloor()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.parent = mazeRoot.transform;
        floor.transform.localPosition = new Vector3(0, -0.1f, 0);
        floor.transform.localScale = new Vector3(FLOOR_W, FLOOR_THICK, FLOOR_D);
        ApplyMaterial(floor, floorMaterial);
    }

    // -------------------------------------------------------
    // Boundary walls (the 2 long outer vertical walls)
    // -------------------------------------------------------
    void BuildBoundaryWalls()
    {
        // Left boundary  X = -26.2 + 0.225
        SpawnWall("Boundary_Left",
            new Vector3(OX + WALL_T * 0.5f, WY, 0),
            new Vector3(WALL_T, WALL_H, 70.05f));

        // Right boundary X = 26.2 - 0.225
        SpawnWall("Boundary_Right",
            new Vector3(-OX - WALL_T * 0.5f, WY, 0),
            new Vector3(WALL_T, WALL_H, 70.05f));

        // Top boundary
        SpawnWall("Boundary_Top",
            new Vector3(0, WY, OZ + WALL_T * 0.5f),
            new Vector3(FLOOR_W, WALL_H, WALL_T));

        // Bottom boundary (gap in centre for exit - leave 5.25 opening)
        SpawnWall("Boundary_Bottom_L",
            new Vector3(-13.575f, WY, -OZ - WALL_T * 0.5f),
            new Vector3(25.25f, WALL_H, WALL_T));
        SpawnWall("Boundary_Bottom_R",
            new Vector3(13.575f, WY, -OZ - WALL_T * 0.5f),
            new Vector3(25.25f, WALL_H, WALL_T));
    }

    // -------------------------------------------------------
    // Interior walls — reconstructed from Overlook maze image
    // Coordinates use WorldH(col, row) / WorldV(col, row) helpers.
    //
    // H = horizontal wall  → position at grid intersection, extends along X
    // V = vertical wall    → position at grid intersection, extends along Z
    //
    // The maze is traced from the image using a 19-col x 26-row grid.
    // Row 0 = top (Z = -35.8), Row 25 = bottom (Z = +35.8)
    // Col 0 = left (X = -26.2), Col 18 = right (X = +26.2)
    // -------------------------------------------------------
    void BuildInteriorWalls()
    {
        // ===================================================
        // ENTRANCE CORRIDOR (top centre)
        // ===================================================
        H(7, 1, 4);   // top entrance left wing
        H(10, 1, 4);  // top entrance right wing

        V(7, 0, 2);
        V(12, 0, 2);

        // ===================================================
        // OUTER RING — top section
        // ===================================================
        H(1, 2, 6);
        H(13, 2, 5);

        V(1, 2, 2);
        V(7, 2, 1);
        V(12, 2, 1);
        V(18, 2, 2);

        H(1, 4, 3);
        H(4, 3, 3);
        H(14, 3, 4);

        V(4, 3, 2);
        V(7, 3, 2);
        V(11, 3, 2);
        V(14, 3, 1);

        // ===================================================
        // UPPER LEFT QUADRANT
        // ===================================================
        H(1, 5, 3);
        H(2, 6, 2);
        H(1, 7, 2);
        H(3, 7, 2);
        H(1, 8, 4);
        H(2, 9, 2);
        H(1, 10, 2);
        H(3, 10, 2);
        H(2, 11, 3);
        H(1, 12, 4);

        V(1, 5, 2);
        V(4, 5, 2);
        V(2, 6, 1);
        V(3, 6, 1);
        V(1, 7, 1);
        V(4, 7, 3);
        V(2, 8, 1);
        V(3, 9, 1);
        V(1, 9, 1);
        V(2, 10, 1);
        V(5, 10, 2);
        V(1, 11, 1);
        V(4, 11, 1);
        V(3, 12, 2);

        // ===================================================
        // UPPER RIGHT QUADRANT
        // ===================================================
        H(14, 5, 4);
        H(15, 6, 2);
        H(14, 7, 2);
        H(16, 7, 2);
        H(14, 8, 4);
        H(15, 9, 2);
        H(14, 10, 2);
        H(16, 10, 2);
        H(13, 11, 3);
        H(14, 12, 4);

        V(14, 5, 2);
        V(17, 5, 2);
        V(15, 6, 1);
        V(16, 6, 1);
        V(14, 7, 1);
        V(13, 7, 3);
        V(15, 8, 1);
        V(16, 9, 1);
        V(14, 9, 1);
        V(15, 10, 1);
        V(12, 10, 2);
        V(14, 11, 1);
        V(13, 11, 1);
        V(14, 12, 2);

        // ===================================================
        // UPPER CENTRE CORRIDOR
        // ===================================================
        H(6, 4, 2);
        H(11, 4, 2);
        H(7, 5, 5);
        H(8, 6, 3);
        H(7, 7, 2);
        H(10, 7, 2);

        V(6, 4, 3);
        V(13, 4, 3);
        V(8, 5, 2);
        V(11, 5, 2);
        V(7, 6, 1);
        V(12, 6, 1);
        V(9, 7, 2);

        // ===================================================
        // MIDDLE SECTION
        // ===================================================
        H(2, 13, 5);
        H(12, 13, 5);
        H(6, 14, 3);
        H(10, 14, 3);

        V(2, 12, 2);
        V(7, 12, 2);
        V(12, 12, 2);
        V(17, 12, 2);
        V(6, 13, 2);
        V(9, 13, 1);
        V(13, 13, 2);

        H(1, 14, 5);
        H(13, 14, 5);
        H(3, 15, 4);
        H(12, 15, 4);

        V(1, 14, 2);
        V(6, 14, 2);
        V(13, 14, 2);
        V(18, 14, 2);
        V(3, 15, 1);
        V(7, 15, 2);
        V(12, 15, 1);
        V(16, 15, 2);

        // ===================================================
        // CENTRE — the iconic cross paths
        // ===================================================
        H(4, 16, 5);
        H(10, 16, 5);

        V(4, 16, 2);
        V(9, 15, 2);
        V(10, 15, 2);
        V(15, 16, 2);

        H(4, 17, 2);
        H(13, 17, 2);
        H(7, 18, 5);

        V(4, 17, 1);
        V(6, 17, 2);
        V(13, 17, 2);
        V(15, 17, 1);
        V(7, 18, 1);
        V(12, 18, 1);

        // ===================================================
        // LOWER CENTRE
        // ===================================================
        H(3, 19, 3);
        H(13, 19, 3);
        H(6, 20, 7);

        V(3, 19, 2);
        V(6, 19, 2);
        V(13, 19, 2);
        V(16, 19, 2);
        V(9, 20, 2);

        H(2, 21, 4);
        H(13, 21, 4);
        H(7, 22, 5);

        V(2, 21, 2);
        V(6, 21, 1);
        V(13, 21, 1);
        V(17, 21, 2);
        V(7, 22, 1);
        V(12, 22, 1);

        // ===================================================
        // LOWER LEFT QUADRANT
        // ===================================================
        H(1, 18, 4);
        H(2, 19, 2);
        H(1, 20, 3);
        H(2, 21, 2);
        H(1, 22, 4);
        H(2, 23, 2);
        H(1, 24, 2);
        H(3, 24, 2);

        V(1, 18, 2);
        V(5, 18, 2);
        V(2, 19, 1);
        V(4, 20, 2);
        V(1, 20, 1);
        V(2, 22, 2);
        V(5, 22, 2);
        V(1, 23, 1);
        V(3, 23, 1);
        V(1, 24, 1);

        // ===================================================
        // LOWER RIGHT QUADRANT
        // ===================================================
        H(14, 18, 4);
        H(15, 19, 2);
        H(14, 20, 3);
        H(15, 21, 2);
        H(14, 22, 4);
        H(15, 23, 2);
        H(14, 24, 2);
        H(16, 24, 2);

        V(14, 18, 2);
        V(18, 18, 2);
        V(15, 19, 1);
        V(13, 20, 2);
        V(14, 20, 1);
        V(15, 22, 2);
        V(12, 22, 2);
        V(14, 23, 1);
        V(16, 23, 1);
        V(14, 24, 1);

        // ===================================================
        // BOTTOM SECTION
        // ===================================================
        H(2, 25, 5);
        H(12, 25, 5);
        H(6, 24, 3);
        H(10, 24, 3);

        V(2, 24, 1);
        V(7, 24, 1);
        V(12, 24, 1);
        V(17, 24, 1);
        V(6, 25, 1);
        V(9, 25, 1);
        V(13, 25, 1);

        // Exit path walls
        H(6, 26, 3);
        H(10, 26, 3);
        V(6, 25, 1);
        V(13, 25, 1);
    }

    // -------------------------------------------------------
    // Shorthand wall placers
    // H = horizontal wall spanning multiple columns
    // V = vertical wall spanning multiple rows
    // col, row = grid position of wall START
    // span = number of segments (each segment = CELL units)
    // -------------------------------------------------------
    int wallIndex = 0;

    void H(int col, int row, int span)
    {
        float wx = CX(col) + (span * CELL - WALL_T) * 0.5f - CELL * 0.5f;
        float wz = CZ(row) - CELL * 0.5f;
        float length = span * CELL - WALL_T;
        SpawnWall("HW_" + wallIndex++,
            new Vector3(wx, WY, wz),
            new Vector3(length, WALL_H, WALL_T));
    }

    void V(int col, int row, int span)
    {
        float wx = CX(col) - CELL * 0.5f;
        float wz = CZ(row) + (span * CELL - WALL_T) * 0.5f - CELL * 0.5f;
        float length = span * CELL - WALL_T;
        SpawnWall("VW_" + wallIndex++,
            new Vector3(wx, WY, wz),
            new Vector3(WALL_T, WALL_H, length));
    }

    // World X centre of grid column
    float CX(int col) => OX + WALL_T * 0.5f + col * CELL;

    // World Z centre of grid row
    float CZ(int row) => OZ + WALL_T * 0.5f + row * CELL;

    // -------------------------------------------------------
    // Spawn a wall cube
    // -------------------------------------------------------
    void SpawnWall(string wallName, Vector3 position, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.parent = mazeRoot.transform;
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        ApplyMaterial(wall, wallMaterial);
    }

    void ApplyMaterial(GameObject go, Material mat)
    {
        if (mat != null)
            go.GetComponent<Renderer>().material = mat;
    }
}

// -------------------------------------------------------
// Custom Inspector — adds a Build button in the Editor
// -------------------------------------------------------
#if UNITY_EDITOR
[CustomEditor(typeof(OverlookMazeBuilder))]
public class OverlookMazeBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);
        var builder = (OverlookMazeBuilder)target;

        if (GUILayout.Button("BUILD MAZE", GUILayout.Height(40)))
            builder.BuildMaze();

        if (GUILayout.Button("Clear Maze", GUILayout.Height(25)))
            builder.ClearMaze();
    }
}
#endif
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Overlook Hotel Hedge Maze - Procedural Builder
/// Attach to an empty GameObject and click "Build Maze" in the Inspector.
/// All walls are created as children of the DungeonMaze parent.
/// 
/// Maze bounds:  X: -26.2 to 26.2  |  Z: -35.8 to 35.8
/// Wall height:  4.0 units
/// Wall thick:   0.45 units
/// Grid unit:    2.4 units (wall segment = 2.85 = 2.4 + 0.45 overlap)
/// </summary>
public class OverlookMazeBuilder : MonoBehaviour
{
    [Header("Maze Settings")]
    public Material floorMaterial;
    public Material wallMaterial;
    public bool buildOnStart = false;

    // -------------------------------------------------------
    // Grid helpers
    // -------------------------------------------------------
    // The maze image maps to a ~19 x 26 grid at 2.4u spacing.
    // Grid origin (cell 0,0) = world (-26.2, 0, -35.8) top-left corner.
    // Cell centre X = -26.2 + 0.225 + col * 2.85   (0.225 = half wall)
    // Cell centre Z = -35.8 + 0.225 + row * 2.85

    private const float WALL_H      = 4.0f;
    private const float WALL_T      = 0.45f;
    private const float CELL        = 2.85f;   // wall segment unit (includes thickness)
    private const float FLOOR_W     = 52.4f;
    private const float FLOOR_D     = 71.6f;
    private const float FLOOR_THICK = 0.2f;
    private const float OX          = -26.2f;  // world X origin
    private const float OZ          = -35.8f;  // world Z origin

    // Wall centre Y
    private const float WY = 2.0f;

    private GameObject mazeRoot;

    // -------------------------------------------------------
    // Entry points
    // -------------------------------------------------------
    void Start() { if (buildOnStart) BuildMaze(); }

    [ContextMenu("Build Maze")]
    public void BuildMaze()
    {
        ClearMaze();
        mazeRoot = new GameObject("DungeonMaze");
        mazeRoot.transform.position = Vector3.zero;

        BuildFloor();
        BuildBoundaryWalls();
        BuildInteriorWalls();

        Debug.Log("[OverlookMaze] Build complete. Total children: " + mazeRoot.transform.childCount);
    }

    [ContextMenu("Clear Maze")]
    public void ClearMaze()
    {
        var existing = GameObject.Find("DungeonMaze");
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing);
#else
            Destroy(existing);
#endif
        }
    }

    // -------------------------------------------------------
    // Floor
    // -------------------------------------------------------
    void BuildFloor()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.parent = mazeRoot.transform;
        floor.transform.localPosition = new Vector3(0, -0.1f, 0);
        floor.transform.localScale = new Vector3(FLOOR_W, FLOOR_THICK, FLOOR_D);
        ApplyMaterial(floor, floorMaterial);
    }

    // -------------------------------------------------------
    // Boundary walls (the 2 long outer vertical walls)
    // -------------------------------------------------------
    void BuildBoundaryWalls()
    {
        // Left boundary  X = -26.2 + 0.225
        SpawnWall("Boundary_Left",
            new Vector3(OX + WALL_T * 0.5f, WY, 0),
            new Vector3(WALL_T, WALL_H, 70.05f));

        // Right boundary X = 26.2 - 0.225
        SpawnWall("Boundary_Right",
            new Vector3(-OX - WALL_T * 0.5f, WY, 0),
            new Vector3(WALL_T, WALL_H, 70.05f));

        // Top boundary
        SpawnWall("Boundary_Top",
            new Vector3(0, WY, OZ + WALL_T * 0.5f),
            new Vector3(FLOOR_W, WALL_H, WALL_T));

        // Bottom boundary (gap in centre for exit - leave 5.25 opening)
        SpawnWall("Boundary_Bottom_L",
            new Vector3(-13.575f, WY, -OZ - WALL_T * 0.5f),
            new Vector3(25.25f, WALL_H, WALL_T));
        SpawnWall("Boundary_Bottom_R",
            new Vector3(13.575f, WY, -OZ - WALL_T * 0.5f),
            new Vector3(25.25f, WALL_H, WALL_T));
    }

    // -------------------------------------------------------
    // Interior walls — reconstructed from Overlook maze image
    // Coordinates use WorldH(col, row) / WorldV(col, row) helpers.
    //
    // H = horizontal wall  → position at grid intersection, extends along X
    // V = vertical wall    → position at grid intersection, extends along Z
    //
    // The maze is traced from the image using a 19-col x 26-row grid.
    // Row 0 = top (Z = -35.8), Row 25 = bottom (Z = +35.8)
    // Col 0 = left (X = -26.2), Col 18 = right (X = +26.2)
    // -------------------------------------------------------
    void BuildInteriorWalls()
    {
        // ===================================================
        // ENTRANCE CORRIDOR (top centre)
        // ===================================================
        H(7, 1, 4);   // top entrance left wing
        H(10, 1, 4);  // top entrance right wing

        V(7, 0, 2);
        V(12, 0, 2);

        // ===================================================
        // OUTER RING — top section
        // ===================================================
        H(1, 2, 6);
        H(13, 2, 5);

        V(1, 2, 2);
        V(7, 2, 1);
        V(12, 2, 1);
        V(18, 2, 2);

        H(1, 4, 3);
        H(4, 3, 3);
        H(14, 3, 4);

        V(4, 3, 2);
        V(7, 3, 2);
        V(11, 3, 2);
        V(14, 3, 1);

        // ===================================================
        // UPPER LEFT QUADRANT
        // ===================================================
        H(1, 5, 3);
        H(2, 6, 2);
        H(1, 7, 2);
        H(3, 7, 2);
        H(1, 8, 4);
        H(2, 9, 2);
        H(1, 10, 2);
        H(3, 10, 2);
        H(2, 11, 3);
        H(1, 12, 4);

        V(1, 5, 2);
        V(4, 5, 2);
        V(2, 6, 1);
        V(3, 6, 1);
        V(1, 7, 1);
        V(4, 7, 3);
        V(2, 8, 1);
        V(3, 9, 1);
        V(1, 9, 1);
        V(2, 10, 1);
        V(5, 10, 2);
        V(1, 11, 1);
        V(4, 11, 1);
        V(3, 12, 2);

        // ===================================================
        // UPPER RIGHT QUADRANT
        // ===================================================
        H(14, 5, 4);
        H(15, 6, 2);
        H(14, 7, 2);
        H(16, 7, 2);
        H(14, 8, 4);
        H(15, 9, 2);
        H(14, 10, 2);
        H(16, 10, 2);
        H(13, 11, 3);
        H(14, 12, 4);

        V(14, 5, 2);
        V(17, 5, 2);
        V(15, 6, 1);
        V(16, 6, 1);
        V(14, 7, 1);
        V(13, 7, 3);
        V(15, 8, 1);
        V(16, 9, 1);
        V(14, 9, 1);
        V(15, 10, 1);
        V(12, 10, 2);
        V(14, 11, 1);
        V(13, 11, 1);
        V(14, 12, 2);

        // ===================================================
        // UPPER CENTRE CORRIDOR
        // ===================================================
        H(6, 4, 2);
        H(11, 4, 2);
        H(7, 5, 5);
        H(8, 6, 3);
        H(7, 7, 2);
        H(10, 7, 2);

        V(6, 4, 3);
        V(13, 4, 3);
        V(8, 5, 2);
        V(11, 5, 2);
        V(7, 6, 1);
        V(12, 6, 1);
        V(9, 7, 2);

        // ===================================================
        // MIDDLE SECTION
        // ===================================================
        H(2, 13, 5);
        H(12, 13, 5);
        H(6, 14, 3);
        H(10, 14, 3);

        V(2, 12, 2);
        V(7, 12, 2);
        V(12, 12, 2);
        V(17, 12, 2);
        V(6, 13, 2);
        V(9, 13, 1);
        V(13, 13, 2);

        H(1, 14, 5);
        H(13, 14, 5);
        H(3, 15, 4);
        H(12, 15, 4);

        V(1, 14, 2);
        V(6, 14, 2);
        V(13, 14, 2);
        V(18, 14, 2);
        V(3, 15, 1);
        V(7, 15, 2);
        V(12, 15, 1);
        V(16, 15, 2);

        // ===================================================
        // CENTRE — the iconic cross paths
        // ===================================================
        H(4, 16, 5);
        H(10, 16, 5);

        V(4, 16, 2);
        V(9, 15, 2);
        V(10, 15, 2);
        V(15, 16, 2);

        H(4, 17, 2);
        H(13, 17, 2);
        H(7, 18, 5);

        V(4, 17, 1);
        V(6, 17, 2);
        V(13, 17, 2);
        V(15, 17, 1);
        V(7, 18, 1);
        V(12, 18, 1);

        // ===================================================
        // LOWER CENTRE
        // ===================================================
        H(3, 19, 3);
        H(13, 19, 3);
        H(6, 20, 7);

        V(3, 19, 2);
        V(6, 19, 2);
        V(13, 19, 2);
        V(16, 19, 2);
        V(9, 20, 2);

        H(2, 21, 4);
        H(13, 21, 4);
        H(7, 22, 5);

        V(2, 21, 2);
        V(6, 21, 1);
        V(13, 21, 1);
        V(17, 21, 2);
        V(7, 22, 1);
        V(12, 22, 1);

        // ===================================================
        // LOWER LEFT QUADRANT
        // ===================================================
        H(1, 18, 4);
        H(2, 19, 2);
        H(1, 20, 3);
        H(2, 21, 2);
        H(1, 22, 4);
        H(2, 23, 2);
        H(1, 24, 2);
        H(3, 24, 2);

        V(1, 18, 2);
        V(5, 18, 2);
        V(2, 19, 1);
        V(4, 20, 2);
        V(1, 20, 1);
        V(2, 22, 2);
        V(5, 22, 2);
        V(1, 23, 1);
        V(3, 23, 1);
        V(1, 24, 1);

        // ===================================================
        // LOWER RIGHT QUADRANT
        // ===================================================
        H(14, 18, 4);
        H(15, 19, 2);
        H(14, 20, 3);
        H(15, 21, 2);
        H(14, 22, 4);
        H(15, 23, 2);
        H(14, 24, 2);
        H(16, 24, 2);

        V(14, 18, 2);
        V(18, 18, 2);
        V(15, 19, 1);
        V(13, 20, 2);
        V(14, 20, 1);
        V(15, 22, 2);
        V(12, 22, 2);
        V(14, 23, 1);
        V(16, 23, 1);
        V(14, 24, 1);

        // ===================================================
        // BOTTOM SECTION
        // ===================================================
        H(2, 25, 5);
        H(12, 25, 5);
        H(6, 24, 3);
        H(10, 24, 3);

        V(2, 24, 1);
        V(7, 24, 1);
        V(12, 24, 1);
        V(17, 24, 1);
        V(6, 25, 1);
        V(9, 25, 1);
        V(13, 25, 1);

        // Exit path walls
        H(6, 26, 3);
        H(10, 26, 3);
        V(6, 25, 1);
        V(13, 25, 1);
    }

    // -------------------------------------------------------
    // Shorthand wall placers
    // H = horizontal wall spanning multiple columns
    // V = vertical wall spanning multiple rows
    // col, row = grid position of wall START
    // span = number of segments (each segment = CELL units)
    // -------------------------------------------------------
    int wallIndex = 0;

    void H(int col, int row, int span)
    {
        float wx = CX(col) + (span * CELL - WALL_T) * 0.5f - CELL * 0.5f;
        float wz = CZ(row) - CELL * 0.5f;
        float length = span * CELL - WALL_T;
        SpawnWall("HW_" + wallIndex++,
            new Vector3(wx, WY, wz),
            new Vector3(length, WALL_H, WALL_T));
    }

    void V(int col, int row, int span)
    {
        float wx = CX(col) - CELL * 0.5f;
        float wz = CZ(row) + (span * CELL - WALL_T) * 0.5f - CELL * 0.5f;
        float length = span * CELL - WALL_T;
        SpawnWall("VW_" + wallIndex++,
            new Vector3(wx, WY, wz),
            new Vector3(WALL_T, WALL_H, length));
    }

    // World X centre of grid column
    float CX(int col) => OX + WALL_T * 0.5f + col * CELL;

    // World Z centre of grid row
    float CZ(int row) => OZ + WALL_T * 0.5f + row * CELL;

    // -------------------------------------------------------
    // Spawn a wall cube
    // -------------------------------------------------------
    void SpawnWall(string wallName, Vector3 position, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.parent = mazeRoot.transform;
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        ApplyMaterial(wall, wallMaterial);
    }

    void ApplyMaterial(GameObject go, Material mat)
    {
        if (mat != null)
            go.GetComponent<Renderer>().material = mat;
    }
}

// -------------------------------------------------------
// Custom Inspector — adds a Build button in the Editor
// -------------------------------------------------------
#if UNITY_EDITOR
[CustomEditor(typeof(OverlookMazeBuilder))]
public class OverlookMazeBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);
        var builder = (OverlookMazeBuilder)target;

        if (GUILayout.Button("BUILD MAZE", GUILayout.Height(40)))
            builder.BuildMaze();

        if (GUILayout.Button("Clear Maze", GUILayout.Height(25)))
            builder.ClearMaze();
    }
}
#endif
