# Labyrinth — 7-Day Full Team Plan

> **SE3032 Graphics & Visualization + SE3062 Intelligent Systems**
> Joint Assignment | Semester 1, 2026

---

## Team Roles

| Name | GV Role | IS Role |
|---|---|---|
| **Dulain** | Lead + World Builder A (Dungeon layout, NavMesh) | IS-1: Graph Formulation |
| **Suhasna** | World Builder B (Forest maze, lighting, textures) | IS-2: Dynamic Adaptation |
| **Sasindi** | Systems Engineer (Player interaction physics) | IS-3: A* Search + Heuristic Design |
| **Sumuditha** | Core Developer (Blender models x2) | IS-4: BFS/UCS + Debug Visualizer |
| **Luchitha** | Agent Controller (Demon dog movement + animation) | IS-5: Dijkstra's + Algorithm Comparison |

---

## Interface Contracts (`CONTRACTS.md`)

> These must be agreed and committed on Day 0. Every student codes to these interfaces.

```
AStarSearch.cs      → returns List<Vector3>
BFSSearch.cs        → returns List<Vector3>
DijkstraSearch.cs   → returns List<Vector3>
GraphBuilder.cs     → exposes Dictionary<Vector3, List<Vector3>> AdjacencyList
EdgeSeverer.cs      → exposes void SeverEdge(Vector3 nodeA, Vector3 nodeB)
```

---

## Day 0 — Setup & Agreements
> **Everyone | 2–3 hours**

### All Team Members
- Install **Unity 6 LTS** via Unity Hub
- Install **VS Code** + Unity C# extension (`ms-dotnettools.csharp`)
- Run `git lfs install` on your machine
- Clone the repo
- Open the project in Unity once to confirm it loads without errors

### Dulain (Lead) — Additional Tasks
- In Unity Package Manager install: `ProBuilder`, `AI Navigation`, `Cinemachine`, `Post Processing`
  - Use **Install package by technical name** in Package Manager:
    - `com.unity.probuilder`
    - `com.unity.ai.navigation`
    - `com.unity.cinemachine`
    - `com.unity.postprocessing`
- Set the grid cell size standard: **1 Unity unit = 1 grid node** — communicate this to everyone
- Define and share the connection point spec between dungeon and forest scenes:
  - Door width = **2 units**, height = **3 units**, floor **Y = 0**
- Commit `CONTRACTS.md` to repo root with the interface contracts above

### Self-Learn Prompt (Everyone)
> *"I am a complete Unity beginner. Give me a 30-minute orientation covering: how GameObjects and Components work, what a MonoBehaviour is, how to attach a C# script to a GameObject, and how to navigate the Scene view. Use concrete examples, no fluff."*

---

## Day 1 — Greybox + Graph Foundation

---

### Dulain — World Builder A + IS-1 | 5–6 hours

**GV Task — Dungeon Greybox (`Dungeon.unity`)**
- Open ProBuilder: `Tools → ProBuilder → ProBuilder Window`
- Build a grid-based dungeon layout — 10×10 section, corridors 2 units wide, rooms 4×4 units
- Use only ProBuilder's Shape Tool (Cube) — no textures yet, just walls and floors
- Place the connection door at the agreed coordinates

**IS-1 Task — Start `GraphBuilder.cs`**
- Write a script that iterates over a 2D grid and creates a node for each walkable cell
- Walkable = no wall collider at that grid position (use `Physics.CheckBox`)
- Store as `Dictionary<Vector3, List<Vector3>>` — key is node world position, value is walkable neighbours
- Test by printing the adjacency list to the Unity console

**Self-Learn Prompt**
> *"Explain how to use Unity's Physics.CheckBox or Physics.OverlapBox in C# to detect if a grid cell is blocked by a collider. Give me a concrete code example for a top-down grid where each cell is 1 Unity unit."*

**Resources**
- ProBuilder docs: `docs.unity3d.com/Packages/com.unity.probuilder`
- Brackeys "How to make a Video Game" Episodes 1–3 (YouTube)

---

### Suhasna — World Builder B + IS-2 | 5–6 hours

**GV Task — Forest Greybox (`Forest.unity`)**
- Same ProBuilder approach — hedge walls as tall thin cubes (0.2 wide, 2 tall)
- Forest section 10×10, corridors 2 units wide
- Place connection door at the exact same spec as dungeon side

**IS-2 Task — Start `WallEventInterceptor.cs`**
- Write a C# event: `public static event Action<Vector3, Vector3> OnEdgeSevered`
- When a barrier is placed (Sasindi will fire this event later), intercept and call `EdgeSeverer.SeverEdge()`
- `EdgeSeverer.cs`: removes the edge between two nodes from the adjacency list and fires a recalculation request

