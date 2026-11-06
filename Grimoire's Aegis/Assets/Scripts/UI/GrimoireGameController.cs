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
        AlgorithmicChallenge, // the question is on screen
        Revealing,            // answer locked in: the attack plays out before anything is said
        TurnResolution,       // short "benar / kurang tepat" card
        MercyIntervention,
        Animating,            // input locked while a sequence plays
        Analyze,              // monster info card (free, from clicking the nameplate)
        BattleWon
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
    /// Game state + adventure board. Battle rules: GrimoireBattle.cs. Story/tutorial: GrimoireDialog.cs.
    /// Drawing: GrimoireScreens.cs / GrimoireModals.cs (partial class).
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
            { "pot_hp", 3 }, { "pot_mp", 2 }, { "scroll_clarity", 1 }
        };

        // --- Difficulty & accessibility (GDD §12, §17) ---
        public Difficulty difficulty = Difficulty.Medium;
        public float timerMultiplier = 1f;
        public bool accessibilityNoShake = false;
        public bool ddaEnabled = true;
        public bool runStarted = false;

        // --- DDA & Learning Telemetry State ---
        public string ddaTier = "Standard";
        public int consecutiveCorrect = 0;
        public int consecutiveWrong = 0;
        public Dictionary<string, int> tpFailStreaks = new Dictionary<string, int>();
        public List<TelemetryRecord> telemetryLog = new List<TelemetryRecord>();
        public string lastExportedCsvPath = "";
        public readonly HashSet<string> monstersSeen = new HashSet<string>();
        public readonly HashSet<string> monstersAnalyzed = new HashSet<string>();
        public readonly HashSet<string> codexOpened = new HashSet<string>();

        // --- Adventure board ---
        public int currentAct = 1;
        public AdventureMap map;
        public int currentNode;
        private int _prevNode = -1;
        public int stepsRemaining = 0;
        public int diceValue = 1;
        public int keysCollected = 0;
        public string boardMessage = "";
        private readonly List<int> _choices = new List<int>();   // next nodes at a fork
        private float _diceRollUntil;
        private float _actIntroStart;
        private bool _walking;

        // --- Board puzzle (GDD §4: no timer, no damage) ---
        [NonSerialized] public QuestionPayload puzzleQuestion;   // Unity would auto-create serializable public fields
        private MapNode _puzzleNode;
        private int _puzzleChosen = -1;   // -1 asking; otherwise the picked option
        public bool PuzzleOpen => puzzleQuestion != null;

        // --- View State ---
        public GameViewMode viewMode = GameViewMode.Title;
        public BattlePhase battlePhase = BattlePhase.PlayerCommand;
        public ModalOverlay activeModal = ModalOverlay.None;

        private readonly Dictionary<string, Texture2D> _texCache = new Dictionary<string, Texture2D>();

        // Computed stats (Crushed lowers DEF by 20%, GDD §9)
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

        public static string ActName(int a) => a == 1 ? "Perpustakaan Terkubur" : (a == 2 ? "Benteng Tulang" : "Menara Pecah-Belah");
        public static string ActTopic(int a) => a == 1 ? "Linear & Binary Search" : (a == 2 ? "Bubble, Selection & Insertion Sort" : "Merge Sort, Quick Sort & Big-O");
        public static string ActIntroText(int a) => a == 1
            ? "Rak-rak Katalog Agung tenggelam dalam debu dan tinta. Temukan 3 Kunci Indeks untuk membuka ruang The Blind Archivist."
            : a == 2 ? "Di bawah perpustakaan, tulang dan prasasti tersusun kacau. Kumpulkan 3 kunci dan hadapi The Abyss Golem."
            : "Menara ini terus terbelah dua. Di puncaknya, Entropy menunggu.";

        private void Awake()
        {
            if (Time.timeScale <= 0f) Time.timeScale = 1f;
            if (GetComponent<GrimoireAudioManager>() == null) gameObject.AddComponent<GrimoireAudioManager>();
            PixelUI.Tex = GetTex;
            PixelUI.Sfx = k => GrimoireAudioManager.Instance?.PlaySfx(k, k == "move" ? 0.35f : 1f);
        }

        private void Start()
        {
            if (Camera.main != null) Camera.main.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
            GrimoireAudioManager.Instance?.PlayMusic("music_title");
            map = AdventureMaps.Build(1);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) Time.timeScale = 1f;
            if (_scheduled.Count > 0)
            {
                var due = _scheduled.Where(e => Time.time >= e.Key).ToList();
                _scheduled.RemoveAll(e => Time.time >= e.Key);
                foreach (var e in due.OrderBy(e => e.Key))
                {
                    try { e.Value?.Invoke(); }
                    catch (Exception ex) { Debug.LogWarning($"[GrimoireGameController] Scheduled callback error: {ex}"); }
                }
            }
            if (viewMode == GameViewMode.Board && stepsRemaining > 0 && !_walking && _choices.Count == 0 && !DiceRolling && !PuzzleOpen && !DialogOpen && _scheduled.Count == 0)
            {
                ContinueWalk();
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
            _texCache[resourcePath] = tex;
            return tex;
        }

        // =====================================================================
        // NEW RUN
        // =====================================================================
        public void StartNewRun(Difficulty d)
        {
            if (Time.timeScale <= 0f) Time.timeScale = 1f;
            _scheduled.Clear();
            _dialog.Clear();
            _line = default;
            _dialogDone = null;
            difficulty = d;
            playerLevel = 1; playerExp = 0; expToNext = 100;
            maxHp = hp = 120; maxMp = mp = 60; focus = 0;
            baseAtk = 18; baseDef = 8; baseSpd = 12; gold = 120; logicFragments = 0;
            equippedWeapon = "blade_lv1"; equippedAccessory = "";
            ownedEquipment = new List<string> { "blade_lv1" };
            consumables = new Dictionary<string, int> { { "pot_hp", 3 }, { "pot_mp", 2 }, { "scroll_clarity", 1 } };
            ddaTier = d == Difficulty.Easy ? "Assisted" : "Standard";
            panicStacks = 0;
            _pStatus.Clear();
            _seenTips.Clear();
            runStarted = true;
            // Lyra sets the scene, then the Act I card
            viewMode = GameViewMode.ActIntro;
            currentAct = 1;
            map = AdventureMaps.Build(1);
            currentNode = map.startId;
            _prevNode = -1;
            stepsRemaining = 0;
            keysCollected = 0;
            _choices.Clear();
            _walking = false;
            puzzleQuestion = null;
            activeModal = ModalOverlay.None;
            _actIntroStart = Time.time + 999f;     // card waits behind the dialog
            StoryIntro(() => StartActExpedition(1));
        }

        // =====================================================================
        // ADVENTURE BOARD
        // =====================================================================
        public void StartActExpedition(int act)
        {
            currentAct = Mathf.Clamp(act, 1, 3);
            map = AdventureMaps.Build(currentAct);
            currentNode = map.startId;
            _prevNode = -1;
            stepsRemaining = 0;
            keysCollected = 0;
            _choices.Clear();
            _walking = false;
            puzzleQuestion = null;
            runStarted = true;
            activeModal = ModalOverlay.None;
            viewMode = GameViewMode.ActIntro;
            _actIntroStart = Time.time;
            boardMessage = "Lempar dadu untuk mulai berjalan.";
            GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey);
            _camPos = NodeWorld(map.Get(currentNode));
        }

        public void EnterBoardFromIntro()
        {
            viewMode = GameViewMode.Board;
            GrimoireAudioManager.Instance?.PlaySfx("select");
            if (currentAct == 1) TipBoard();
        }

        public bool DiceRolling => Time.time < _diceRollUntil;
        public bool AwaitingChoice => _choices.Count > 0;
        public IReadOnlyList<int> Choices => _choices;
        public bool BoardBusy => DiceRolling || _walking || stepsRemaining > 0 || PuzzleOpen || DialogOpen;

        public void RollExplorationDice()
        {
            if (BoardBusy) return;
            int bonus = TotalSpd >= 16 ? 1 : 0;
            diceValue = UnityEngine.Random.Range(1, 7);
            _diceRollUntil = Time.time + 0.6f;
            GrimoireAudioManager.Instance?.PlaySfx("dice");
            int steps = diceValue + bonus;
            Schedule(0.6f, () =>
            {
                stepsRemaining = steps;
                boardMessage = $"Dadu {{#ffd86a}}{diceValue}{{/}}{(bonus > 0 ? " +1" : "")}!";
                ContinueWalk();
            });
        }

        private IEnumerable<int> Forward(MapNode n) => n.neighbors.Where(id => id != _prevNode);

        /// <summary>Walks one node at a time; stops at forks (player picks), dead ends, blockers and the locked gate.</summary>
        private void ContinueWalk()
        {
            _choices.Clear();
            var cur = map.Get(currentNode);
            if (cur == null || stepsRemaining <= 0) { stepsRemaining = 0; _walking = false; if (cur != null) Land(cur); return; }

            var next = Forward(cur).ToList();
            if (next.Count == 0) next = cur.neighbors.ToList();      // dead end: turn around
            // the gate stops you unless all three keys are in hand
            var gate = next.Where(id => map.Get(id).type == NodeType.Gate && !map.Get(id).cleared).ToList();
            if (gate.Count > 0 && keysCollected < 3)
            {
                next.RemoveAll(id => gate.Contains(id));
                if (next.Count == 0)
                {
                    if (_walking)
                    {
                        _prevNode = gate[0];
                        stepsRemaining = 0;
                        _walking = false;
                        TipGateLocked();
                        return;
                    }
                    next = cur.neighbors.Where(id => !gate.Contains(id)).ToList();
                    if (next.Count == 0)
                    {
                        stepsRemaining = 0;
                        _walking = false;
                        TipGateLocked();
                        return;
                    }
                }
            }
            if (next.Count == 1)
            {
                _walking = true;
                Schedule(0.2f, () => StepTo(next[0]));
            }
            else
            {
                _walking = false;
                _choices.AddRange(next);
                boardMessage = $"Persimpangan! Pilih arah ({stepsRemaining} langkah lagi).";
            }
        }

        public void ChooseDirection(int nodeId)
        {
            if (!_choices.Contains(nodeId)) return;
            _choices.Clear();
            _walking = true;
            StepTo(nodeId);
        }

        /// <summary>Arrow keys at a fork.</summary>
        public void ChooseDirection(int dx, int dy)
        {
            var cur = map.Get(currentNode);
            foreach (int id in _choices.ToList())
            {
                var n = map.Get(id);
                if (n.gx - cur.gx == dx && n.gy - cur.gy == dy) { ChooseDirection(id); return; }
            }
        }

        private void StepTo(int id)
        {
            var node = map.Get(id);
            if (node == null) { stepsRemaining = 0; _walking = false; return; }
            _prevNode = currentNode;
            currentNode = id;
            stepsRemaining = Mathf.Max(0, stepsRemaining - 1);
            _tokenHopStart = Time.time;
            node.visited = true;
            map.RevealAround(id, 2);
            GrimoireAudioManager.Instance?.PlaySfx("step");

            if (node.type == NodeType.Gate && !node.cleared && keysCollected >= 3)
            {
                node.cleared = true;          // three keys: it opens as you walk in
                stepsRemaining = 0;
                _walking = false;
                GrimoireAudioManager.Instance?.PlaySfx("levelup");
                Shake(3f, 0.3f);
                Schedule(0.1f, TipGateOpen);
                return;
            }
            if (node.Blocks)
            {
                stepsRemaining = 0;
                _walking = false;
                Schedule(0.3f, () => Trigger(node));
                return;
            }
            Schedule(0.22f, ContinueWalk);
        }

        /// <summary>Things that stop you on the way: monsters, keys, the boss.</summary>
        private void Trigger(MapNode node)
        {
            switch (node.type)
            {
                case NodeType.Enemy:
                case NodeType.Elite:
                    StartBattleEncounter(node.monsterId, node.type == NodeType.Elite, node);
                    break;
                case NodeType.Boss:
                    BossIntro(node.monsterId, () => StartBattleEncounter(node.monsterId, true, node));
                    break;
                case NodeType.Puzzle:
                    OpenPuzzle(node);
                    break;
                case NodeType.KeyChest:
                    node.cleared = true;
                    GainKey("Peti tersembunyi berisi Kunci Indeks!");
                    break;
            }
        }

        /// <summary>Things that happen when the walk ends on a tile.</summary>
        private void Land(MapNode node)
        {
            if (node.cleared && node.type != NodeType.Merchant) { boardMessage = "Lempar dadu lagi."; return; }
            switch (node.type)
            {
                case NodeType.Treasure:
                {
                    node.cleared = true;
                    int g = UnityEngine.Random.Range(40, 80) * currentAct;
                    gold += g; focus = Mathf.Min(100, focus + 20);
                    GrimoireAudioManager.Instance?.PlaySfx("coin");
                    boardMessage = $"Peti harta: {{#ffd86a}}+{g} Gold{{/}}";
                    PopBoard($"+{g} Gold", PixelUI.Gold);
                    break;
                }
                case NodeType.Rest:
                {
                    node.cleared = true;
                    hp = Mathf.Min(maxHp, hp + Mathf.RoundToInt(maxHp * 0.5f));
                    mp = Mathf.Min(maxMp, mp + Mathf.RoundToInt(maxMp * 0.5f));
                    _pStatus.Remove("poison");
                    GrimoireAudioManager.Instance?.PlaySfx("heal");
                    boardMessage = "Perapian: HP dan MP pulih.";
                    PopBoard("HP & MP pulih", PixelUI.Green);
                    break;
                }
                case NodeType.Shrine:
                {
                    node.cleared = true;
                    hp = maxHp; focus = Mathf.Min(100, focus + 30);
                    GrimoireAudioManager.Instance?.PlaySfx("magic");
                    boardMessage = "Patung Malaikat: HP penuh.";
                    PopBoard("HP penuh!", PixelUI.Green);
                    break;
                }
                case NodeType.Merchant:
                    GrimoireAudioManager.Instance?.PlaySfx("coin");
                    _shopTab = 0;
                    activeModal = ModalOverlay.ShopInventory;
                    break;
                case NodeType.Trap:
                {
                    node.cleared = true;
                    int dmg = Mathf.Max(5, Mathf.RoundToInt(maxHp * 0.10f));
                    hp = Mathf.Max(1, hp - dmg);
                    GrimoireAudioManager.Instance?.PlaySfx("hit");
                    Shake(4f, 0.25f);
                    boardMessage = $"Jebakan! {{#f05a50}}-{dmg} HP{{/}}";
                    PopBoard($"-{dmg} HP", PixelUI.Red);
                    break;
                }
                case NodeType.Event:
                    node.cleared = true;
                    GainExpAndGold(20 * currentAct, 0);
                    LyraNote();
                    break;
                default:
                    boardMessage = "Lempar dadu lagi.";
                    break;
            }
        }

        private void GainKey(string why)
        {
            keysCollected = Mathf.Min(3, keysCollected + 1);
            GrimoireAudioManager.Instance?.PlaySfx("levelup");
            PopBoard("Kunci Indeks!", PixelUI.Gold);
            boardMessage = $"{why} ({keysCollected}/3)";
            TipKey();
        }

        /// <summary>After fleeing, step back so you are not standing on the monster.</summary>
        private void StepBackAfterFlee()
        {
            if (_prevNode >= 0) { int c = currentNode; currentNode = _prevNode; _prevNode = c; }
        }

        // --------------------------------------------------------------- puzzle
        private void OpenPuzzle(MapNode node)
        {
            _puzzleNode = node;
            var pool = GrimoireDatabase.Stages[currentAct].regularEnemies.SelectMany(m => GrimoireDatabase.Monsters[m].tpPool).Distinct().ToArray();
            puzzleQuestion = ProceduralQuestionEngine.GenerateQuestion(pool[UnityEngine.Random.Range(0, pool.Length)], "Assisted", TotalSpd, 0f);
            _puzzleChosen = -1;
            _popupStart = Time.time;
            TipPuzzle();
        }

        public void AnswerPuzzle(int i)
        {
            if (puzzleQuestion == null || _puzzleChosen >= 0) return;
            _puzzleChosen = i;
            bool ok = i == puzzleQuestion.correctIndex;
            telemetryLog.Add(new TelemetryRecord
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), act = currentAct, monsterName = "Rak Terkunci",
                tp = puzzleQuestion.tp, bloom = puzzleQuestion.bloom, formatCode = puzzleQuestion.formatCode, ddaTier = "Puzzle",
                isCorrect = ok, defenceGrade = ok ? "PUZZLE OK" : "PUZZLE RETRY", responseTimeSec = Time.time - _popupStart,
                timeLimitSec = 0f, misconceptionTag = ok ? "-" : CleanMis(puzzleQuestion.misconceptionTags[i])
            });
            GrimoireAudioManager.Instance?.PlaySfx(ok ? "parry" : "error");
        }

        public void ClosePuzzle()
        {
            if (puzzleQuestion == null || _puzzleChosen < 0) return;
            bool ok = _puzzleChosen == puzzleQuestion.correctIndex;
            if (ok)
            {
                puzzleQuestion = null;
                _puzzleNode.cleared = true;
                GainKey("Rak terbuka, ada Kunci Indeks di dalamnya!");
            }
            else
            {
                OpenPuzzle(_puzzleNode);      // new question, no penalty
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
                maxHp += 18; hp = maxHp;
                maxMp += 10; mp = maxMp;
                baseAtk += 4; baseDef += 2; baseSpd += 1;
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
            if (it.kind == "consumable") consumables[id] = (consumables.TryGetValue(id, out int q) ? q : 0) + 1;
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
