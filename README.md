# Labyrinth

A 3D maze game built in Unity 6 LTS for the joint **SE3032 — Graphics & Visualization** and **SE3062 — Intelligent Systems** assignment.

You play a hero exploring a connected **dungeon** and **forest** labyrinth while AI demon dogs hunt you down. Place barriers to seal off corridors and force the dogs to recompute their paths in real time. The game runs three pathfinding algorithms — **A\***, **BFS**, and **Dijkstra** — over the same navigation graph so you can compare them side-by-side.

![Title screenshot](docs/screenshots/01_title.png)

---

## Features

- Two zones, two levels each: `Dungeon/Level_1`, `Dungeon/Level_2`, `Forest/Level_1`, `Forest/Level_2`
- Third-person hero with cape and sword, animated walk / run / jump
- AI demon dog NPCs that pathfind toward the player every few seconds
- Three pathfinding algorithms running on a shared adjacency-list graph
- Dynamic graph adaptation — placing a barrier severs an edge and triggers re-planning
- Debug overlays: live path visualizer and side-by-side algorithm comparison

![Dungeon level](docs/screenshots/02_dungeon.png)
![Forest level](docs/screenshots/03_forest.png)

---

## Controls

| Key | Action |
|---|---|
| `W` `A` `S` `D` | Move |
| `Mouse` | Look around |
| `Left Shift` | Run |
| `Space` | Jump |
| `Left Click` | Place barrier |
| `P` | Toggle path visualizer |
| `C` | Toggle algorithm comparison overlay |
| `R` | Re-run all three algorithms |

---

## Pathfinding visualizer

When the visualizer is on, all three algorithms compute a path from the nearest demon dog to the player and draw their results simultaneously:

- **A\*** — green
- **BFS** — blue
- **Dijkstra** — yellow

![Path visualizer](docs/screenshots/04_path_visualizer.png)
![Algorithm comparison overlay](docs/screenshots/05_algorithm_comparison.png)

---

## Running the project

1. Install **Unity 6 LTS** via Unity Hub
2. Run `git lfs install` once on your machine
3. Clone this repo and open the project folder in Unity Hub
4. Open `Assets/Scenes/Dungeon/Level_1.unity` and press **Play**

---

## Team

| Name | Role |
|---|---|
| Dulain | Lead — dungeon layout, NavMesh, graph formulation |
| Suhasna | Forest layout, lighting, dynamic graph adaptation |
| Sasindi | Player physics, A\* search |
| Sumuditha | Character models, BFS + path visualizer |
| Luchitha | Demon dog agent, Dijkstra |
