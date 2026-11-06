using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrimoireAegis.Core
{
    public enum NodeType
    {
        Path,      // plain road
        Start,
        Enemy,     // stops you when you walk into it
        Elite,     // stops you; drops an Index Key
        Puzzle,    // no-timer environment puzzle (GDD §4); drops an Index Key
        KeyChest,  // hidden chest with an Index Key
        Treasure,
        Rest,
        Merchant,
        Shrine,    // Angel statue: heal
        Event,     // Lyra's notes / lore
        Trap,
        Gate,      // needs 3 Index Keys
        Boss
    }

    [Serializable]
    public class MapNode
    {
        public int id;
        public int gx, gy;
        public NodeType type;
        public string monsterId;
        public readonly List<int> neighbors = new List<int>();
        public bool revealed, visited, cleared;

        /// <summary>Nodes that stop the walk when you reach them (until cleared).</summary>
        public bool Blocks => !cleared && (type == NodeType.Enemy || type == NodeType.Elite || type == NodeType.Boss ||
                                           type == NodeType.Puzzle || type == NodeType.KeyChest || type == NodeType.Gate);
    }

    public class AdventureMap
    {
        public int act, width, height;
        public readonly List<MapNode> nodes = new List<MapNode>();
        public readonly Dictionary<Vector2Int, int> grid = new Dictionary<Vector2Int, int>();
        public int startId, gateId, bossId;

        public MapNode Get(int id) => id >= 0 && id < nodes.Count ? nodes[id] : null;
        public bool IsPath(int x, int y) => grid.ContainsKey(new Vector2Int(x, y));
        public MapNode At(int x, int y) => grid.TryGetValue(new Vector2Int(x, y), out int id) ? nodes[id] : null;

        public void RevealAround(int id, int radius)
        {
            var c = Get(id);
            foreach (var n in nodes)
                if (Mathf.Abs(n.gx - c.gx) + Mathf.Abs(n.gy - c.gy) <= radius) n.revealed = true;
        }
    }

    /// <summary>
    /// Hand-authored adventure boards, one per Act (validated: connected, all three keys reachable
    /// without passing the gate, boss only behind the gate).
    /// Legend: '.' road, S start, e monster, E elite+key, P puzzle+key, K key chest, T treasure,
    /// R rest, M merchant, H angel statue, ? notes, ^ trap, G gate, B boss.
    /// </summary>
    public static class AdventureMaps
    {
        private static readonly string[][] Layouts =
        {
            new[]
            {
                "                                  ",
                "   K..e...?        T..e..H        ",
                "   .     .         .     .        ",
                "   .     e..R..M...e     .        ",
                "   ^     .         .     E        ",
                "   .     .         ?     .        ",
                "   e..T..H         .     ..e..G   ",
                "   .               .          .   ",
                "   .   P..e..^.....e          B   ",
                "   .   .                          ",
                " S....e.                          ",
                "                                  ",
            },
            new[]
            {
                "                                  ",
                "  S..e..^..e..R           K       ",
                "        .     .           .       ",
                "  T..e..H     e..?..e..M..e...    ",
                "  .           .              .    ",
                "  .     P..e..e              E    ",
                "  .     .                    .    ",
                "  e..^..e..T.....e..H..e..G...    ",
                "                          .       ",
                "                          .       ",
                "                          B       ",
                "                                  ",
            },
            new[]
            {
                "                                  ",
                "     B                            ",
                "     .                            ",
                "     G..e..H..e...?   K..e..T     ",
                "           .      .   .     .     ",
                "   P..e..R..      ..e..     e     ",
                "   .               .        .     ",
                "   e..T..^..e..M...e..E..H..e     ",
                "   .                     .        ",
                "   ?..e..S               ^..T     ",
                "                                  ",
            },
        };

        private static NodeType Parse(char c)
        {
            switch (c)
            {
                case 'S': return NodeType.Start;
                case 'e': return NodeType.Enemy;
                case 'E': return NodeType.Elite;
                case 'P': return NodeType.Puzzle;
                case 'K': return NodeType.KeyChest;
                case 'T': return NodeType.Treasure;
                case 'R': return NodeType.Rest;
                case 'M': return NodeType.Merchant;
                case 'H': return NodeType.Shrine;
                case '?': return NodeType.Event;
                case '^': return NodeType.Trap;
                case 'G': return NodeType.Gate;
                case 'B': return NodeType.Boss;
                default: return NodeType.Path;
            }
        }

        public static AdventureMap Build(int act)
        {
            act = Mathf.Clamp(act, 1, 3);
            var rows = Layouts[act - 1];
            var stage = GrimoireDatabase.Stages[act];
            var map = new AdventureMap { act = act, height = rows.Length };
            foreach (var r in rows) map.width = Mathf.Max(map.width, r.Length);

            int enemyIdx = 0;
            for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
            {
                char ch = rows[y][x];
                if (ch == ' ') continue;
                var n = new MapNode { id = map.nodes.Count, gx = x, gy = y, type = Parse(ch) };
                if (n.type == NodeType.Enemy)
                {
                    // early monsters first, harder ones later along the board
                    var pool = stage.regularEnemies;
                    n.monsterId = pool[Mathf.Min(pool.Length - 1, (enemyIdx++ * pool.Length) / 9) % pool.Length];
                }
                else if (n.type == NodeType.Elite) n.monsterId = stage.eliteEnemies[0];
                else if (n.type == NodeType.Boss) n.monsterId = stage.bossId;
                if (n.type == NodeType.Start) { map.startId = n.id; n.cleared = n.visited = true; }
                if (n.type == NodeType.Gate) map.gateId = n.id;
                if (n.type == NodeType.Boss) map.bossId = n.id;
                map.grid[new Vector2Int(x, y)] = n.id;
                map.nodes.Add(n);
            }
            foreach (var n in map.nodes)
            {
                foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                    if (map.grid.TryGetValue(new Vector2Int(n.gx + d.x, n.gy + d.y), out int nb)) n.neighbors.Add(nb);
            }
            map.RevealAround(map.startId, 3);
            return map;
        }

        public static string NodeName(NodeType t)
        {
            switch (t)
            {
                case NodeType.Start: return "Titik Awal";
                case NodeType.Enemy: return "Monster";
                case NodeType.Elite: return "Penjaga Kunci";
                case NodeType.Puzzle: return "Rak Terkunci";
                case NodeType.KeyChest: return "Peti Tersembunyi";
                case NodeType.Treasure: return "Peti Harta";
                case NodeType.Rest: return "Perapian";
                case NodeType.Merchant: return "Lapak Mordecai";
                case NodeType.Shrine: return "Patung Malaikat";
                case NodeType.Event: return "Catatan Lyra";
                case NodeType.Trap: return "Jebakan";
                case NodeType.Gate: return "Gerbang Boss";
                case NodeType.Boss: return "Boss";
                default: return "Jalan";
            }
        }

        public static string NodeDesc(NodeType t)
        {
            switch (t)
            {
                case NodeType.Enemy: return "Menghadang jalan. Kalahkan untuk lewat.";
                case NodeType.Elite: return "Musuh kuat yang menyimpan 1 Kunci Indeks.";
                case NodeType.Puzzle: return "Teka-teki tanpa batas waktu. Hadiah: 1 Kunci Indeks.";
                case NodeType.KeyChest: return "Berisi 1 Kunci Indeks.";
                case NodeType.Treasure: return "Gold dan Focus.";
                case NodeType.Rest: return "Pulihkan HP dan MP.";
                case NodeType.Merchant: return "Beli ramuan dan perlengkapan.";
                case NodeType.Shrine: return "Pulihkan HP penuh.";
                case NodeType.Event: return "Catatan kecil dari Lyra.";
                case NodeType.Trap: return "Hati-hati, lantai rapuh.";
                case NodeType.Gate: return "Butuh 3 Kunci Indeks.";
                case NodeType.Boss: return "Penguasa Act ini.";
                default: return "";
            }
        }
    }
}
