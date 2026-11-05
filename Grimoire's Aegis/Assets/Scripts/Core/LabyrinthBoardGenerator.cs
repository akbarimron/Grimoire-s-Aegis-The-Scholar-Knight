using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GrimoireAegis.Core
{
    public enum BoardTileType
    {
        Start,
        Enemy,
        Elite,
        Boss,
        Treasure,
        Merchant,
        Rest,
        Shrine,
        Event,
        Trap
    }

    [Serializable]
    public class BoardTileNode
    {
        public int id;
        public int gx;
        public int gy;
        public BoardTileType type;
        public int variant;
        public List<int> neighbors = new List<int>();
        public bool visited;
        public bool cleared;
        public bool revealed;
    }

    [Serializable]
    public class LabyrinthBoardData
    {
        public int act;
        public int width;
        public int height;
        public List<BoardTileNode> tiles = new List<BoardTileNode>();
        public Dictionary<Vector2Int, int> gridToId = new Dictionary<Vector2Int, int>();
        public int startId;
        public int bossId;

        public BoardTileNode GetTile(int id)
        {
            if (id < 0 || id >= tiles.Count) return null;
            return tiles[id];
        }

        public void RevealAround(int tileId, int radius = 1)
        {
            var center = GetTile(tileId);
            if (center == null) return;
            center.revealed = true;
            foreach (int nb in center.neighbors)
            {
                var nTile = GetTile(nb);
                if (nTile != null)
                {
                    nTile.revealed = true;
                    if (radius > 1)
                    {
                        foreach (int nb2 in nTile.neighbors)
                        {
                            var n2 = GetTile(nb2);
                            if (n2 != null) n2.revealed = true;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Procedural Labyrinth Board Generator modeled after Lost in Multiverse RPG's BoardGen.
    /// Creates a connected spanning-tree dungeon with extra loops, BFS distance placement,
    /// guaranteed Boss at the farthest node, Elite guard, Merchant, Rest, Treasure, Shrine, Event & Enemy tiles.
    /// </summary>
    public static class LabyrinthBoardGenerator
    {
        private static readonly Vector2Int[] Dirs = new[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        public static LabyrinthBoardData Generate(int act, int seed = -1)
        {
            System.Random rng = seed >= 0 ? new System.Random(seed) : new System.Random();
            var stage = GrimoireDatabase.Stages.ContainsKey(act) ? GrimoireDatabase.Stages[act] : GrimoireDatabase.Stages[1];
            int w = stage.gridW;
            int h = stage.gridH;

            var board = new LabyrinthBoardData
            {
                act = act,
                width = w,
                height = h
            };

            // Determine active cells (~82% density, keeping start (0, h/2) active)
            bool[,] active = new bool[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    active[x, y] = true;
                }
            }

            int holesToCarve = Mathf.Max(2, (w * h) / 6);
            for (int k = 0; k < holesToCarve; k++)
            {
                int hx = rng.Next(1, w - 1);
                int hy = rng.Next(0, h);
                active[hx, hy] = false;
            }
            active[0, h / 2] = true;
            active[w - 1, h / 2] = true;

            // Create nodes for connected component starting from (0, h/2)
            Vector2Int startPos = new Vector2Int(0, h / 2);
            Queue<Vector2Int> q = new Queue<Vector2Int>();
            HashSet<Vector2Int> reached = new HashSet<Vector2Int> { startPos };
            q.Enqueue(startPos);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                foreach (var d in Dirs)
                {
                    var nx = cur + d;
                    if (nx.x >= 0 && nx.x < w && nx.y >= 0 && nx.y < h && active[nx.x, nx.y] && !reached.Contains(nx))
                    {
                        reached.Add(nx);
                        q.Enqueue(nx);
                    }
                }
            }

            var sortedCells = reached.OrderBy(c => c.y * w + c.x).ToList();
            for (int i = 0; i < sortedCells.Count; i++)
            {
                var c = sortedCells[i];
                var node = new BoardTileNode
                {
                    id = i,
                    gx = c.x,
                    gy = c.y,
                    type = BoardTileType.Enemy,
                    variant = rng.Next(0, 4)
                };
                board.tiles.Add(node);
                board.gridToId[c] = i;
            }

            board.startId = board.gridToId[startPos];

            // Build randomized DFS spanning tree + ~38% extra loop corridors
            HashSet<int> treeVisited = new HashSet<int> { board.startId };
            Stack<int> stack = new Stack<int>();
            stack.Push(board.startId);

            while (stack.Count > 0)
            {
                int curId = stack.Peek();
                var curNode = board.tiles[curId];
                var curVec = new Vector2Int(curNode.gx, curNode.gy);

                var unvisitedNeighbors = new List<int>();
                foreach (var d in Dirs)
                {
                    var np = curVec + d;
                    if (board.gridToId.TryGetValue(np, out int nid) && !treeVisited.Contains(nid))
                    {
                        unvisitedNeighbors.Add(nid);
                    }
                }

                if (unvisitedNeighbors.Count > 0)
                {
                    int chosen = unvisitedNeighbors[rng.Next(unvisitedNeighbors.Count)];
                    Connect(board, curId, chosen);
                    treeVisited.Add(chosen);
                    stack.Push(chosen);
                }
                else
                {
                    stack.Pop();
                }
            }

            // Add extra loop edges so players have strategic path choices
            foreach (var node in board.tiles)
            {
                var pos = new Vector2Int(node.gx, node.gy);
                foreach (var d in Dirs)
                {
                    if (board.gridToId.TryGetValue(pos + d, out int nid))
                    {
                        if (!node.neighbors.Contains(nid) && rng.NextDouble() < 0.38)
                        {
                            Connect(board, node.id, nid);
                        }
                    }
                }
            }

            // Compute BFS distance from startId to place Boss at farthest node
            Dictionary<int, int> dist = new Dictionary<int, int> { [board.startId] = 0 };
            Queue<int> bfs = new Queue<int>();
            bfs.Enqueue(board.startId);
            int farthestId = board.startId;

            while (bfs.Count > 0)
            {
                int u = bfs.Dequeue();
                foreach (int v in board.tiles[u].neighbors)
                {
                    if (!dist.ContainsKey(v))
                    {
                        dist[v] = dist[u] + 1;
                        if (dist[v] >= dist[farthestId]) farthestId = v;
                        bfs.Enqueue(v);
                    }
                }
            }

            board.bossId = farthestId;
            var startTile = board.tiles[board.startId];
            startTile.type = BoardTileType.Start;
            startTile.visited = true;
            startTile.cleared = true;
            startTile.revealed = true;

            var bossTile = board.tiles[board.bossId];
            bossTile.type = BoardTileType.Boss;
            bossTile.revealed = true; // Boss beacon always visible on map

            // Assign remaining tiles by distance & weighted pool
            var middleIds = board.tiles
                .Select(t => t.id)
                .Where(id => id != board.startId && id != board.bossId)
                .OrderByDescending(id => dist.ContainsKey(id) ? dist[id] : 0)
                .ToList();

            if (middleIds.Count > 0)
            {
                // Place 1 Elite near the Boss
                board.tiles[middleIds[0]].type = BoardTileType.Elite;
            }
            if (middleIds.Count > 1)
            {
                board.tiles[middleIds[1]].type = BoardTileType.Rest;
            }
            if (middleIds.Count > 2)
            {
                board.tiles[middleIds[2]].type = BoardTileType.Merchant;
            }
            if (middleIds.Count > 3)
            {
                board.tiles[middleIds[3]].type = BoardTileType.Treasure;
            }
            if (middleIds.Count > 4)
            {
                board.tiles[middleIds[4]].type = BoardTileType.Shrine;
            }

            BoardTileType[] weightedPool = new[]
            {
                BoardTileType.Enemy, BoardTileType.Enemy, BoardTileType.Enemy,
                BoardTileType.Treasure,
                BoardTileType.Event,
                BoardTileType.Shrine,
                BoardTileType.Rest,
                BoardTileType.Merchant,
                BoardTileType.Trap
            };

            for (int i = 5; i < middleIds.Count; i++)
            {
                board.tiles[middleIds[i]].type = weightedPool[rng.Next(weightedPool.Length)];
            }

            board.RevealAround(board.startId, 1);
            return board;
        }

        private static void Connect(LabyrinthBoardData board, int a, int b)
        {
            if (!board.tiles[a].neighbors.Contains(b)) board.tiles[a].neighbors.Add(b);
            if (!board.tiles[b].neighbors.Contains(a)) board.tiles[b].neighbors.Add(a);
        }
    }
}
