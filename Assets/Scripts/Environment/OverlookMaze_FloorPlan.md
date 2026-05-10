# Overlook Hotel Hedge Maze — Unity Floor Plan
## Based on: The Shining (1980) Hedge Maze

---

## Coordinate System

```
World Origin: (0, 0, 0)
Maze bounds:  X: -26.2 to +26.2  |  Z: -35.8 to +35.8
Floor Y:      -0.2 to 0
Wall Y:        0 to 4.0 (centre Y = 2.0)
Wall thickness: 0.45 units
Grid cell size: 2.85 units (2.4 path + 0.45 wall)
Grid size:      19 columns x 26 rows
```

```
Grid → World conversion:
  World X = -26.2 + 0.225 + (col × 2.85)
  World Z = -35.8 + 0.225 + (row × 2.85)

  Col 0  → X = -25.975     Row 0  → Z = -35.575
  Col 9  → X =  -0.325     Row 13 → Z =  +1.475  (centre)
  Col 18 → X = +25.325     Row 25 → Z = +35.275
```

---

## Floor Plan Grid (Top-down view)
```
     0    1    2    3    4    5    6    7    8    9   10   11   12   13   14   15   16   17   18
     |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |
 0 ──┼────┼────┼────┼────┼────┼────┼──╔╗────┼────┼────┼──╔╗────┼────┼────┼────┼────┼────┼────┼──
     │                              │  ││ENTR││  │                              │
 1   │    ────────────────           │  ╚╝ANCE╚╝  │           ────────────────   │
     │                              │             │                              │
 2   ├──────────────┐              ┌┤             ├┐              ┌──────────────┤
     │              │              ││             ││              │              │
 3   │         ┌────┘   ┌──────┐  ││             ││  ┌──────┐   └────┐          │
     │         │        │      │  ││             ││  │      │        │          │
 4   │    ─────┘   ─────┘  ────┘  ││  ─────────  ││  └────  └─────   └─────     │
     │                             │              │                              │
     [  UPPER LEFT QUADRANT  ]     │   [  TOP  ]  │     [ UPPER RIGHT QUADRANT ] 
     │                             │    CENTRE    │                              │
12   ├──────────────────────────────┤             ├──────────────────────────────┤
     │                              │             │                              │
13   │    ──────────────            └─────────────┘            ──────────────    │
     │                                                                           │
14   ├──────────────┐                                         ┌──────────────────┤
     │              │                                         │                  │
15   │    ──────────┘    ─────────────────────────────────    └──────────        │
     │                                                                           │
     [  LOWER LEFT QUADRANT  ]         [ CENTRE ]          [ LOWER RIGHT QUAD ] 
     │                             CROSS CORRIDORS                               │
16   │              ─────────────┐               ┌─────────────                  │
     │                           │               │                               │
17   │    ─────                  │               │                  ─────        │
     │                           │               │                               │
18   ├──────────────┐            └───────────────┘            ┌──────────────────┤
     │              │                                         │                  │
     [  LOWER LEFT  ]                                         [  LOWER RIGHT  ]
     │              │                                         │                  │
25   │    ──────────┘    ─────────────────────────────────    └──────────        │
     │                                                                           │
26 ──┼────┼────┼────┼────┼────┼────┼──╔╗────┼────┼────┼──╔╗────┼────┼────┼────┼────┼────┼────┼──
                                    EXIT (centre bottom)
```

---

## Key Dimensions

| Element | Value |
|---|---|
| Total width | 52.4 units |
| Total depth | 71.6 units |
| Wall height | 4.0 units |
| Wall thickness | 0.45 units |
| Path width | 2.4 units (playable corridor) |
| Grid cell | 2.85 units |
| Entrance | Top centre, 5.25 unit gap |
| Exit | Bottom centre, 5.25 unit gap |

---

## Playable Path Width Analysis

```
Minimum corridor: 2.4 units wide
Recommended player capsule: radius 0.3, height 1.8
Camera height above player: 8–12 units (isometric)
Recommended camera FOV: 60°
```

At these proportions the maze corridors feel appropriately tight 
for a chase/pathfinding game without being frustratingly narrow.

---

## Zone Breakdown

### Zone 1 — Entrance Corridor (Top)
```
World Z: -35.8 to -29.05
Entry gap at X: -2.625 to +2.625
Two parallel approach walls funnel player inward
```

### Zone 2 — Upper Quadrants (rows 1–12)
```
Left quadrant:   X: -26.2 to  -2.625
Right quadrant:  X:  +2.625 to +26.2
Centre spine:    X:  -2.625 to +2.625

Symmetric mirror design — left and right are near-identical
Nested rectangular loops, 3 layers deep each side
```

