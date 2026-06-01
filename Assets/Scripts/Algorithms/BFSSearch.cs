using UnityEngine;
using System.Collections.Generic;

public class BFSSearch : MonoBehaviour
{
    public static BFSSearch Instance { get; private set; }

    private void Awake()
    {
        // Singleton — only one BFSSearch should exist in the scene
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }
    //This part is the start of the BFS pathfinding method.
    public List<Vector3> FindPath(
        Dictionary<Vector3, List<Vector3>> graph,
        Vector3 start,
        Vector3 goal)
    {
        //It checks whether the input data is valid before running BFS
        if (graph == null || graph.Count == 0)
        {
            Debug.LogWarning("[BFSSearch] Graph is empty or null.");
            return new List<Vector3>();
        }
       //Check whether start and goal are inside the graph
        if (!graph.ContainsKey(start) || !graph.ContainsKey(goal))
        {
            Debug.LogWarning("[BFSSearch] Start or goal node not found in graph.");
            return new List<Vector3>();
        }

        // If we are already at the goal, return immediately
        if (start == goal)
        {
            return new List<Vector3> { start };
        }

        // --- BFS Core ---

        //frontier is the waiting list of nodes to check.
        var frontier = new Queue<Vector3>();

        //visited stores nodes that BFS has already seen don't revisit them
        var visited = new HashSet<Vector3>();

        //This remembers the path history.
        var cameFrom = new Dictionary<Vector3, Vector3>();

        //begin at the start node
        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            // Take the next node from the front of the queue
            Vector3 current = frontier.Dequeue();

            // Did we reach the goal?
            if (current == goal)
            {
                break;
            }

            // Look at every neighbour of the current node
            if (!graph.TryGetValue(current, out List<Vector3> neighbors))
            {
                continue;
            }
            //This goes through each neighbour of the current node.
            foreach (Vector3 next in neighbors)
            {
                //If already visited, skip it.
                if (visited.Contains(next))
                {
                    continue;
                }

                visited.Add(next);          //Mark this neighbour as visited.
                cameFrom[next] = current;   //remember how we got this neighbour.
                frontier.Enqueue(next);     //schedule it to be explored
            }
        }

        //After BFS finishes, this method uses cameFrom to rebuild the final path.
        return ReconstructPath(cameFrom, start, goal);
    }
    //It rebuilds the path from the cameFrom map, tracing backwards from the goal to the start.
    private List<Vector3> ReconstructPath(
        Dictionary<Vector3, Vector3> cameFrom,
        Vector3 start,
        Vector3 goal)
    {
        if (!cameFrom.ContainsKey(goal))
        {
            //if goal is not inside cameFrom, it means no path was found.
            return new List<Vector3>();
        }
        //this builds the path backwards from goal to start
        var path = new List<Vector3> { goal };
        Vector3 current = goal;
        //keep tracing backwards until we hit the start node
        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse(); 
        return path;
    }
    //This part is a small test function to check whether your BFS algorithm works without using the full Unity maze.
    [ContextMenu("Run BFS Hardcoded Test")]
    public void RunHardcodedTest()
    {
        // Build a tiny 4-node graph:  A - B - C
        //                                 |
        //                                 D
        var graph = new Dictionary<Vector3, List<Vector3>>();

        Vector3 a = new Vector3(0f, 0f, 0f); //These create 4 nodes in world space.
        Vector3 b = new Vector3(1f, 0f, 0f);
        Vector3 c = new Vector3(2f, 0f, 0f);
        Vector3 d = new Vector3(1f, 0f, 1f);

        graph[a] = new List<Vector3> { b }; //This builds the connections (edges)
        graph[b] = new List<Vector3> { a, c, d };
        graph[c] = new List<Vector3> { b };
        graph[d] = new List<Vector3> { b };

        // Expected shortest path: A → B → C  (2 hops)
        List<Vector3> path = FindPath(graph, a, c);

        if (path.Count == 0)
            Debug.LogError("[BFSSearch] Test FAILED: no path found.");
        else
            Debug.Log($"[BFSSearch] Test PASSED: {string.Join(" -> ", path)}");
    }
}