**Self-Learn Prompt**
> *"Explain C# events and the Action delegate in Unity. How do I create a static event that one script fires and another script listens to, across different GameObjects? Give me a minimal working example."*

---

### Sasindi — Systems Engineer + IS-3 | 5–6 hours

**GV Task — `PlayerController.cs`**
- Isometric top-down movement: read `Input.GetAxis`, move via `transform.Translate`
- Starting camera angle: position `(-10, 20, -10)`, rotation `(45, 45, 0)`

**IS-3 Task — Start `AStarSearch.cs` (on paper first)**
- Write out the algorithm in pseudocode before touching Unity
- Define the heuristic — **Manhattan distance** for 4-directional movement, **Euclidean** for diagonal
- Decide movement direction today — lock it in and tell the team
- Start `PriorityQueue.cs` — implement a min-heap in C#

**Self-Learn Prompt**
> *"Explain A* search algorithm from scratch. Cover: the open list, closed list, g-cost, h-cost, f-cost, and how the priority queue works. Then show me a clean C# implementation for a grid-based game where nodes are Vector3 positions. Include the priority queue implementation."*

**Resources**
- Sebastian Lague "A* Pathfinding" series (YouTube) — definitive Unity A* tutorial
- `redblobgames.com/pathfinding/a-star/introduction.html` — best visual A* explainer

---

### Sumuditha — Core Developer + IS-4 | 5–6 hours

**GV Task — Start Hero Model in Blender**
- Target: 800–1000 polygons, blocky isometric proportions, small sword
- Don't rig yet — get the mesh right first
- Reference search: *"low poly character blender isometric"* on YouTube

**IS-4 Task — Start `BFSSearch.cs`**
- BFS is simpler than A* — implement it first to understand graph traversal
- Uses a Queue, no heuristic, finds shortest path by node count
- Print path to console as `List<Vector3>`

**Self-Learn Prompt**
> *"Show me how to implement BFS (Breadth-First Search) in C# for a graph where nodes are Vector3 positions and the graph is stored as Dictionary<Vector3, List<Vector3>>. Return the path as List<Vector3>."*

**Blender Resources**
- Quaternius (`quaternius.com`) — download a free low-poly character as mesh reference
- YouTube: "Low Poly Character in 1 Hour" by Imphenzia

---

### Luchitha — Agent Controller + IS-5 | 5–6 hours

**GV Task — Demon Dog Placeholder + `DemonDogController.cs`**
- Use a capsule GameObject as placeholder (model comes Day 3)
- Write `DemonDogController.cs` — accepts `List<Vector3>` path, moves agent using `Vector3.MoveTowards`
- Rotate agent toward next waypoint using `Quaternion.LookRotation`
- **Mock the path input** — hardcode a test `List<Vector3>` for now, replace with real A* output on Day 4

**IS-5 Task — Start `DijkstraSearch.cs`**
- Dijkstra = A* without the heuristic, with weighted edges
- Treat all edge weights as 1 for now (uniform cost)
- Can reuse Sasindi's `PriorityQueue.cs`

**Self-Learn Prompt**
> *"Explain Dijkstra's algorithm and how it differs from A* and BFS. Implement it in C# for a graph stored as Dictionary<Vector3, List<Vector3>> with uniform edge weights. Return the path as List<Vector3>. Then explain the time complexity difference between Dijkstra, BFS, and A*."*

---

## Day 2 — Core Systems + Modeling

---

### Dulain — WB A + IS-1 | 4–5 hours

**GV:** Add dungeon detail — pillars, central room, dead ends. ProBuilder only, no textures yet.

**IS-1:** Finish `GraphBuilder.cs`
- Install AI Navigation package, add `NavMeshSurface` component to dungeon floor, bake
- Cross-check: graph nodes should align with NavMesh walkable areas
- Expose as singleton: `public static GraphBuilder Instance`

**Coordination task:** Do a dry-run merge of dungeon + forest into `Main.unity` — catch connection point misalignments now, not Day 5.

---

### Suhasna — WB B + IS-2 | 4–5 hours

**GV:** Add forest detail — tree stump cylinders, uneven hedge heights. Bake NavMesh on forest floor.

**IS-2:** Finish `EdgeSeverer.cs`
- Test: manually call `SeverEdge()` on two adjacent nodes, verify they disappear from adjacency list
- Fire `OnRecalculationRequired` event after severing — IS-3, IS-4, IS-5 all listen to this

---

### Sasindi — Systems Engineer + IS-3 | 4–5 hours

