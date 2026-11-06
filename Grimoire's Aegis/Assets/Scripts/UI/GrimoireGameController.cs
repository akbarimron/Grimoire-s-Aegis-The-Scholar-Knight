using System;
using System.Collections.Generic;
using System.Linq;
using GrimoireAegis.Core;
using UnityEngine;

namespace GrimoireAegis.UI
{
    public enum GameViewMode
    {
        Title,
        ActIntro,
        Board,
        Battle,
        Victory
    }

    public enum BattlePhase
    {
        PlayerCommand,
        SpellSelect,
        ItemSelect,
        AlgorithmicChallenge, // Spell verification or Active Defend question
        TurnResolution,
        MercyIntervention,
        Animating,            // an attack / death sequence is playing; input is locked
        Analyze,              // GDD §6 Analyze: monster dossier
        BattleWon             // rewards summary
    }

    public enum ModalOverlay
    {
        None,
        CodexBestiary,
        ShopInventory,
        TeacherAnalytics,
        Settings,
        Guide,
        Pause
    }

    /// <summary>
    /// Game state + board exploration. Battle rules live in GrimoireBattle.cs,
    /// all drawing in GrimoireScreens.cs (partial class).
    /// </summary>
    public partial class GrimoireGameController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (FindFirstObjectByType<GrimoireGameController>() == null)
            {
                var go = new GameObject("GrimoireGameManager");
                DontDestroyOnLoad(go);
                go.AddComponent<GrimoireAudioManager>();
                go.AddComponent<GrimoireGameController>();
            }
        }

        // --- Player RPG State (Aegis, The Scholar Knight) ---
        public int playerLevel = 1;
        public int playerExp = 0;
        public int expToNext = 100;
        public int maxHp = 120;
        public int hp = 120;
        public int maxMp = 60;
        public int mp = 60;
        public int focus = 0; // 0..100 Limit Break gauge
        public int baseAtk = 18;
        public int baseDef = 8;
        public int baseSpd = 12;
        public int gold = 120;
        public int logicFragments = 0;

        public string equippedWeapon = "blade_lv1";
        public string equippedAccessory = "";
        public List<string> ownedEquipment = new List<string> { "blade_lv1" };
        public Dictionary<string, int> consumables = new Dictionary<string, int>
        {
            { "pot_hp", 3 },
            { "pot_mp", 2 },
            { "scroll_clarity", 1 },
            { "golden_die", 1 }
        };

        // --- Difficulty & accessibility (GDD §12, §17) ---
        public Difficulty difficulty = Difficulty.Medium;
        public float timerMultiplier = 1f;      // 1.0 / 1.25 / 1.5 / 2.0
        public bool accessibilityNoShake = false;
        public bool ddaEnabled = true;
        public bool runStarted = false;

        // --- DDA & Learning Telemetry State ---
        public string ddaTier = "Standard"; // "Assisted", "Standard", "Mastery"
        public int consecutiveCorrect = 0;
        public int consecutiveWrong = 0;
        public Dictionary<string, int> tpFailStreaks = new Dictionary<string, int>();
        public List<TelemetryRecord> telemetryLog = new List<TelemetryRecord>();
        public string lastExportedCsvPath = "";
        public readonly HashSet<string> monstersSeen = new HashSet<string>();
        public readonly HashSet<string> monstersAnalyzed = new HashSet<string>();
        public readonly HashSet<string> codexOpened = new HashSet<string>();

        // --- Labyrinth Board State ---
        public int currentAct = 1;
        public LabyrinthBoardData board;
        public int currentTileId = 0;
        public int diceValue = 1;
        public int stepsRemaining = 0;
        public bool forceGoldenDie = false;
        public string boardBannerMessage = "Lempar dadu [Spasi] untuk menjelajahi labirin perpustakaan!";
        private float _diceRollUntil;
        private float _actIntroStart;

        // --- View State ---
        public GameViewMode viewMode = GameViewMode.Title;
        public BattlePhase battlePhase = BattlePhase.PlayerCommand;
        public ModalOverlay activeModal = ModalOverlay.None;

        // --- Pixel-Art Texture Cache ---
        private readonly Dictionary<string, Texture2D> _texCache = new Dictionary<string, Texture2D>();

        // Computed Total Stats (Crushed lowers DEF by 20%, GDD §9)
        public int TotalAtk => baseAtk + (GrimoireDatabase.Items.TryGetValue(equippedWeapon, out var w) ? w.atkBonus : 0);
        public int TotalDef
        {
            get
            {
                int d = baseDef + (GrimoireDatabase.Items.TryGetValue(equippedAccessory, out var a) ? a.defBonus : 0);
                return HasP("crushed") ? Mathf.RoundToInt(d * 0.8f) : d;
            }
        }
        public int TotalSpd => baseSpd + (GrimoireDatabase.Items.TryGetValue(equippedAccessory, out var a) ? a.spdBonus : 0);
        public int TotalInt => 10 + (playerLevel - 1) * 2;
        public float AccessoryTimerBonus => GrimoireDatabase.Items.TryGetValue(equippedAccessory, out var a) ? a.timerBonus : 0f;

        private void Awake()
        {
            if (GetComponent<GrimoireAudioManager>() == null)
            {
                gameObject.AddComponent<GrimoireAudioManager>();
            }
            PixelUI.Tex = GetTex;
            PixelUI.Sfx = k => GrimoireAudioManager.Instance?.PlaySfx(k, k == "move" ? 0.35f : 1f);
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
            }
            GrimoireAudioManager.Instance?.PlayMusic("music_title");
            GenerateActBoard(1);
        }

        private void Update()
        {
            // Run scheduled animation beats (attack contact, death -> victory, etc.)
            if (_scheduled.Count > 0)
            {
                var due = _scheduled.Where(e => Time.time >= e.Key).ToList();
                _scheduled.RemoveAll(e => Time.time >= e.Key);
                foreach (var e in due.OrderBy(e => e.Key)) e.Value?.Invoke();
            }

            TickQuestionTimer();
            HandleKeyboard();
        }

        private void ToggleModal(ModalOverlay target)
        {
            GrimoireAudioManager.Instance?.PlaySfx("select");
            activeModal = (activeModal == target) ? ModalOverlay.None : target;
            _modalScroll = 0f;
            _listSel = 0;
        }

        // =====================================================================
        // PIXEL ART TEXTURE LOADER (STRICT POINT FILTERING)
        // =====================================================================
        private Texture2D GetTex(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            if (_texCache.TryGetValue(resourcePath, out Texture2D tex)) return tex;

            tex = Resources.Load<Texture2D>("Gen/" + resourcePath);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
            }
            _texCache[resourcePath] = tex;   // cache misses too (null)
            return tex;
        }

        // =====================================================================
        // NEW RUN / DIFFICULTY
        // =====================================================================
        public void StartNewRun(Difficulty d, int act = 1)
        {
            difficulty = d;
            playerLevel = 1; playerExp = 0; expToNext = 100;
            maxHp = hp = 120; maxMp = mp = 60; focus = 0;
            baseAtk = 18; baseDef = 8; baseSpd = 12; gold = 120; logicFragments = 0;
            equippedWeapon = "blade_lv1"; equippedAccessory = "";
            ownedEquipment = new List<string> { "blade_lv1" };
            consumables = new Dictionary<string, int> { { "pot_hp", 3 }, { "pot_mp", 2 }, { "scroll_clarity", 1 }, { "golden_die", 1 } };
            ddaTier = d == Difficulty.Easy ? "Assisted" : "Standard";
            panicStacks = 0;
            _pStatus.Clear();
            runStarted = true;
            StartActExpedition(act);
        }

        // =====================================================================
        // BOARD EXPLORATION & DICE LOGIC (LOST IN MULTIVERSE RPG STYLE)
        // =====================================================================
        public void StartActExpedition(int act)
        {
            currentAct = Mathf.Clamp(act, 1, 3);
            GenerateActBoard(currentAct);
            runStarted = true;
            activeModal = ModalOverlay.None;
            viewMode = GameViewMode.ActIntro;   // LIM-style chapter card first
            _actIntroStart = Time.time;

            var stage = GrimoireDatabase.Stages[currentAct];
            GrimoireAudioManager.Instance?.PlayMusic(stage.musicKey);
            boardBannerMessage = "Lempar dadu [Spasi] lalu pilih petak bercahaya. Kalahkan boss di ujung labirin!";
        }

        public void EnterBoardFromIntro()
        {
            viewMode = GameViewMode.Board;
            GrimoireAudioManager.Instance?.PlaySfx("select");
        }

        public void GenerateActBoard(int act)
        {
            board = LabyrinthBoardGenerator.Generate(act);
            currentTileId = board.startId;
            stepsRemaining = 0;
            diceValue = 1;
            board.RevealAround(currentTileId, 1);
        }

        public bool DiceRolling => Time.time < _diceRollUntil;

        public void RollExplorationDice()
        {
            if (stepsRemaining > 0 || DiceRolling) return;

            int spdDiceBonus = TotalSpd >= 16 ? 1 : 0;
            if (forceGoldenDie)
            {
                diceValue = 6;
                forceGoldenDie = false;
            }
            else
            {
                diceValue = UnityEngine.Random.Range(1, 7);
            }

            _diceRollUntil = Time.time + 0.55f;
            GrimoireAudioManager.Instance?.PlaySfx("dice");
            int steps = diceValue + spdDiceBonus;
            Schedule(0.55f, () =>
            {
                stepsRemaining = steps;
                boardBannerMessage = spdDiceBonus > 0
                    ? $"Dadu {{#ffd86a}}{diceValue}{{/}} + bonus SPD = {{#ffd86a}}{stepsRemaining}{{/}} langkah. Klik petak bercahaya atau tekan panah."
                    : $"Dadu {{#ffd86a}}{diceValue}{{/}} langkah. Klik petak bercahaya atau tekan panah untuk bergerak.";
            });
        }

        public void StepToNeighborTile(int targetTileId)
        {
            if (board == null || stepsRemaining <= 0) return;
            var curTile = board.GetTile(currentTileId);
            if (curTile == null || !curTile.neighbors.Contains(targetTileId)) return;

            currentTileId = targetTileId;
            stepsRemaining--;
            _tokenHopStart = Time.time;
            var landed = board.GetTile(currentTileId);
            landed.visited = true;
            board.RevealAround(currentTileId, 1);
            GrimoireAudioManager.Instance?.PlaySfx("step");

            // Boss / elite tiles stop you; otherwise resolve when steps run out
            if (landed.type == BoardTileType.Boss || landed.type == BoardTileType.Elite || stepsRemaining == 0)
            {
                stepsRemaining = 0;
                Schedule(0.25f, () => ResolveBoardTileEvent(landed));
            }
            else
            {
                boardBannerMessage = $"Sisa langkah: {{#ffd86a}}{stepsRemaining}{{/}}. Lanjutkan, atau berhenti di sini.";
            }
        }

        /// <summary>Arrow keys on the board: step to the neighbour in that direction.</summary>
        public void StepDirection(int dx, int dy)
        {
            if (board == null || stepsRemaining <= 0) return;
            var cur = board.GetTile(currentTileId);
            foreach (int nb in cur.neighbors)
            {
                var t = board.GetTile(nb);
                if (t.gx - cur.gx == dx && t.gy - cur.gy == dy) { StepToNeighborTile(nb); return; }
            }
        }

        public static string TileName(BoardTileType t)
        {
            switch (t)
            {
                case BoardTileType.Start: return "Gerbang Masuk";
                case BoardTileType.Enemy: return "Monster";
                case BoardTileType.Elite: return "Elite / Mid-Boss";
                case BoardTileType.Boss: return "Boss Act";
                case BoardTileType.Treasure: return "Peti Arsip";
                case BoardTileType.Merchant: return "Mordecai (Toko)";
                case BoardTileType.Rest: return "Perapian (Istirahat)";
                case BoardTileType.Shrine: return "Patung Malaikat";
                case BoardTileType.Event: return "Manuskrip Misteri";
                case BoardTileType.Trap: return "Jebakan";
                default: return t.ToString();
            }
        }

        public static string TileDesc(BoardTileType t)
        {
            switch (t)
            {
                case BoardTileType.Start: return "Titik awal ekspedisi.";
                case BoardTileType.Enemy: return "Pertarungan melawan monster biasa Act ini.";
                case BoardTileType.Elite: return "Musuh kuat. Langkahmu berhenti di sini.";
                case BoardTileType.Boss: return "Boss Act. Kalahkan untuk membuka Act berikutnya.";
                case BoardTileType.Treasure: return "Gold dan Focus.";
                case BoardTileType.Merchant: return "Beli senjata, aksesori, dan ramuan.";
                case BoardTileType.Rest: return "Pulihkan 45% HP dan 50% MP.";
                case BoardTileType.Shrine: return "Focus penuh (Limit siap) dan Max MP +5.";
                case BoardTileType.Event: return "Fragmen algoritma kuno: EXP dan Gold.";
                case BoardTileType.Trap: return "Lantai runtuh: -10% HP.";
                default: return "";
            }
        }

        public void ResolveBoardTileEvent(BoardTileNode tile)
        {
            if (tile == null) return;
            var stage = GrimoireDatabase.Stages[currentAct];

            if (tile.cleared && tile.type != BoardTileType.Merchant && tile.type != BoardTileType.Rest && tile.type != BoardTileType.Shrine)
            {
                boardBannerMessage = "Petak ini sudah dibersihkan. Lempar dadu lagi untuk melanjutkan.";
                return;
            }

            switch (tile.type)
            {
                case BoardTileType.Enemy:
                {
                    string mId = stage.regularEnemies[UnityEngine.Random.Range(0, stage.regularEnemies.Length)];
                    StartBattleEncounter(mId, false, tile);
                    break;
                }
                case BoardTileType.Elite:
                {
                    string mId = stage.eliteEnemies[UnityEngine.Random.Range(0, stage.eliteEnemies.Length)];
                    StartBattleEncounter(mId, false, tile);
                    break;
                }
                case BoardTileType.Boss:
                {
                    StartBattleEncounter(stage.bossId, true, tile);
                    break;
                }
                case BoardTileType.Treasure:
                {
                    tile.cleared = true;
                    int foundGold = UnityEngine.Random.Range(45, 95) * currentAct;
                    gold += foundGold;
                    focus = Mathf.Min(100, focus + 25);
                    GrimoireAudioManager.Instance?.PlaySfx("coin");
                    boardBannerMessage = $"{{#ffd86a}}PETI ARSIP KUNO!{{/}} +{foundGold} Gold dan +25 Focus.";
                    break;
                }
                case BoardTileType.Rest:
                {
                    tile.cleared = true;
                    int recHp = Mathf.RoundToInt(maxHp * 0.45f);
                    int recMp = Mathf.RoundToInt(maxMp * 0.50f);
                    hp = Mathf.Min(maxHp, hp + recHp);
                    mp = Mathf.Min(maxMp, mp + recMp);
                    _pStatus.Remove("poison");
                    GrimoireAudioManager.Instance?.PlaySfx("heal");
                    boardBannerMessage = $"{{#8ae05a}}PERAPIAN ARSIP:{{/}} Aegis beristirahat. +{recHp} HP, +{recMp} MP, racun hilang.";
                    break;
                }
                case BoardTileType.Shrine:
                {
                    tile.cleared = true;
                    focus = 100;
                    maxMp += 5;
                    mp = maxMp;
                    GrimoireAudioManager.Instance?.PlaySfx("magic");
                    boardBannerMessage = "{#7fd8ff}PATUNG MALAIKAT:{/} Focus penuh - LIMIT BREAK siap! Max MP +5.";
                    break;
                }
                case BoardTileType.Merchant:
                {
                    GrimoireAudioManager.Instance?.PlaySfx("coin");
                    boardBannerMessage = "Mordecai si Pandai Besi membuka lapaknya.";
                    _shopTab = 0;
                    activeModal = ModalOverlay.ShopInventory;
                    break;
                }
                case BoardTileType.Trap:
                {
                    tile.cleared = true;
                    int dmg = Mathf.Max(5, Mathf.RoundToInt(maxHp * 0.10f));
                    hp = Mathf.Max(1, hp - dmg);
                    GrimoireAudioManager.Instance?.PlaySfx("hit");
                    Shake(4f, 0.2f);
                    boardBannerMessage = $"{{#f05a50}}JEBAKAN!{{/}} Lantai indeks runtuh: -{dmg} HP.";
                    break;
                }
                case BoardTileType.Event:
                default:
                {
                    tile.cleared = true;
                    int expGain = 30 * currentAct;
                    GainExpAndGold(expGain, 35 * currentAct);
                    GrimoireAudioManager.Instance?.PlaySfx("levelup");
                    boardBannerMessage = $"{{#d0a0ff}}MANUSKRIP TERSEMBUNYI:{{/}} fragmen algoritma kuno (+{expGain} EXP, +{35 * currentAct} Gold).";
                    break;
                }
            }
        }

        // =====================================================================
        // ECONOMY
        // =====================================================================
        private bool GainExpAndGold(int expGain, int goldGain)
        {
            bool leveled = false;
            playerExp += expGain;
            gold += goldGain;
            while (playerExp >= expToNext && playerLevel < 10)
            {
                playerExp -= expToNext;
                playerLevel++;
                expToNext = Mathf.RoundToInt(expToNext * 1.35f);
                maxHp += 18;
                hp = maxHp;
                maxMp += 10;
                mp = maxMp;
                baseAtk += 4;
                baseDef += 2;
                baseSpd += 1;
                leveled = true;
                GrimoireAudioManager.Instance?.PlaySfx("levelup");
            }
            return leveled;
        }

        public bool BuyItem(string id)
        {
            if (!GrimoireDatabase.Items.TryGetValue(id, out var it)) return false;
            if (gold < it.price) { GrimoireAudioManager.Instance?.PlaySfx("error"); return false; }
            gold -= it.price;
            if (it.kind == "consumable")
            {
                consumables[id] = (consumables.TryGetValue(id, out int q) ? q : 0) + 1;
            }
            else
            {
                if (!ownedEquipment.Contains(id)) ownedEquipment.Add(id);
                EquipItem(id);
            }
            GrimoireAudioManager.Instance?.PlaySfx("coin");
            return true;
        }

        public void EquipItem(string id)
        {
            if (!GrimoireDatabase.Items.TryGetValue(id, out var it) || !ownedEquipment.Contains(id)) return;
            if (it.kind == "weapon") equippedWeapon = id;
            else if (it.kind == "accessory") equippedAccessory = equippedAccessory == id ? "" : id;
            GrimoireAudioManager.Instance?.PlaySfx("select");
        }
    }
}