### Zone 3 — Middle Cross (rows 12–16)
```
World Z: -1.225 to +9.335
The iconic cross-shaped open area from the film
Two main corridors intersect here
This is where A* paths will most visibly cross
```

### Zone 4 — Lower Quadrants (rows 16–25)
```
Mirror of upper quadrants
Increasingly tight loops toward the exit
```

### Zone 5 — Exit Corridor (Bottom)
```
World Z: +29.05 to +35.8
Exit gap at X: -2.625 to +2.625
```

---

## Unity Setup Instructions

### Step 1 — Add the builder script
1. Copy `OverlookMazeBuilder.cs` into `Assets/Scripts/`
2. Create an empty GameObject in your scene → name it `MazeBuilder`
3. Attach `OverlookMazeBuilder` component to it

### Step 2 — Assign materials (optional)
- Create two materials: `MazeFLoor` and `MazeWall`
- Assign them in the component's Inspector slots
- Hedge green recommended: `#2D6A2D` or use a grass/foliage texture from Kenney.nl

### Step 3 — Build
- Click **BUILD MAZE** button in the Inspector
- The script generates the full `DungeonMaze` GameObject with all children

### Step 4 — NavMesh
- Add `NavMeshSurface` component to `DungeonMaze`
- Click **Bake**
- Verify walkable area covers all corridors (should be ~80 nodes on a 2.85 grid)

### Step 5 — Player spawn
- Place player at: `(0, 0, -32.0)` — just inside the entrance
- Place AI agent spawn at: `(0, 0, -28.0)` — one cell into the maze

### Step 6 — Camera (isometric)
```
Position: (0, 45, -20)
Rotation: (65, 0, 0)
Projection: Orthographic
Size: 20
```

---

## A* Grid Overlay

For the IS module graph, overlay a grid on the maze:
```
Grid origin: (-26.2, 0, -35.8)
Cell size:    2.85
Cols:         18 (skipping wall columns)
Rows:         25 (skipping wall rows)

Walkable check: Physics.CheckBox at each cell centre
  → if no collider hit → node is walkable → add to graph
```

This gives approximately **120–150 walkable nodes** — 
sufficient for clear A* visualization without performance overhead.
# Overlook Hotel Hedge Maze — Unity Floor Plan
## Based on: The Shining (1980) Hedge Maze

---

## Coordinate System

```
World Origin: (0, 0, 0)
Maze bounds:  X: -26.2 to +26.2  |  Z: -35.8 to +35.8
Floor Y:      -0.2 to 0
Wall Y:        0 to 4.0 (centre Y = 2.0)
Wall thickness: 0.45 units
Grid cell size: 2.85 units (2.4 path + 0.45 wall)
Grid size:      19 columns x 26 rows
```

```
Grid → World conversion:
  World X = -26.2 + 0.225 + (col × 2.85)
  World Z = -35.8 + 0.225 + (row × 2.85)

  Col 0  → X = -25.975     Row 0  → Z = -35.575
  Col 9  → X =  -0.325     Row 13 → Z =  +1.475  (centre)
  Col 18 → X = +25.325     Row 25 → Z = +35.275
```

---

## Floor Plan Grid (Top-down view)
```
     0    1    2    3    4    5    6    7    8    9   10   11   12   13   14   15   16   17   18
     |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |
 0 ──┼────┼────┼────┼────┼────┼────┼──╔╗────┼────┼────┼──╔╗────┼────┼────┼────┼────┼────┼────┼──
     │                              │  ││ENTR││  │                              │
 1   │    ────────────────           │  ╚╝ANCE╚╝  │           ────────────────   │
     │                              │             │                              │
 2   ├──────────────┐              ┌┤             ├┐              ┌──────────────┤
     │              │              ││             ││              │              │
 3   │         ┌────┘   ┌──────┐  ││             ││  ┌──────┐   └────┐          │
     │         │        │      │  ││             ││  │      │        │          │
 4   │    ─────┘   ─────┘  ────┘  ││  ─────────  ││  └────  └─────   └─────     │
     │                             │              │                              │
     [  UPPER LEFT QUADRANT  ]     │   [  TOP  ]  │     [ UPPER RIGHT QUADRANT ] 
     │                             │    CENTRE    │                              │
12   ├──────────────────────────────┤             ├──────────────────────────────┤
     │                              │             │                              │
13   │    ──────────────            └─────────────┘            ──────────────    │
     │                                                                           │
14   ├──────────────┐                                         ┌──────────────────┤
     │              │                                         │                  │
15   │    ──────────┘    ─────────────────────────────────    └──────────        │
     │                                                                           │
     [  LOWER LEFT QUADRANT  ]         [ CENTRE ]          [ LOWER RIGHT QUAD ] 
     │                             CROSS CORRIDORS                               │
16   │              ─────────────┐               ┌─────────────                  │
     │                           │               │                               │
17   │    ─────                  │               │                  ─────        │
     │                           │               │                               │
18   ├──────────────┐            └───────────────┘            ┌──────────────────┤
     │              │                                         │                  │
     [  LOWER LEFT  ]                                         [  LOWER RIGHT  ]
     │              │                                         │                  │
25   │    ──────────┘    ─────────────────────────────────    └──────────        │
     │                                                                           │
26 ──┼────┼────┼────┼────┼────┼────┼──╔╗────┼────┼────┼──╔╗────┼────┼────┼────┼────┼────┼────┼──
                                    EXIT (centre bottom)
```