**GV:** Write `BarrierPlacer.cs`
- Raycast from camera to floor on mouse click
- Instantiate `Barrier.prefab` (placeholder cube) at hit point
- Fire `WallEventInterceptor.OnEdgeSevered` with the two node positions the barrier sits between
- ⚠️ This script is the link between GV and IS — connects Sasindi's physics work to Suhasna's graph work

**IS-3:** Finish `AStarSearch.cs`
- Test against a hardcoded adjacency list before connecting to `GraphBuilder`
- Console print: `Path found: (0,0,0) → (1,0,0) → (2,0,0)...`

**Self-Learn Prompt**
> *"In Unity C#, how do I use Physics.Raycast from the main camera to the floor plane on mouse click, get the world position, and instantiate a prefab there? Give me the complete script."*

---

### Sumuditha — Core Developer + IS-4 | 4–5 hours

**GV:** Continue Hero model in Blender — finish mesh today, start basic armature rig.

**IS-4:** Start `PathVisualizer.cs`
- Use `Debug.DrawLine()` — draws lines between path nodes in Scene view
- Color coding: **A* = green**, **BFS = blue**, **Dijkstra = yellow**
- Toggle with `P` key using `Input.GetKeyDown`

**Self-Learn Prompt**
> *"How do I use Debug.DrawLine and Debug.DrawRay in Unity to visualize paths in the Scene view at runtime? How do I make them persist for more than one frame? Show me how to toggle visualization on/off with a keypress."*

---

### Luchitha — Agent Controller + IS-5 | 4–5 hours

**GV:** Refine `DemonDogController.cs`
- Add arrival threshold: if distance to next waypoint < 0.1f, advance to next waypoint
- Smooth rotation: use `Quaternion.Slerp` instead of instant `LookRotation`

**IS-5:** Finish `DijkstraSearch.cs`
- Test against same hardcoded graph Sasindi used for A* — outputs should be identical on uniform-weight graphs

---

## Day 3 — Integration Sprint

> ⚠️ Hardest day. All systems start talking to each other.

---

### Dulain — Lead Coordination | 4 hours

**Primary job today is integration, not building.**

- Merge all branches into `main`
- Fix Scene file conflicts — copy-paste GameObjects from `Dungeon.unity` and `Forest.unity` into `Main.unity`
- Set up `GraphBuilder` as a singleton (critical for all algorithm scripts):

```csharp
public class GraphBuilder : MonoBehaviour {
    public static GraphBuilder Instance;
    void Awake() { Instance = this; }
    public Dictionary<Vector3, List<Vector3>> AdjacencyList;
}
```

- Run the game — confirm no null reference errors on startup
- **IS-1 final:** Run `GraphBuilder` on `Main.unity`, verify node count (~60–80 nodes for a 10×10 grid minus walls)

---

### Suhasna — WB B + IS-2 | 3 hours

**GV:** Apply textures to forest — source from `ambientcg.com` (search: "grass", "bark", "gravel" — download 1024px).

**IS-2:** Connect `WallEventInterceptor` to `BarrierPlacer`
- Test: place a barrier in game → verify edge is severed in adjacency list → verify recalculation event fires
- Console log everything for debugging

---

### Sasindi — Systems Engineer + IS-3 | 4 hours

**GV:** Replace placeholder barrier cube with a proper `Barrier.prefab` — add semi-transparent material.

**IS-3:** Connect `AStarSearch` to `GraphBuilder.Instance.AdjacencyList`
- Test: set start = hero spawn position, end = fixed point across the map
- Confirm valid path prints to console

---

### Sumuditha — Core Developer + IS-4 | 5 hours

**GV:** Finish Hero mesh + rig in Blender. Export `Hero.fbx` → import into `Assets/Models/Characters/`. Start Demon Dog model.

**IS-4:** Connect `PathVisualizer` to all three algorithm outputs
- All three paths draw simultaneously in different colors when debug mode is on
- Confirm toggle works in play mode

---

### Luchitha — Agent Controller + IS-5 | 4 hours

**GV:** Replace hardcoded mock path with real A* output from `AStarSearch.Instance`
- Demon dog now pathfinds toward hero's actual position
- Update path every 2 seconds using `InvokeRepeating`

**IS-5:** Add `AlgorithmComparison.cs`
- On debug toggle, display on-screen UI (`OnGUI`) showing path lengths and node counts for all three algorithms side by side
- Strong viva talking point

---

## Day 4 — Polish + Animation

---

### Dulain — WB A | 3 hours

- Apply dungeon textures from `ambientcg.com` (search: "stone wall", "cobblestone", "medieval brick" — 1024px)
- Add point lights in dungeon rooms
- Set ambient light to near-black: `Window → Rendering → Lighting → Ambient Color`
- **Final NavMesh rebake** after all geometry is finalized — do not move walls after this