---

## Key Dimensions

| Element | Value |
|---|---|
| Total width | 52.4 units |
| Total depth | 71.6 units |
| Wall height | 4.0 units |
| Wall thickness | 0.45 units |
| Path width | 2.4 units (playable corridor) |
| Grid cell | 2.85 units |
| Entrance | Top centre, 5.25 unit gap |
| Exit | Bottom centre, 5.25 unit gap |

---

## Playable Path Width Analysis

```
Minimum corridor: 2.4 units wide
Recommended player capsule: radius 0.3, height 1.8
Camera height above player: 8–12 units (isometric)
Recommended camera FOV: 60°
```

At these proportions the maze corridors feel appropriately tight 
for a chase/pathfinding game without being frustratingly narrow.

---

## Zone Breakdown

### Zone 1 — Entrance Corridor (Top)
```
World Z: -35.8 to -29.05
Entry gap at X: -2.625 to +2.625
Two parallel approach walls funnel player inward
```

### Zone 2 — Upper Quadrants (rows 1–12)
```
Left quadrant:   X: -26.2 to  -2.625
Right quadrant:  X:  +2.625 to +26.2
Centre spine:    X:  -2.625 to +2.625

Symmetric mirror design — left and right are near-identical
Nested rectangular loops, 3 layers deep each side
```

### Zone 3 — Middle Cross (rows 12–16)
```
World Z: -1.225 to +9.335
The iconic cross-shaped open area from the film
Two main corridors intersect here
This is where A* paths will most visibly cross
```

### Zone 4 — Lower Quadrants (rows 16–25)
```
Mirror of upper quadrants
Increasingly tight loops toward the exit
```

### Zone 5 — Exit Corridor (Bottom)
```
World Z: +29.05 to +35.8
Exit gap at X: -2.625 to +2.625
```

---

## Unity Setup Instructions

### Step 1 — Add the builder script
1. Copy `OverlookMazeBuilder.cs` into `Assets/Scripts/`
2. Create an empty GameObject in your scene → name it `MazeBuilder`
3. Attach `OverlookMazeBuilder` component to it

### Step 2 — Assign materials (optional)
- Create two materials: `MazeFLoor` and `MazeWall`
- Assign them in the component's Inspector slots
- Hedge green recommended: `#2D6A2D` or use a grass/foliage texture from Kenney.nl

### Step 3 — Build
- Click **BUILD MAZE** button in the Inspector
- The script generates the full `DungeonMaze` GameObject with all children

### Step 4 — NavMesh
- Add `NavMeshSurface` component to `DungeonMaze`
- Click **Bake**
- Verify walkable area covers all corridors (should be ~80 nodes on a 2.85 grid)

### Step 5 — Player spawn
- Place player at: `(0, 0, -32.0)` — just inside the entrance
- Place AI agent spawn at: `(0, 0, -28.0)` — one cell into the maze

### Step 6 — Camera (isometric)
```
Position: (0, 45, -20)
Rotation: (65, 0, 0)
Projection: Orthographic
Size: 20
```

---

## A* Grid Overlay

For the IS module graph, overlay a grid on the maze:
```
Grid origin: (-26.2, 0, -35.8)
Cell size:    2.85
Cols:         18 (skipping wall columns)
Rows:         25 (skipping wall rows)

Walkable check: Physics.CheckBox at each cell centre
  → if no collider hit → node is walkable → add to graph
```

This gives approximately **120–150 walkable nodes** — 
sufficient for clear A* visualization without performance overhead.