---

### Suhasna — WB B | 3 hours

- Add flickering light prefab using `FlickerLight.cs`:

```csharp
IEnumerator Flicker() {
    while(true) {
        GetComponent<Light>().enabled = !GetComponent<Light>().enabled;
        yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
    }
}
```

- Add post-processing Volume:
  - Dungeon: warm orange color grading
  - Forest: cold blue-green color grading
  - Both: slight vignette

---

### Sasindi — Systems Engineer | 3 hours

- Add highlight effect when player aims at a barrier (change material color on raycast hit)
- Add barrier placement limit: **max 5 barriers** to keep game balance
- Final bug check on `BarrierPlacer.cs` and `DoorController.cs`

---

### Sumuditha — Core Developer + IS-4 | 6 hours

**GV:** Finish Demon Dog model + export. Import both models. Set up Animator Controllers:
- Hero: Idle → Walk (transition on `speed` float parameter)
- Demon Dog: Idle → Walk → Attack

**IS-4:** Final PathVisualizer polish
- Replace `Debug.DrawLine` with `Gizmos.DrawSphere` at each node for cleaner Scene view visualization
- Ensure toggle works both in Scene view and Game view

---

### Luchitha — Agent Controller + IS-5 | 4 hours

**GV:** Hook Animator to `DemonDogController.cs`:

```csharp
animator.SetFloat("speed", agent.velocity.magnitude);
```

- Spawn multiple demon dogs (3–5) from a `SpawnManager.cs`
- Each dog runs its own A* independently

---

## Day 5 — Bug Fixes + Demo Prep

> **Everyone | 3–4 hours each**

### Priority Bug Checklist

| Bug | Fix |
|---|---|
| Null reference on `GraphBuilder.Instance` | Add null check before any algorithm runs |
| A* returns empty path when start = end | Add early return at top of search method |
| Demon dog jitters at waypoints | Increase arrival threshold to 0.2f |
| Barriers not severing edges | Add debug log to `EdgeSeverer` to trace calls |
| Animation not triggering | Check Animator parameter name matches exactly |

### Demo Video Structure (3 minutes)

| Timestamp | Content |
|---|---|
| 0:00–0:30 | Full environment walkthrough — both dungeon and forest zones |
| 0:30–1:00 | Player moves, demon dogs pathfind in real time |
| 1:00–1:30 | Player places barriers, dogs visibly recalculate paths |
| 1:30–2:30 | Toggle debug mode — show all three algorithm frontiers in different colors, explain verbally |
| 2:30–3:00 | Algorithm comparison UI on screen showing path lengths and node counts |

### Viva Prep Prompt (Everyone — run this individually)
> *"I implemented [your specific algorithm/role] in C# for a Unity grid-based game. Ask me 10 hard viva questions about my implementation, focusing on algorithmic complexity, edge cases, data structures used, and why I made specific design choices. After I answer each one, tell me if my answer is correct and how to improve it."*

---

## Days 6–7 — Buffer

> Do not plan features here. This is your slip buffer.

- Fix any integration issues that broke during Day 4–5 merges
- Re-record demo video if needed
- Each team member runs the viva prep prompt above against their own role

> ⚠️ The IS rubric awards **25 marks for algorithmic complexity explanation alone**. That is not something you can wing on the day. Every person must be able to mathematically explain their own algorithm.

---

## Resources Master List

| Resource | What For | Where |
|---|---|---|
| Brackeys "How to make a Video Game" | Unity orientation | YouTube |
| Sebastian Lague "A* Pathfinding" | Sasindi — A* implementation | YouTube |
| Imphenzia "Low Poly Character in 1 Hour" | Sumuditha — Blender modeling | YouTube |
| Red Blob Games Pathfinding | A*, BFS, Dijkstra visual explainer | `redblobgames.com/pathfinding` |
| AmbientCG | Free PBR textures | `ambientcg.com` |
| Quaternius | Free low-poly asset reference | `quaternius.com` |
| Kenney.nl | Free environment props | `kenney.nl/assets` |
| ProBuilder Docs | Level building | `docs.unity3d.com/Packages/com.unity.probuilder` |
| Unity AI Navigation Docs | NavMesh setup | `docs.unity3d.com/Packages/com.unity.ai.navigation` |

---

## Git Branching

```
main
├── feature/dungeon-layout        ← Dulain
├── feature/forest-layout         ← Suhasna
├── feature/barrier-physics       ← Sasindi
├── feature/models-debug          ← Sumuditha
└── feature/agent-dijkstra        ← Luchitha
```

> Merge into `main` only at milestones: end of Day 2, Day 4, and Day 5. Never work directly on `main`.