using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GrimoireAegis.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrimoireAegis.UI
{
    public enum GameViewMode
    {
        Title,
        Board,
        Battle,
        Victory
    }

    public enum BattlePhase
    {
        PlayerCommand,
        SpellSelect,
        ItemSelect,
        AlgorithmicChallenge, // Used for both Spell Cast Verification & Active Defend Parry
        TurnResolution,
        MercyIntervention
    }

    public enum ModalOverlay
    {
        None,
        CodexBestiary,
        ShopInventory,
        TeacherAnalytics,
        Settings
    }

    public class GrimoireGameController : MonoBehaviour
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

        // --- DDA & Learning Telemetry State ---
        public string ddaTier = "Standard"; // "Assisted", "Standard", "Mastery"
        public int consecutiveCorrect = 0;
        public int consecutiveWrong = 0;
        public Dictionary<string, int> tpFailStreaks = new Dictionary<string, int>();
        public List<TelemetryRecord> telemetryLog = new List<TelemetryRecord>();
        public string lastExportedCsvPath = "";

        // --- Labyrinth Board State ---
        public int currentAct = 1;
        public LabyrinthBoardData board;
        public int currentTileId = 0;
        public int diceValue = 1;
        public int stepsRemaining = 0;
        public bool forceGoldenDie = false;
        public string boardBannerMessage = "Gulir dadu [SPACE / ROLL DICE] untuk menjelajahi labirin perpustakaan!";

        // --- Turn-Based Battle State ---
        public GameViewMode viewMode = GameViewMode.Title;
        public BattlePhase battlePhase = BattlePhase.PlayerCommand;
        public ModalOverlay activeModal = ModalOverlay.None;

        public MonsterDef activeMonster;
        public int monsterMaxHp = 100;
        public int monsterHp = 100;
        public int bossPhase = 1;
        public bool isBossEncounter = false;
        public bool aegisCharmUsedThisBattle = false;

        // Player Status Effects in Battle
        public int poisonTurns = 0;
        public int silenceTurns = 0;
        public int tornPageTurns = 0;

        // Active Question / Defend State
        public bool isChallengeForSpell = false;
        public SpellDef pendingSpell = null;
        public QuestionPayload currentQuestion = null;
        public float questionTimer = 0f;
        public float questionMaxTime = 12f;
        public List<int> eliminatedOptions = new List<int>();
        public bool hintUsedOnQuestion = false;
        public int obscuredOptionIdx = -1;
        public string lastResolutionTitle = "";
        public string lastResolutionDetail = "";
        public Color lastResolutionColor = Color.white;
        public CodexEntry mercyCodexEntry = null;

        // Combat Log
        private readonly List<string> _combatLog = new List<string>();
        private Vector2 _codexScroll;
        private Vector2 _shopScroll;
        private Vector2 _telemetryScroll;
        private int _codexTab = 0; // 0 = 12 TP Codex, 1 = 13 Bestiary

        // --- Pixel-Art Texture Cache ---
        private readonly Dictionary<string, Texture2D> _texCache = new Dictionary<string, Texture2D>();
        private Texture2D _whiteTex;
        private GUIStyle _pixelHeaderStyle;
        private GUIStyle _pixelBodyStyle;
        private GUIStyle _pixelSmallStyle;
        private GUIStyle _pixelBtnStyle;
        private GUIStyle _pixelBadgeStyle;
        private bool _stylesInitialized;

        // Computed Total Stats
        public int TotalAtk => baseAtk + (GrimoireDatabase.Items.TryGetValue(equippedWeapon, out var w) ? w.atkBonus : 0);
        public int TotalDef => baseDef + (GrimoireDatabase.Items.TryGetValue(equippedAccessory, out var a) ? a.defBonus : 0);
        public int TotalSpd => baseSpd + (GrimoireDatabase.Items.TryGetValue(equippedAccessory, out var a) ? a.spdBonus : 0);
        public float AccessoryTimerBonus => GrimoireDatabase.Items.TryGetValue(equippedAccessory, out var a) ? a.timerBonus : 0f;

        private void Awake()
        {
            if (GetComponent<GrimoireAudioManager>() == null)
            {
                gameObject.AddComponent<GrimoireAudioManager>();
            }
        }

        private void Start()
        {
            // Set Camera background to dark obsidian
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = new Color(0.05f, 0.05f, 0.09f);
            }
            GrimoireAudioManager.Instance?.PlayMusic("music_title");
            GenerateActBoard(1);
        }

        private void Update()
        {
            // Handle Active Question Countdown Timer
            if (viewMode == GameViewMode.Battle && battlePhase == BattlePhase.AlgorithmicChallenge && activeModal == ModalOverlay.None)
            {
                questionTimer -= Time.deltaTime;
                if (questionTimer <= 0f)
                {
                    questionTimer = 0f;
                    SubmitAnswer(-1); // Timeout
                }
            }

            // Keyboard Shortcuts (New Input System safe check)
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.cKey.wasPressedThisFrame)
                {
                    ToggleModal(ModalOverlay.CodexBestiary);
                }
                else if (kb.tKey.wasPressedThisFrame)
                {
                    ToggleModal(ModalOverlay.TeacherAnalytics);
                }
                else if (kb.iKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame)
                {
                    ToggleModal(ModalOverlay.ShopInventory);
                }
                else if (kb.escapeKey.wasPressedThisFrame)
                {
                    if (activeModal != ModalOverlay.None) activeModal = ModalOverlay.None;
                }
                else if (viewMode == GameViewMode.Board && activeModal == ModalOverlay.None && kb.spaceKey.wasPressedThisFrame)
                {
                    if (stepsRemaining == 0) RollExplorationDice();
                }
                else if (viewMode == GameViewMode.Battle && battlePhase == BattlePhase.AlgorithmicChallenge && activeModal == ModalOverlay.None)
                {
                    if (kb.digit1Key.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) SubmitAnswer(0);
                    else if (kb.digit2Key.wasPressedThisFrame || kb.bKey.wasPressedThisFrame) SubmitAnswer(1);
                    else if (kb.digit3Key.wasPressedThisFrame || kb.cKey.wasPressedThisFrame) SubmitAnswer(2);
                    else if (kb.digit4Key.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) SubmitAnswer(3);
                    else if (kb.hKey.wasPressedThisFrame) UseScholarHint();
                }
            }
        }

        private void ToggleModal(ModalOverlay target)
        {
            GrimoireAudioManager.Instance?.PlaySfx("select");
            activeModal = (activeModal == target) ? ModalOverlay.None : target;
        }

        // =====================================================================
        // PIXEL ART TEXTURE LOADER (STRICT POINT FILTERING)
        // =====================================================================
        private Texture2D GetTex(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return _whiteTex;
            if (_texCache.TryGetValue(resourcePath, out Texture2D tex) && tex != null) return tex;

            tex = Resources.Load<Texture2D>("Gen/" + resourcePath);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                _texCache[resourcePath] = tex;
                return tex;
            }
            return _whiteTex;
        }

        private void EnsureStyles()
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(1, 1);
                _whiteTex.SetPixel(0, 0, Color.white);
                _whiteTex.Apply();
            }

            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _pixelHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            _pixelHeaderStyle.normal.textColor = new Color(1.0f, 0.93f, 0.72f);

            _pixelBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true
            };
            _pixelBodyStyle.normal.textColor = new Color(0.92f, 0.93f, 0.96f);

            _pixelSmallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true
            };
            _pixelSmallStyle.normal.textColor = new Color(0.85f, 0.88f, 0.95f);

            _pixelBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _pixelBtnStyle.normal.textColor = new Color(1.0f, 0.96f, 0.84f);

            _pixelBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _pixelBadgeStyle.normal.textColor = Color.white;
        }

        // =====================================================================
        // BOARD EXPLORATION & DICE LOGIC (LOST IN MULTIVERSE RPG STYLE)
        // =====================================================================
        public void StartActExpedition(int act)
        {
            currentAct = Mathf.Clamp(act, 1, 3);
            GenerateActBoard(currentAct);
            viewMode = GameViewMode.Board;
            activeModal = ModalOverlay.None;

            var stage = GrimoireDatabase.Stages[currentAct];
            GrimoireAudioManager.Instance?.PlayMusic(stage.musicKey);
            GrimoireAudioManager.Instance?.PlaySfx("levelup");
            boardBannerMessage = $"{stage.title} — {stage.intro}";
        }

        public void GenerateActBoard(int act)
        {
            board = LabyrinthBoardGenerator.Generate(act);
            currentTileId = board.startId;
            stepsRemaining = 0;
            diceValue = 1;
            board.RevealAround(currentTileId, 1);
        }

        public void RollExplorationDice()
        {
            if (stepsRemaining > 0) return;

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

            stepsRemaining = diceValue + spdDiceBonus;
            GrimoireAudioManager.Instance?.PlaySfx("dice");
            boardBannerMessage = spdDiceBonus > 0
                ? $"Dadu menunjukkan [{diceValue}] + Bonus SPD (+{spdDiceBonus}) = {stepsRemaining} langkah! Klik petak tetangga yang menyala."
                : $"Dadu menunjukkan [{diceValue}] langkah! Klik petak tetangga yang terhubung untuk bergerak.";
        }

        public void StepToNeighborTile(int targetTileId)
        {
            if (board == null || stepsRemaining <= 0) return;
            var curTile = board.GetTile(currentTileId);
            if (curTile == null || !curTile.neighbors.Contains(targetTileId)) return;

            currentTileId = targetTileId;
            stepsRemaining--;
            var landed = board.GetTile(currentTileId);
            landed.visited = true;
            board.RevealAround(currentTileId, 1);
            GrimoireAudioManager.Instance?.PlaySfx("step");

            // If Boss tile or steps finished, trigger tile event immediately!
            if (landed.type == BoardTileType.Boss || landed.type == BoardTileType.Elite || stepsRemaining == 0)
            {
                stepsRemaining = 0;
                ResolveBoardTileEvent(landed);
            }
            else
            {
                boardBannerMessage = $"Melangkah ke petak #{landed.id} ({landed.type}). Sisa langkah: {stepsRemaining} (atau klik [STOP & INTERACT] di bawah).";
            }
        }

        public void ResolveBoardTileEvent(BoardTileNode tile)
        {
            if (tile == null) return;
            var stage = GrimoireDatabase.Stages[currentAct];

            if (tile.cleared && tile.type != BoardTileType.Merchant && tile.type != BoardTileType.Rest && tile.type != BoardTileType.Shrine)
            {
                boardBannerMessage = "Petak ini telah dibersihkan. Gulir dadu lagi untuk melanjutkan perjalanan.";
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
                    boardBannerMessage = $"PETI ARSIP KUNO! Kamu menemukan +{foundGold} Gold dan +25 Focus!";
                    break;
                }
                case BoardTileType.Rest:
                {
                    tile.cleared = true;
                    int recHp = Mathf.RoundToInt(maxHp * 0.45f);
                    int recMp = Mathf.RoundToInt(maxMp * 0.50f);
                    hp = Mathf.Min(maxHp, hp + recHp);
                    mp = Mathf.Min(maxMp, mp + recMp);
                    GrimoireAudioManager.Instance?.PlaySfx("heal");
                    boardBannerMessage = $"SANCTUARY REST: Aegis beristirahat di perapian arsip. Pulih +{recHp} HP & +{recMp} MP!";
                    break;
                }
                case BoardTileType.Shrine:
                {
                    tile.cleared = true;
                    focus = 100;
                    maxMp += 5;
                    mp = maxMp;
                    GrimoireAudioManager.Instance?.PlaySfx("magic");
                    boardBannerMessage = "SHRINE OF THE GREAT INDEX: Fokus terisi penuh (100% LIMIT BREAK READY) & Max MP +5!";
                    break;
                }
                case BoardTileType.Merchant:
                {
                    GrimoireAudioManager.Instance?.PlaySfx("coin");
                    boardBannerMessage = "WANDERING SCHOLAR MERCHANT: Tukarkan Gold dengan Index Blade, Aksesori, atau Ramuan!";
                    activeModal = ModalOverlay.ShopInventory;
                    break;
                }
                case BoardTileType.Trap:
                {
                    tile.cleared = true;
                    int dmg = Mathf.Max(5, Mathf.RoundToInt(maxHp * 0.10f));
                    hp = Mathf.Max(1, hp - dmg);
                    GrimoireAudioManager.Instance?.PlaySfx("hit");
                    boardBannerMessage = $"JEBAKAN INDEKS RUSAK! Lantai runtuh menggores -{dmg} HP. Tetap waspada!";
                    break;
                }
                case BoardTileType.Event:
                default:
                {
                    tile.cleared = true;
                    int expGain = 30 * currentAct;
                    GainExpAndGold(expGain, 35 * currentAct);
                    GrimoireAudioManager.Instance?.PlaySfx("levelup");
                    boardBannerMessage = $"MANUSKRIP TERSEMBUNYI: Aegis mempelajari fragmen algoritma kuno (+{expGain} EXP & +{35 * currentAct} Gold)!";
                    break;
                }
            }
        }

        // =====================================================================
        // TURN-BASED BATTLE & ACTIVE PARRY ENGINE
        // =====================================================================
        public void StartBattleEncounter(string monsterId, bool isBoss, BoardTileNode sourceTile = null)
        {
            if (!GrimoireDatabase.Monsters.TryGetValue(monsterId, out MonsterDef def))
            {
                def = GrimoireDatabase.Monsters["corrupted_slime"];
            }

            activeMonster = def;
            monsterMaxHp = def.hp;
            monsterHp = def.hp;
            bossPhase = 1;
            isBossEncounter = isBoss || def.role == "Boss" || def.role == "Final Boss" || def.role == "Mid-Boss";
            aegisCharmUsedThisBattle = false;

            poisonTurns = 0;
            silenceTurns = 0;
            tornPageTurns = 0;

            viewMode = GameViewMode.Battle;
            battlePhase = BattlePhase.PlayerCommand;
            _combatLog.Clear();
            AddCombatLog($"[ENCOUNTER] {def.name} ({def.role}) menghadang! HP: {def.hp} | ATK: {def.atk} | DEF: {def.def}");
            AddCombatLog($"[MECHANIC] {def.mechanicDesc}");

            GrimoireAudioManager.Instance?.PlayMusic("music_battle");
            GrimoireAudioManager.Instance?.PlaySfx("select");
        }

        private void AddCombatLog(string msg)
        {
            _combatLog.Insert(0, msg);
            if (_combatLog.Count > 12) _combatLog.RemoveAt(_combatLog.Count - 1);
        }

        public void ExecuteBasicAttack()
        {
            GrimoireAudioManager.Instance?.PlaySfx("hit");

            float armorMitigation = activeMonster.id == "the_abyss_golem" ? 0.30f : 1.0f;
            int rawDmg = Mathf.Max(4, Mathf.RoundToInt((TotalAtk * 1.0f - activeMonster.def * 0.45f) * armorMitigation));
            monsterHp = Mathf.Max(0, monsterHp - rawDmg);
            focus = Mathf.Min(100, focus + 12);

            string armorNote = activeMonster.id == "the_abyss_golem" ? " (Heavy Stone Armor menahan 70% serangan fisik!)" : "";
            AddCombatLog($"Aegis menebas dengan Index Blade -> {rawDmg} DMG! (+12 Focus){armorNote}");

            CheckBossPhaseTransition();
            if (monsterHp <= 0)
            {
                HandleBattleVictory();
                return;
            }

            // Launch Enemy Attack -> Player Active Defend!
            BeginAlgorithmicChallenge(false, null);
        }

        public void BeginSpellCast(SpellDef spell)
        {
            if (silenceTurns > 0 && spell.focusCost == 0)
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                AddCombatLog("Status SILENCE aktif! Mantra biasa terkunci, gunakan Basic Slash atau Item pembersih.");
                return;
            }

            int actualMpCost = GetEffectiveMpCost(spell);
            if (mp < actualMpCost || focus < spell.focusCost)
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                AddCombatLog("MP atau Focus belum mencukupi untuk merapal mantra ini!");
                return;
            }

            mp -= actualMpCost;
            focus = Mathf.Max(0, focus - spell.focusCost);
            GrimoireAudioManager.Instance?.PlaySfx("magic");
            BeginAlgorithmicChallenge(true, spell);
        }

        public int GetEffectiveMpCost(SpellDef spell)
        {
            if (equippedAccessory == "acc_ring_comp")
            {
                return Mathf.Max(0, Mathf.RoundToInt(spell.mpCost * 0.80f));
            }
            return spell.mpCost;
        }

        public void BeginAlgorithmicChallenge(bool forSpell, SpellDef spell)
        {
            isChallengeForSpell = forSpell;
            pendingSpell = spell;
            eliminatedOptions.Clear();
            hintUsedOnQuestion = false;

            string targetTp;
            if (forSpell && spell != null && !string.IsNullOrEmpty(spell.tpCode))
            {
                targetTp = spell.tpCode;
            }
            else if (activeMonster != null && activeMonster.tpPool != null && activeMonster.tpPool.Length > 0)
            {
                targetTp = activeMonster.tpPool[UnityEngine.Random.Range(0, activeMonster.tpPool.Length)];
            }
            else
            {
                targetTp = "TP-01";
            }

            currentQuestion = ProceduralQuestionEngine.GenerateQuestion(targetTp, ddaTier, TotalSpd, AccessoryTimerBonus);
            questionMaxTime = currentQuestion.timeLimitSec;
            questionTimer = questionMaxTime;

            // Torn Page debuff obscures 1 option text unless Cloak of the Archivist is equipped
            obscuredOptionIdx = -1;
            if (tornPageTurns > 0 && equippedAccessory != "acc_cloak")
            {
                obscuredOptionIdx = UnityEngine.Random.Range(0, 4);
            }

            battlePhase = BattlePhase.AlgorithmicChallenge;
        }

        public void UseScholarHint()
        {
            if (currentQuestion == null || hintUsedOnQuestion) return;
            if (mp < 10)
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                AddCombatLog("MP tidak cukup untuk menggunakan Scholar's Hint (butuh 10 MP)!");
                return;
            }

            mp -= 10;
            hintUsedOnQuestion = true;
            GrimoireAudioManager.Instance?.PlaySfx("magic");

            int toEliminate = equippedAccessory == "acc_monocle" ? 2 : 1;
            for (int i = 0; i < 4 && eliminatedOptions.Count < toEliminate; i++)
            {
                if (i != currentQuestion.correctIndex && !eliminatedOptions.Contains(i))
                {
                    eliminatedOptions.Add(i);
                }
            }
        }

        public void SubmitAnswer(int chosenIndex)
        {
            if (currentQuestion == null) return;

            float elapsed = Mathf.Clamp(questionMaxTime - questionTimer, 0.1f, questionMaxTime);
            float timeRatio = elapsed / Mathf.Max(0.1f, questionMaxTime);
            bool isCorrect = (chosenIndex == currentQuestion.correctIndex);

            string tp = currentQuestion.tp;
            if (!tpFailStreaks.ContainsKey(tp)) tpFailStreaks[tp] = 0;

            string grade;
            if (isCorrect)
            {
                consecutiveCorrect++;
                consecutiveWrong = 0;
                tpFailStreaks[tp] = 0;

                if (consecutiveCorrect >= 3)
                {
                    ddaTier = ddaTier == "Assisted" ? "Standard" : "Mastery";
                }

                if (timeRatio <= 0.35f) grade = "PERFECT PARRY";
                else if (timeRatio <= 0.70f) grade = "SOLID BLOCK";
                else grade = "GRAZED GUARD";
            }
            else
            {
                consecutiveWrong++;
                consecutiveCorrect = 0;
                tpFailStreaks[tp]++;

                if (consecutiveWrong >= 2)
                {
                    ddaTier = ddaTier == "Mastery" ? "Standard" : "Assisted";
                }
                grade = "GUARD BROKEN";
            }

            string misTag = (chosenIndex >= 0 && chosenIndex < 4)
                ? currentQuestion.misconceptionTags[chosenIndex]
                : "Timeout: Waktu Habis Sebelum Menjawab";

            telemetryLog.Add(new TelemetryRecord
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                act = currentAct,
                monsterName = activeMonster != null ? activeMonster.name : "Training",
                tp = tp,
                bloom = currentQuestion.bloom,
                formatCode = currentQuestion.formatCode,
                ddaTier = ddaTier,
                isCorrect = isCorrect,
                defenceGrade = grade,
                responseTimeSec = elapsed,
                timeLimitSec = questionMaxTime,
                misconceptionTag = misTag
            });

            if (isChallengeForSpell && pendingSpell != null)
            {
                ResolveSpellCastOutcome(isCorrect, grade, elapsed, misTag);
            }
            else
            {
                ResolveActiveDefendOutcome(isCorrect, grade, elapsed, misTag);
            }
        }

        private void ResolveSpellCastOutcome(bool isCorrect, string grade, float elapsed, string misTag)
        {
            var spell = pendingSpell;
            if (isCorrect)
            {
                GrimoireAudioManager.Instance?.PlaySfx("magic");
                float speedBonus = grade == "PERFECT PARRY" ? 1.20f : 1.0f;
                float defFactor = spell.id == "order_restored" ? 0f : (spell.id == "binary_pierce" ? 0.5f : 1.0f);
                int dmg = Mathf.Max(8, Mathf.RoundToInt((TotalAtk * spell.multiplier * speedBonus) - (activeMonster.def * 0.4f * defFactor)));

                monsterHp = Mathf.Max(0, monsterHp - dmg);
                focus = Mathf.Min(100, focus + (grade == "PERFECT PARRY" ? 25 : 15));

                if (spell.id == "bubble_shield")
                {
                    hp = Mathf.Min(maxHp, hp + 25);
                    poisonTurns = 0;
                    tornPageTurns = 0;
                }
                else if (spell.id == "order_restored")
                {
                    hp = Mathf.Min(maxHp, hp + Mathf.RoundToInt(maxHp * 0.30f));
                    poisonTurns = 0;
                    silenceTurns = 0;
                    tornPageTurns = 0;
                }

                lastResolutionTitle = $"MANTRA SUKSES: {spell.name.ToUpper()} ({grade} — {elapsed:F1}s)";
                lastResolutionDetail = $"Serangan algoritma menghantam {activeMonster.name} sebesar {dmg} DMG!\n\nPenjelasan Pedagogis ({currentQuestion.tp}):\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(0.35f, 0.92f, 0.55f);
                AddCombatLog($"[SPELL] {spell.name} sukses ({grade}) -> {dmg} DMG!");
            }
            else
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                int fizzleDmg = Mathf.Max(3, Mathf.RoundToInt(TotalAtk * 0.4f));
                monsterHp = Mathf.Max(0, monsterHp - fizzleDmg);

                if (activeMonster.id == "hydra_of_division")
                {
                    int regen = Mathf.RoundToInt(monsterMaxHp * 0.12f);
                    monsterHp = Mathf.Min(monsterMaxHp, monsterHp + regen);
                    AddCombatLog($"[HYDRA REGEN] Hydra of Division membelah diri & pulih +{regen} HP!");
                }

                lastResolutionTitle = $"MANTRA TERGANGGU (FIZZLE) — Diagnostik: {misTag}";
                lastResolutionDetail = $"Mantra hanya menghasilkan percikan {fizzleDmg} DMG.\n\nKoreksi Algoritma ({currentQuestion.tp}):\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(0.95f, 0.42f, 0.38f);
                AddCombatLog($"[FIZZLE] {spell.name} gagal ({misTag}) -> {fizzleDmg} DMG.");
            }

            CheckBossPhaseTransition();
            if (monsterHp <= 0)
            {
                HandleBattleVictory();
                return;
            }

            // Check Mercy Intervention if 3 consecutive fails on this TP
            if (!isCorrect && tpFailStreaks.TryGetValue(currentQuestion.tp, out int streak) && streak >= 3)
            {
                TriggerMercyGuidance(currentQuestion.tp);
                return;
            }

            battlePhase = BattlePhase.TurnResolution;
        }

        private void ResolveActiveDefendOutcome(bool isCorrect, string grade, float elapsed, string misTag)
        {
            int rawEnemyAtk = Mathf.Max(6, activeMonster.atk + (bossPhase - 1) * 4 - Mathf.RoundToInt(TotalDef * 0.55f));

            // Check Aegis Charm rescue
            if (grade == "GUARD BROKEN" && equippedAccessory == "acc_aegis_charm" && !aegisCharmUsedThisBattle)
            {
                aegisCharmUsedThisBattle = true;
                grade = "SOLID BLOCK";
                isCorrect = true;
                AddCombatLog("[AEGIS CHARM] Jimat suci aktif! Guard Broken diubah menjadi Solid Block (75% mitigasi)!");
            }

            if (grade == "PERFECT PARRY")
            {
                GrimoireAudioManager.Instance?.PlaySfx("parry");
                int counterDmg = Mathf.Max(10, Mathf.RoundToInt(TotalAtk * 1.5f - activeMonster.def * 0.3f));
                monsterHp = Mathf.Max(0, monsterHp - counterDmg);
                focus = Mathf.Min(100, focus + 25);

                lastResolutionTitle = $"PERFECT PARRY! (0 DMG Diterima | Counter {counterDmg} DMG! | {elapsed:F1}s)";
                lastResolutionDetail = $"Aegis menangkis di Zona Emas (<=35% waktu) dan melancarkan Counter-Strike 1.5x ATK ({counterDmg} DMG) + 25 Focus!\n\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(1.0f, 0.86f, 0.25f);
                AddCombatLog($"[PERFECT PARRY] 0 DMG & Counter-Attack {counterDmg} DMG! (+25 Focus)");
            }
            else if (grade == "SOLID BLOCK")
            {
                GrimoireAudioManager.Instance?.PlaySfx("guard");
                int dmgTaken = Mathf.Max(1, Mathf.RoundToInt(rawEnemyAtk * 0.25f));
                hp = Mathf.Max(1, hp - dmgTaken);
                focus = Mathf.Min(100, focus + 12);

                lastResolutionTitle = $"SOLID BLOCK! (75% Mitigasi -> Hanya {dmgTaken} DMG | {elapsed:F1}s)";
                lastResolutionDetail = $"Perisai Grimoire menahan 75% serangan {activeMonster.name} (+12 Focus).\n\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(0.38f, 0.85f, 0.95f);
                AddCombatLog($"[SOLID BLOCK] Menahan serangan, terima {dmgTaken} DMG (+12 Focus).");
            }
            else if (grade == "GRAZED GUARD")
            {
                GrimoireAudioManager.Instance?.PlaySfx("guard");
                int dmgTaken = Mathf.Max(2, Mathf.RoundToInt(rawEnemyAtk * 0.60f));
                hp = Mathf.Max(1, hp - dmgTaken);
                focus = Mathf.Min(100, focus + 6);

                lastResolutionTitle = $"GRAZED GUARD! (40% Mitigasi -> Terima {dmgTaken} DMG | {elapsed:F1}s)";
                lastResolutionDetail = $"Jawaban benar namun mendekati batas waktu; menahan 40% serangan.\n\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(0.92f, 0.75f, 0.32f);
                AddCombatLog($"[GRAZED GUARD] Terima {dmgTaken} DMG (+6 Focus).");
            }
            else
            {
                GrimoireAudioManager.Instance?.PlaySfx("hit");
                float critMult = activeMonster.id == "pivot_wraith" ? 1.35f : 1.0f;
                int dmgTaken = Mathf.Max(4, Mathf.RoundToInt(rawEnemyAtk * critMult));
                hp = Mathf.Max(0, hp - dmgTaken);

                ApplyMonsterStatusDebuff();

                lastResolutionTitle = $"GUARD BROKEN! (Terima {dmgTaken} DMG) — Diagnostik: {misTag}";
                lastResolutionDetail = $"Pertahanan terbuka! {activeMonster.name} menghantam sebesar {dmgTaken} DMG.\n\nKoreksi Algoritma ({currentQuestion.tp}):\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(0.96f, 0.35f, 0.35f);
                AddCombatLog($"[GUARD BROKEN] Terima {dmgTaken} DMG! ({misTag})");
            }

            // Tick status effects at end of enemy turn
            if (poisonTurns > 0)
            {
                int psnDmg = Mathf.Max(2, Mathf.RoundToInt(maxHp * 0.05f));
                hp = Mathf.Max(1, hp - psnDmg);
                poisonTurns--;
                AddCombatLog($"[POISON] Racun menguras -{psnDmg} HP ({poisonTurns} giliran tersisa).");
            }
            if (silenceTurns > 0) silenceTurns--;
            if (tornPageTurns > 0) tornPageTurns--;

            CheckBossPhaseTransition();
            if (monsterHp <= 0)
            {
                HandleBattleVictory();
                return;
            }

            if (hp <= 0)
            {
                // Scholar's Resilience: Revive at Sanctuary with Mercy guidance instead of harsh permadeath
                hp = Mathf.RoundToInt(maxHp * 0.5f);
                mp = Mathf.RoundToInt(maxMp * 0.5f);
                TriggerMercyGuidance(currentQuestion.tp);
                return;
            }

            if (!isCorrect && tpFailStreaks.TryGetValue(currentQuestion.tp, out int streak) && streak >= 3)
            {
                TriggerMercyGuidance(currentQuestion.tp);
                return;
            }

            battlePhase = BattlePhase.TurnResolution;
        }

        private void ApplyMonsterStatusDebuff()
        {
            if (activeMonster == null) return;
            if (activeMonster.statusEffect == "poison" && equippedAccessory != "acc_pendant")
            {
                poisonTurns = 3;
                AddCombatLog("[STATUS] Venomous Arachnid menyuntikkan POISON (3 giliran)!");
            }
            else if (activeMonster.statusEffect == "silence" && equippedAccessory != "acc_pendant")
            {
                silenceTurns = 2;
                AddCombatLog("[STATUS] Twin-Faced Librarian membisukan Grimoire: SILENCE (2 giliran)!");
            }
            else if (activeMonster.statusEffect == "torn_page" && equippedAccessory != "acc_cloak")
            {
                tornPageTurns = 2;
                AddCombatLog("[STATUS] Bookworm Horror merobek halaman: TORN PAGE (2 giliran)!");
            }
        }

        private void CheckBossPhaseTransition()
        {
            if (activeMonster == null) return;
            float ratio = (float)monsterHp / Mathf.Max(1, monsterMaxHp);
            if (activeMonster.id == "entropy_chaos_king")
            {
                if (ratio <= 0.33f && bossPhase < 3)
                {
                    bossPhase = 3;
                    AddCombatLog("[BOSS PHASE 3] ENTROPY MEMBUKA GRAND CATALOG! Divide & Conquer + Big-O diaktifkan!");
                }
                else if (ratio <= 0.66f && bossPhase < 2)
                {
                    bossPhase = 2;
                    AddCombatLog("[BOSS PHASE 2] ENTROPY MENGACAUKAN PRASASTI! Serangan meningkat!");
                }
            }
            else if (isBossEncounter && ratio <= 0.50f && bossPhase < 2)
            {
                bossPhase = 2;
                AddCombatLog($"[BOSS PHASE 2] {activeMonster.name} memasuki fase murka! Mekanik {activeMonster.statusEffect.ToUpper()} menguat!");
            }
        }

        private void TriggerMercyGuidance(string tp)
        {
            tpFailStreaks[tp] = 0;
            ddaTier = "Assisted";
            int healHp = Mathf.RoundToInt(maxHp * 0.25f);
            int healMp = Mathf.RoundToInt(maxMp * 0.30f);
            hp = Mathf.Min(maxHp, hp + healHp);
            mp = Mathf.Min(maxMp, mp + healMp);

            mercyCodexEntry = GrimoireDatabase.Codex.FirstOrDefault(c => c.tp == tp) ?? GrimoireDatabase.Codex[0];
            GrimoireAudioManager.Instance?.PlaySfx("heal");
            AddCombatLog($"[MERCY SYSTEM] Roh Archivist memulihkan +{healHp} HP, +{healMp} MP & membuka panduan {tp}!");
            battlePhase = BattlePhase.MercyIntervention;
        }

        private void HandleBattleVictory()
        {
            GrimoireAudioManager.Instance?.PlaySfx("victory");
            var tile = board?.GetTile(currentTileId);
            if (tile != null) tile.cleared = true;

            GainExpAndGold(activeMonster.exp, activeMonster.gold);

            if (activeMonster.id == "entropy_chaos_king")
            {
                viewMode = GameViewMode.Victory;
                GrimoireAudioManager.Instance?.PlayMusic("music_title");
                return;
            }

            if (activeMonster.id == "the_blind_archivist" && currentAct == 1)
            {
                StartActExpedition(2);
                boardBannerMessage = "BOSS ACT I DIKALAHKAN! Gerbang menuju ACT II: THE CATACOMBS OF CHAOS telah terbuka!";
                return;
            }
            if (activeMonster.id == "the_abyss_golem" && currentAct == 2)
            {
                StartActExpedition(3);
                boardBannerMessage = "BOSS ACT II DIKALAHKAN! Kamu menaiki ACT III: THE TOWER OF ENTROPY!";
                return;
            }

            viewMode = GameViewMode.Board;
            GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey);
            boardBannerMessage = $"KEMENANGAN! {activeMonster.name} dikalahkan (+{activeMonster.exp} EXP, +{activeMonster.gold} Gold)!";
        }

        private void GainExpAndGold(int expGain, int goldGain)
        {
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
                GrimoireAudioManager.Instance?.PlaySfx("levelup");
            }
        }

        public void UseConsumableItem(string itemId)
        {
            if (!consumables.TryGetValue(itemId, out int count) || count <= 0) return;
            if (!GrimoireDatabase.Items.TryGetValue(itemId, out ItemDef item)) return;

            consumables[itemId] = count - 1;
            GrimoireAudioManager.Instance?.PlaySfx("heal");

            if (item.healHp > 0) hp = Mathf.Min(maxHp, hp + item.healHp);
            if (item.healMp > 0) mp = Mathf.Min(maxMp, mp + item.healMp);

            if (itemId == "pot_hp" || itemId == "pot_elixir") poisonTurns = 0;
            if (itemId == "pot_mp" || itemId == "pot_elixir") silenceTurns = 0;
            if (itemId == "pot_elixir") tornPageTurns = 0;
            if (itemId == "scroll_clarity") focus = Mathf.Min(100, focus + 50);
            if (itemId == "golden_die") forceGoldenDie = true;

            if (viewMode == GameViewMode.Battle)
            {
                if (itemId == "smoke_scroll" && !isBossEncounter)
                {
                    viewMode = GameViewMode.Board;
                    GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey);
                    boardBannerMessage = "Aegis menggunakan Teleport Bookmark untuk mundur ke labirin dengan aman.";
                    return;
                }
                AddCombatLog($"[ITEM] Menggunakan {item.name}: {item.desc}");
                battlePhase = BattlePhase.PlayerCommand;
            }
        }

        // =====================================================================
        // MASTER PIXEL-ART GUI RENDERER
        // =====================================================================
        private void OnGUI()
        {
            EnsureStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            switch (viewMode)
            {
                case GameViewMode.Title:
                    DrawTitleScreen(sw, sh);
                    break;
                case GameViewMode.Board:
                    DrawLabyrinthBoardScreen(sw, sh);
                    break;
                case GameViewMode.Battle:
                    DrawBattleArenaScreen(sw, sh);
                    break;
                case GameViewMode.Victory:
                    DrawGrandVictoryScreen(sw, sh);
                    break;
            }

            if (activeModal != ModalOverlay.None)
            {
                DrawModalOverlay(sw, sh);
            }
        }

        private void DrawPixelPanel(Rect r, Color bgTint, Color borderCol)
        {
            GUI.color = bgTint;
            GUI.DrawTexture(r, _whiteTex);
            GUI.color = borderCol;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2), _whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 2, r.width, 2), _whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y, 2, r.height), _whiteTex);
            GUI.DrawTexture(new Rect(r.xMax - 2, r.y, 2, r.height), _whiteTex);
            GUI.color = Color.white;
        }

        private bool DrawPixelButton(Rect r, string label, Color bgCol, Color borderCol)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Color fill = hover ? Color.Lerp(bgCol, Color.white, 0.18f) : bgCol;
            DrawPixelPanel(r, fill, hover ? new Color(1f, 0.88f, 0.35f) : borderCol);
            return GUI.Button(r, label, _pixelBtnStyle);
        }

        // ---------------------------------------------------------------------
        // 1. TITLE SCREEN
        // ---------------------------------------------------------------------
        private void DrawTitleScreen(float sw, float sh)
        {
            GUI.DrawTexture(new Rect(0, 0, sw, sh), GetTex("bg/bg_title"), ScaleMode.StretchToFill);
            DrawPixelPanel(new Rect(0, 0, sw, sh), new Color(0.04f, 0.04f, 0.08f, 0.45f), Color.clear);

            float cardW = Mathf.Min(860f, sw - 40f);
            float cardH = Mathf.Min(540f, sh - 40f);
            Rect card = new Rect((sw - cardW) * 0.5f, (sh - cardH) * 0.5f, cardW, cardH);
            DrawPixelPanel(card, new Color(0.08f, 0.09f, 0.15f, 0.94f), new Color(0.92f, 0.76f, 0.22f));

            // Animated Hero & Final Boss Preview Sprites
            bool frame2 = (Mathf.FloorToInt(Time.time * 2.5f) % 2) == 1;
            Texture2D heroSprite = GetTex(frame2 ? "chars/player_idle2" : "chars/player_idle");
            Texture2D bossSprite = GetTex(frame2 ? "enemies/entropy_chaos_king_2" : "enemies/entropy_chaos_king");

            GUI.DrawTexture(new Rect(card.x + 35, card.y + 40, 128, 128), heroSprite, ScaleMode.ScaleToFit);
            GUI.DrawTexture(new Rect(card.xMax - 165, card.y + 30, 136, 136), bossSprite, ScaleMode.ScaleToFit);

            // Title Header
            var titleStyle = new GUIStyle(_pixelHeaderStyle) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(card.x + 160, card.y + 24, card.width - 320, 40), "GRIMOIRE'S AEGIS: THE SCHOLAR KNIGHT", titleStyle);

            var subStyle = new GUIStyle(_pixelSmallStyle) { fontSize = 14 };
            subStyle.normal.textColor = new Color(0.55f, 0.85f, 0.98f);
            GUI.Label(new Rect(card.x + 140, card.y + 64, card.width - 280, 28),
                "Educational Turn-Based Active-Parry & Labyrinth Board RPG — Searching & Sorting Algorithms (TP-01..TP-12)", subStyle);

            GUI.Label(new Rect(card.x + 170, card.y + 98, card.width - 340, 68),
                "Pulihkan The Great Index di Perpustakaan Agung Alexandria-Nova!\nJelajahi papan labirin dengan dadu, visualisasikan Array secara real-time, dan tangkis serangan 13 Monster Bestiary di Zona Emas!",
                new GUIStyle(_pixelBodyStyle) { alignment = TextAnchor.MiddleCenter });

            // Act Selector Cards
            float actY = card.y + 185;
            float actW = (card.width - 80) / 3f;
            for (int a = 1; a <= 3; a++)
            {
                var st = GrimoireDatabase.Stages[a];
                Rect ar = new Rect(card.x + 25 + (a - 1) * (actW + 15), actY, actW, 175);
                DrawPixelPanel(ar, new Color(0.12f, 0.13f, 0.22f, 0.96f), st.rimColor);
                GUI.DrawTexture(new Rect(ar.x + 6, ar.y + 6, ar.width - 12, 58), GetTex($"bg/bg_s{a}"), ScaleMode.StretchToFill);
                GUI.Label(new Rect(ar.x + 8, ar.y + 68, ar.width - 16, 24), st.title, new GUIStyle(_pixelSmallStyle) { normal = { textColor = st.rimColor } });
                GUI.Label(new Rect(ar.x + 8, ar.y + 90, ar.width - 16, 38), st.subtitle, _pixelSmallStyle);

                if (DrawPixelButton(new Rect(ar.x + 12, ar.yMax - 40, ar.width - 24, 30), $"MULAI ACT {a}", new Color(0.22f, 0.36f, 0.58f), st.rimColor))
                {
                    StartActExpedition(a);
                }
            }

            // Bottom Utility Buttons
            float btnY = card.yMax - 65;
            float bW = (card.width - 80) / 3f;
            if (DrawPixelButton(new Rect(card.x + 25, btnY, bW, 42), "GRIMOIRE CODEX & 13 BESTIARY [C]", new Color(0.24f, 0.20f, 0.38f), new Color(0.75f, 0.58f, 0.95f)))
            {
                ToggleModal(ModalOverlay.CodexBestiary);
            }
            if (DrawPixelButton(new Rect(card.x + 40 + bW, btnY, bW, 42), "MERCHANT & EQUIPMENT [I]", new Color(0.20f, 0.34f, 0.28f), new Color(0.45f, 0.88f, 0.65f)))
            {
                ToggleModal(ModalOverlay.ShopInventory);
            }
            if (DrawPixelButton(new Rect(card.x + 55 + bW * 2, btnY, bW, 42), "TEACHER ANALYTICS CSV [T]", new Color(0.38f, 0.26f, 0.16f), new Color(0.95f, 0.75f, 0.32f)))
            {
                ToggleModal(ModalOverlay.TeacherAnalytics);
            }
        }

        // ---------------------------------------------------------------------
        // 2. LABYRINTH BOARD CRAWLER SCREEN (LOST IN MULTIVERSE STYLE)
        // ---------------------------------------------------------------------
        private void DrawLabyrinthBoardScreen(float sw, float sh)
        {
            var stage = GrimoireDatabase.Stages[currentAct];
            GUI.DrawTexture(new Rect(0, 0, sw, sh), GetTex($"bg/bg_s{currentAct}"), ScaleMode.StretchToFill);
            DrawPixelPanel(new Rect(0, 0, sw, sh), new Color(0.04f, 0.05f, 0.09f, 0.55f), Color.clear);

            // Top HUD Bar
            Rect topBar = new Rect(12, 10, sw - 24, 64);
            DrawPixelPanel(topBar, new Color(0.08f, 0.09f, 0.15f, 0.95f), stage.rimColor);

            GUI.Label(new Rect(topBar.x + 14, topBar.y + 6, 360, 26), $"{stage.title} — {stage.subtitle}", _pixelHeaderStyle);
            GUI.Label(new Rect(topBar.x + 14, topBar.y + 34, 620, 24),
                $"Lv.{playerLevel} Aegis | HP: {hp}/{maxHp} | MP: {mp}/{maxMp} | FOCUS: {focus}% | ATK:{TotalAtk} DEF:{TotalDef} SPD:{TotalSpd} | Gold: {gold}G | DDA: {ddaTier}",
                _pixelBodyStyle);

            // Top-Right Quick Navigation Buttons
            float rBtnX = topBar.xMax - 490;
            if (DrawPixelButton(new Rect(rBtnX, topBar.y + 14, 110, 36), "CODEX [C]", new Color(0.25f, 0.20f, 0.42f), stage.rimColor)) ToggleModal(ModalOverlay.CodexBestiary);
            if (DrawPixelButton(new Rect(rBtnX + 118, topBar.y + 14, 115, 36), "SHOP / BAG [I]", new Color(0.18f, 0.35f, 0.28f), stage.rimColor)) ToggleModal(ModalOverlay.ShopInventory);
            if (DrawPixelButton(new Rect(rBtnX + 241, topBar.y + 14, 125, 36), "TEACHER CSV [T]", new Color(0.38f, 0.26f, 0.14f), stage.rimColor)) ToggleModal(ModalOverlay.TeacherAnalytics);
            if (DrawPixelButton(new Rect(rBtnX + 374, topBar.y + 14, 104, 36), "TITLE MENU", new Color(0.35f, 0.18f, 0.22f), stage.rimColor))
            {
                viewMode = GameViewMode.Title;
                GrimoireAudioManager.Instance?.PlayMusic("music_title");
            }

            // Center Labyrinth Board Viewport
            Rect boardRect = new Rect(16, 84, sw - 32, sh - 210);
            DrawPixelPanel(boardRect, new Color(0.06f, 0.07f, 0.12f, 0.88f), new Color(0.32f, 0.36f, 0.48f));

            if (board != null && board.tiles.Count > 0)
            {
                float tileSize = Mathf.Min((boardRect.width - 120f) / board.width, (boardRect.height - 70f) / board.height);
                tileSize = Mathf.Clamp(tileSize, 54f, 88f);
                float pitch = tileSize * 1.28f;
                float totalW = (board.width - 1) * pitch + tileSize;
                float totalH = (board.height - 1) * pitch + tileSize;
                float startX = boardRect.x + (boardRect.width - totalW) * 0.5f;
                float startY = boardRect.y + (boardRect.height - totalH) * 0.5f;

                var curTile = board.GetTile(currentTileId);

                // 1) Draw Corridor Connectors between Neighboring Tiles
                foreach (var t in board.tiles)
                {
                    Vector2 c1 = new Vector2(startX + t.gx * pitch + tileSize * 0.5f, startY + t.gy * pitch + tileSize * 0.5f);
                    foreach (int nbId in t.neighbors)
                    {
                        if (nbId < t.id) continue;
                        var nb = board.GetTile(nbId);
                        Vector2 c2 = new Vector2(startX + nb.gx * pitch + tileSize * 0.5f, startY + nb.gy * pitch + tileSize * 0.5f);
                        Rect conn = new Rect(Mathf.Min(c1.x, c2.x) - 5, Mathf.Min(c1.y, c2.y) - 5, Mathf.Abs(c2.x - c1.x) + 10, Mathf.Abs(c2.y - c1.y) + 10);
                        bool isStepEdge = stepsRemaining > 0 && ((t.id == currentTileId && nb.id != currentTileId) || (nb.id == currentTileId && t.id != currentTileId));
                        GUI.color = isStepEdge ? new Color(1.0f, 0.88f, 0.28f, 0.9f) : new Color(0.32f, 0.36f, 0.52f, 0.75f);
                        GUI.DrawTexture(conn, _whiteTex);
                        GUI.color = Color.white;
                    }
                }

                // 2) Draw Each Board Tile Plate, Icon & Hero Token
                bool animFrame = (Mathf.FloorToInt(Time.time * 3f) % 2) == 1;
                foreach (var t in board.tiles)
                {
                    Rect tr = new Rect(startX + t.gx * pitch, startY + t.gy * pitch, tileSize, tileSize);
                    bool isCurrent = (t.id == currentTileId);
                    bool isValidNextStep = (stepsRemaining > 0 && curTile != null && curTile.neighbors.Contains(t.id));

                    GUI.DrawTexture(tr, GetTex($"tiles/plate_s{currentAct}_{t.variant}"), ScaleMode.StretchToFill);

                    if (!t.revealed && !isCurrent)
                    {
                        GUI.DrawTexture(tr, GetTex("tiles/fog"), ScaleMode.StretchToFill);
                        GUI.Label(tr, "?", _pixelSmallStyle);
                    }
                    else
                    {
                        string iconName = GetTileIconKey(t);
                        Rect iconRect = new Rect(tr.x + tileSize * 0.18f, tr.y + tileSize * 0.14f, tileSize * 0.64f, tileSize * 0.64f);
                        GUI.DrawTexture(iconRect, GetTex("tiles/" + iconName), ScaleMode.ScaleToFit);

                        var lblRect = new Rect(tr.x, tr.yMax - 18, tr.width, 16);
                        GUI.Label(lblRect, t.type.ToString().ToUpper(), _pixelBadgeStyle);
                    }

                    if (isValidNextStep)
                    {
                        DrawPixelPanel(tr, new Color(1f, 0.9f, 0.2f, 0.22f), new Color(1f, 0.92f, 0.25f));
                        if (GUI.Button(tr, GUIContent.none, GUIStyle.none))
                        {
                            StepToNeighborTile(t.id);
                        }
                    }
                    else if (isCurrent)
                    {
                        DrawPixelPanel(tr, new Color(0.2f, 0.85f, 1f, 0.18f), new Color(0.35f, 0.95f, 1f));
                    }

                    if (isCurrent)
                    {
                        Texture2D pTok = GetTex(animFrame ? "chars/player_front2" : "chars/player_front");
                        Rect pRect = new Rect(tr.x + tileSize * 0.12f, tr.y - tileSize * 0.18f, tileSize * 0.76f, tileSize * 0.76f);
                        GUI.DrawTexture(pRect, pTok, ScaleMode.ScaleToFit);
                    }
                }
            }

            // Bottom Exploration Control Panel (Dice + Banner + Quick Actions)
            Rect botBar = new Rect(16, sh - 118, sw - 32, 106);
            DrawPixelPanel(botBar, new Color(0.08f, 0.09f, 0.15f, 0.96f), stage.rimColor);

            // Pixel Dice Display
            Rect dieBox = new Rect(botBar.x + 16, botBar.y + 12, 82, 82);
            DrawPixelPanel(dieBox, new Color(0.14f, 0.15f, 0.24f), stage.rimColor);
            GUI.DrawTexture(new Rect(dieBox.x + 9, dieBox.y + 9, 64, 64), GetTex($"dice/die_{Mathf.Clamp(diceValue, 1, 6)}"), ScaleMode.ScaleToFit);

            // Roll Dice / Stop & Interact Buttons
            Rect rollBtn = new Rect(botBar.x + 110, botBar.y + 14, 195, 38);
            if (stepsRemaining == 0)
            {
                if (DrawPixelButton(rollBtn, "ROLL DICE [SPACE]", new Color(0.22f, 0.48f, 0.32f), new Color(0.45f, 0.95f, 0.62f)))
                {
                    RollExplorationDice();
                }
            }
            else
            {
                if (DrawPixelButton(rollBtn, $"STEPS LEFT: {stepsRemaining} (STOP HERE)", new Color(0.52f, 0.38f, 0.14f), new Color(1f, 0.85f, 0.3f)))
                {
                    stepsRemaining = 0;
                    ResolveBoardTileEvent(board.GetTile(currentTileId));
                }
            }

            // Instant Boss / Practice Encounter Button
            Rect bossBtn = new Rect(botBar.x + 110, botBar.y + 58, 195, 34);
            if (DrawPixelButton(bossBtn, $"CHALLENGE ACT {currentAct} BOSS", new Color(0.48f, 0.18f, 0.22f), new Color(0.95f, 0.42f, 0.42f)))
            {
                StartBattleEncounter(stage.bossId, true, board.GetTile(board.bossId));
            }

            // Message Banner & Act Switcher
            GUI.Label(new Rect(botBar.x + 320, botBar.y + 14, botBar.width - 510, 78), boardBannerMessage, _pixelBodyStyle);

            float actSwitchX = botBar.xMax - 175;
            GUI.Label(new Rect(actSwitchX, botBar.y + 10, 160, 20), "PINDAH ACT:", _pixelSmallStyle);
            for (int a = 1; a <= 3; a++)
            {
                if (DrawPixelButton(new Rect(actSwitchX + (a - 1) * 54, botBar.y + 34, 48, 30), $"ACT {a}",
                    currentAct == a ? new Color(0.35f, 0.52f, 0.78f) : new Color(0.18f, 0.20f, 0.30f), stage.rimColor))
                {
                    StartActExpedition(a);
                }
            }
            if (DrawPixelButton(new Rect(actSwitchX, botBar.y + 68, 156, 28), "RE-ENTER TILE EVENT", new Color(0.24f, 0.28f, 0.44f), stage.rimColor))
            {
                ResolveBoardTileEvent(board.GetTile(currentTileId));
            }
        }

        private string GetTileIconKey(BoardTileNode t)
        {
            if (t.cleared && t.type != BoardTileType.Boss && t.type != BoardTileType.Merchant && t.type != BoardTileType.Rest && t.type != BoardTileType.Shrine)
            {
                return "icon_cleared";
            }
            switch (t.type)
            {
                case BoardTileType.Start: return "icon_start";
                case BoardTileType.Enemy: return "icon_enemy";
                case BoardTileType.Elite: return "icon_elite";
                case BoardTileType.Boss: return "icon_boss";
                case BoardTileType.Treasure: return "icon_treasure";
                case BoardTileType.Merchant: return "icon_merchant";
                case BoardTileType.Rest: return "icon_rest";
                case BoardTileType.Shrine: return "icon_shrine";
                case BoardTileType.Event: return "icon_event";
                case BoardTileType.Trap: return "icon_trap";
                default: return "icon_enemy";
            }
        }

        // ---------------------------------------------------------------------
        // 3. TURN-BASED BATTLE ARENA + ARRAY VISUALIZER + GOLD-ZONE PARRY
        // ---------------------------------------------------------------------
        private void DrawBattleArenaScreen(float sw, float sh)
        {
            int actBg = activeMonster != null ? Mathf.Clamp(activeMonster.act, 1, 3) : currentAct;
            GUI.DrawTexture(new Rect(0, 0, sw, sh), GetTex($"bg/bg_s{actBg}"), ScaleMode.StretchToFill);
            DrawPixelPanel(new Rect(0, 0, sw, sh), new Color(0.03f, 0.04f, 0.08f, 0.48f), Color.clear);

            // Top Battle Status Header
            Rect topRect = new Rect(14, 10, sw - 28, 54);
            DrawPixelPanel(topRect, new Color(0.08f, 0.09f, 0.15f, 0.94f), new Color(0.88f, 0.72f, 0.24f));

            string phaseTag = isBossEncounter ? $" [PHASE {bossPhase}]" : "";
            GUI.Label(new Rect(topRect.x + 14, topRect.y + 6, 520, 24),
                $"VS {activeMonster.name.ToUpper()} ({activeMonster.role}){phaseTag} — HP: {monsterHp}/{monsterMaxHp}", _pixelHeaderStyle);
            GUI.Label(new Rect(topRect.x + 14, topRect.y + 28, 660, 22),
                $"Mekanik: {activeMonster.mechanicDesc} | DDA Tier: {ddaTier} | TP Pool: {string.Join(", ", activeMonster.tpPool)}", _pixelBodyStyle);

            if (DrawPixelButton(new Rect(topRect.xMax - 250, topRect.y + 10, 115, 34), "CODEX [C]", new Color(0.24f, 0.20f, 0.42f), new Color(0.75f, 0.6f, 0.95f)))
            {
                ToggleModal(ModalOverlay.CodexBestiary);
            }
            if (!isBossEncounter && DrawPixelButton(new Rect(topRect.xMax - 125, topRect.y + 10, 112, 34), "RETREAT", new Color(0.42f, 0.18f, 0.20f), new Color(0.9f, 0.4f, 0.4f)))
            {
                viewMode = GameViewMode.Board;
                GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey);
            }

            // Arena Stage: Hero Sprite (Left) + Array Visualizer (Center) + Monster Sprite (Right)
            float stageY = 72f;
            float stageH = sh - 310f;
            bool frame2 = (Mathf.FloorToInt(Time.time * 2.8f) % 2) == 1;

            // Hero Platform & Sprite
            Rect heroBox = new Rect(36, stageY + stageH * 0.18f, 150, 150);
            string heroTexKey = battlePhase == BattlePhase.AlgorithmicChallenge
                ? (isChallengeForSpell ? "chars/player_attack" : "chars/player_defend")
                : (frame2 ? "chars/player_idle2" : "chars/player_idle");
            GUI.DrawTexture(heroBox, GetTex(heroTexKey), ScaleMode.ScaleToFit);
            GUI.Label(new Rect(36, heroBox.yMax - 4, 160, 22), $"Aegis Lv.{playerLevel} ({hp}/{maxHp} HP)", _pixelSmallStyle);

            // Monster Platform & Sprite
            Rect monBox = new Rect(sw - 205, stageY + stageH * 0.10f, 168, 168);
            string monTexKey = frame2 ? $"enemies/{activeMonster.id}_2" : $"enemies/{activeMonster.id}";
            GUI.DrawTexture(monBox, GetTex(monTexKey), ScaleMode.ScaleToFit);

            // Monster HP Bar
            Rect mHpBg = new Rect(sw - 220, monBox.yMax + 2, 190, 18);
            DrawPixelPanel(mHpBg, new Color(0.18f, 0.06f, 0.08f), new Color(0.85f, 0.3f, 0.3f));
            float mHpRatio = Mathf.Clamp01((float)monsterHp / Mathf.Max(1, monsterMaxHp));
            GUI.color = new Color(0.92f, 0.25f, 0.28f);
            GUI.DrawTexture(new Rect(mHpBg.x + 2, mHpBg.y + 2, (mHpBg.width - 4) * mHpRatio, mHpBg.height - 4), _whiteTex);
            GUI.color = Color.white;
            GUI.Label(mHpBg, $"{monsterHp} / {monsterMaxHp} HP", _pixelBadgeStyle);

            // Center Interactive Array Visualizer & Active Challenge Panel
            Rect centerStage = new Rect(200, stageY, sw - 425, stageH);
            DrawArrayVisualizerAndChallenge(centerStage);

            // Bottom 3-Column RPG HUD (Lost in Multiverse RPG Layout)
            Rect hudRect = new Rect(14, sh - 228, sw - 28, 216);
            DrawBattleBottomHud(hudRect);
        }

        private void DrawArrayVisualizerAndChallenge(Rect area)
        {
            DrawPixelPanel(area, new Color(0.06f, 0.08f, 0.14f, 0.92f), new Color(0.38f, 0.52f, 0.78f));

            int[] arr = (currentQuestion != null && currentQuestion.arrayData != null && currentQuestion.arrayData.Length > 0)
                ? currentQuestion.arrayData
                : new[] { 15, 28, 42, 57, 73, 89 };

            string algoTitle = currentQuestion != null
                ? $"{currentQuestion.tp} ({currentQuestion.bloom}) — {currentQuestion.algorithmName} [{currentQuestion.formatName}]"
                : "GRIMOIRE ARRAY VISUALIZER — Pilih Serangan atau Mantra untuk Memulai Eksekusi";

            GUI.Label(new Rect(area.x + 14, area.y + 6, area.width - 28, 24), algoTitle, _pixelHeaderStyle);

            // Draw Array Cells
            int n = arr.Length;
            float cellW = Mathf.Min(74f, (area.width - 40f) / Mathf.Max(1, n) - 8f);
            float cellH = 54f;
            float totalArrW = n * (cellW + 8f) - 8f;
            float startX = area.x + (area.width - totalArrW) * 0.5f;
            float arrY = area.y + 36f;

            for (int i = 0; i < n; i++)
            {
                Rect cellRect = new Rect(startX + i * (cellW + 8f), arrY, cellW, cellH);
                bool isHighlighted = currentQuestion != null && currentQuestion.highlightIndices != null && currentQuestion.highlightIndices.Contains(i);
                bool isMid = currentQuestion != null && currentQuestion.midIdx == i;
                bool isPivot = currentQuestion != null && currentQuestion.pivotIdx == i;
                bool isLow = currentQuestion != null && currentQuestion.lowIdx == i;
                bool isHigh = currentQuestion != null && currentQuestion.highIdx == i;

                Color cellBg = new Color(0.14f, 0.17f, 0.28f);
                Color cellBorder = new Color(0.42f, 0.48f, 0.68f);
                if (isPivot) { cellBg = new Color(0.48f, 0.18f, 0.52f); cellBorder = new Color(0.95f, 0.45f, 1.0f); }
                else if (isMid) { cellBg = new Color(0.48f, 0.38f, 0.12f); cellBorder = new Color(1.0f, 0.86f, 0.25f); }
                else if (isHighlighted) { cellBg = new Color(0.16f, 0.42f, 0.48f); cellBorder = new Color(0.35f, 0.95f, 0.95f); }

                DrawPixelPanel(cellRect, cellBg, cellBorder);

                // Index label [i]
                GUI.Label(new Rect(cellRect.x, cellRect.y + 2, cellRect.width, 16), $"idx [{i}]", _pixelBadgeStyle);

                // Boss Fog / Buried Mechanic Check
                bool fogged = activeMonster != null && activeMonster.id == "the_blind_archivist" && bossPhase >= 2 && isMid == false && (i == 0 || i == n - 1);
                string valText = fogged ? "??" : arr[i].ToString();
                var valStyle = new GUIStyle(_pixelHeaderStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 19 };
                GUI.Label(new Rect(cellRect.x, cellRect.y + 18, cellRect.width, 32), valText, valStyle);

                // Pointer Tags below cell (LOW / MID / HIGH / PIVOT)
                List<string> tags = new List<string>();
                if (isLow) tags.Add("LOW");
                if (isMid) tags.Add("MID");
                if (isHigh) tags.Add("HIGH");
                if (isPivot) tags.Add("PIVOT");
                if (tags.Count > 0)
                {
                    GUI.Label(new Rect(cellRect.x - 4, cellRect.yMax + 1, cellRect.width + 8, 16), string.Join("/", tags),
                        new GUIStyle(_pixelBadgeStyle) { normal = { textColor = new Color(1f, 0.88f, 0.32f) } });
                }
            }

            // Sub-panel below Array Visualizer depends on BattlePhase
            Rect subArea = new Rect(area.x + 14, arrY + cellH + 22f, area.width - 28, area.height - (cellH + 64f));

            if (battlePhase == BattlePhase.AlgorithmicChallenge && currentQuestion != null)
            {
                // Gold-Zone Active Defend Parry Bar
                Rect timerBar = new Rect(subArea.x, subArea.y, subArea.width, 20);
                DrawPixelPanel(timerBar, new Color(0.12f, 0.12f, 0.18f), new Color(0.6f, 0.6f, 0.75f));

                // Gold Parry Zone marker (top 35% of remaining time = elapsed <= 35%)
                float goldWidth = (timerBar.width - 4) * 0.35f;
                GUI.color = new Color(1.0f, 0.84f, 0.18f, 0.28f);
                GUI.DrawTexture(new Rect(timerBar.xMax - 2 - goldWidth, timerBar.y + 2, goldWidth, timerBar.height - 4), _whiteTex);

                float remRatio = Mathf.Clamp01(questionTimer / Mathf.Max(0.1f, questionMaxTime));
                float elapsedRatio = 1.0f - remRatio;
                Color barColor = elapsedRatio <= 0.35f
                    ? new Color(1.0f, 0.86f, 0.22f) // GOLD ZONE (PERFECT PARRY)
                    : (elapsedRatio <= 0.70f ? new Color(0.28f, 0.85f, 0.95f) : new Color(0.95f, 0.42f, 0.32f));

                GUI.color = barColor;
                GUI.DrawTexture(new Rect(timerBar.x + 2, timerBar.y + 2, (timerBar.width - 4) * remRatio, timerBar.height - 4), _whiteTex);
                GUI.color = Color.white;

                string zoneName = elapsedRatio <= 0.35f ? "★ GOLD PARRY ZONE (1.5x Counter / 100% Mitigasi)" : (elapsedRatio <= 0.70f ? "SOLID BLOCK ZONE (75% Mitigasi)" : "GRAZED ZONE (40% Mitigasi)");
                GUI.Label(timerBar, $"{questionTimer:F1}s / {questionMaxTime:F1}s — {zoneName}", _pixelBadgeStyle);

                // Prompt
                GUI.Label(new Rect(subArea.x, subArea.y + 24, subArea.width, 48), currentQuestion.prompt, _pixelBodyStyle);

                // 2x2 Answer Options Grid
                float optW = (subArea.width - 12f) * 0.5f;
                float optH = Mathf.Max(34f, (subArea.height - 80f) * 0.5f - 4f);
                string[] keys = { "A", "B", "C", "D" };

                for (int i = 0; i < 4; i++)
                {
                    int row = i / 2;
                    int col = i % 2;
                    Rect optRect = new Rect(subArea.x + col * (optW + 12f), subArea.y + 74f + row * (optH + 6f), optW, optH);

                    if (eliminatedOptions.Contains(i))
                    {
                        DrawPixelPanel(optRect, new Color(0.12f, 0.12f, 0.15f, 0.6f), new Color(0.3f, 0.3f, 0.35f));
                        GUI.Label(optRect, $"[{keys[i]}] --- DIELIMINASI OLEH SCHOLAR'S HINT ---", _pixelSmallStyle);
                    }
                    else
                    {
                        string optText = (obscuredOptionIdx == i)
                            ? $"[{keys[i]}] [HALAMAN ROBEK / TORN PAGE: {currentQuestion.options[i].Substring(0, Mathf.Min(12, currentQuestion.options[i].Length))}...]"
                            : $"[{keys[i]}] {currentQuestion.options[i]}";

                        if (DrawPixelButton(optRect, optText, new Color(0.16f, 0.24f, 0.38f), new Color(0.48f, 0.68f, 0.95f)))
                        {
                            SubmitAnswer(i);
                        }
                    }
                }
            }
            else if (battlePhase == BattlePhase.TurnResolution)
            {
                DrawPixelPanel(subArea, new Color(0.10f, 0.12f, 0.20f), lastResolutionColor);
                var resHeader = new GUIStyle(_pixelHeaderStyle) { normal = { textColor = lastResolutionColor } };
                GUI.Label(new Rect(subArea.x + 12, subArea.y + 8, subArea.width - 24, 26), lastResolutionTitle, resHeader);
                GUI.Label(new Rect(subArea.x + 12, subArea.y + 36, subArea.width - 24, subArea.height - 82), lastResolutionDetail, _pixelBodyStyle);

                if (DrawPixelButton(new Rect(subArea.x + subArea.width - 220, subArea.yMax - 40, 208, 32),
                    isChallengeForSpell ? "LANJUT KE ACTIVE DEFEND >>" : "GILIRAN BERIKUTNYA >>",
                    new Color(0.24f, 0.46f, 0.32f), lastResolutionColor))
                {
                    if (isChallengeForSpell)
                    {
                        // After Player Spell finishes, Monster attacks -> trigger Active Defend!
                        BeginAlgorithmicChallenge(false, null);
                    }
                    else
                    {
                        battlePhase = BattlePhase.PlayerCommand;
                    }
                }
            }
            else if (battlePhase == BattlePhase.MercyIntervention && mercyCodexEntry != null)
            {
                DrawPixelPanel(subArea, new Color(0.14f, 0.12f, 0.24f), new Color(0.95f, 0.78f, 0.28f));
                GUI.Label(new Rect(subArea.x + 12, subArea.y + 6, subArea.width - 24, 24),
                    $"★ MERCY SYSTEM INTERVENTION — {mercyCodexEntry.tp}: {mercyCodexEntry.title} (+25% HP/MP Pulih!)", _pixelHeaderStyle);
                GUI.Label(new Rect(subArea.x + 12, subArea.y + 32, subArea.width - 24, subArea.height - 74),
                    $"{mercyCodexEntry.summary}\nPseudocode: {mercyCodexEntry.pseudocode.Replace("\n", " | ")}", _pixelBodyStyle);

                if (DrawPixelButton(new Rect(subArea.x + subArea.width - 220, subArea.yMax - 38, 208, 30), "PAHAM & LANJUTKAN BERTARUNG",
                    new Color(0.28f, 0.48f, 0.34f), new Color(0.95f, 0.82f, 0.32f)))
                {
                    battlePhase = BattlePhase.PlayerCommand;
                }
            }
            else
            {
                GUI.Label(new Rect(subArea.x + 8, subArea.y + 8, subArea.width - 16, subArea.height - 16),
                    $"Lore Monster: \"{activeMonster.lore}\"\n\nPilih aksi pada HUD di bawah: [1] BASIC SLASH, [2] GRIMOIRE SPELLS (8 Mantra Algoritma + Limit Break), atau [3] ITEMS.",
                    _pixelBodyStyle);
            }
        }

        private void DrawBattleBottomHud(Rect hud)
        {
            float col1W = 250f;
            float col3W = 310f;
            float col2W = hud.width - col1W - col3W - 20f;

            Rect col1 = new Rect(hud.x, hud.y, col1W, hud.height);
            Rect col2 = new Rect(col1.xMax + 10f, hud.y, col2W, hud.height);
            Rect col3 = new Rect(col2.xMax + 10f, hud.y, col3W, hud.height);

            // COLUMN 1: HERO VITALS & STATUS BADGES
            DrawPixelPanel(col1, new Color(0.08f, 0.09f, 0.15f, 0.96f), new Color(0.45f, 0.65f, 0.92f));
            GUI.Label(new Rect(col1.x + 12, col1.y + 8, col1.width - 24, 24), $"AEGIS (Lv.{playerLevel})", _pixelHeaderStyle);

            DrawStatBar(new Rect(col1.x + 12, col1.y + 38, col1.width - 24, 22), $"HP: {hp} / {maxHp}", (float)hp / maxHp, new Color(0.22f, 0.78f, 0.38f));
            DrawStatBar(new Rect(col1.x + 12, col1.y + 66, col1.width - 24, 22), $"MP: {mp} / {maxMp}", (float)mp / maxMp, new Color(0.25f, 0.58f, 0.95f));
            DrawStatBar(new Rect(col1.x + 12, col1.y + 94, col1.width - 24, 22), $"FOCUS: {focus}% {(focus >= 100 ? "[LIMIT READY!]" : "")}", focus / 100f, new Color(0.95f, 0.75f, 0.20f));

            GUI.Label(new Rect(col1.x + 12, col1.y + 122, col1.width - 24, 42),
                $"ATK: {TotalAtk} | DEF: {TotalDef} | SPD: {TotalSpd}\nStatus: {(poisonTurns > 0 ? $"POISON({poisonTurns}) " : "")}{(silenceTurns > 0 ? $"SILENCE({silenceTurns}) " : "")}{(tornPageTurns > 0 ? $"TORN({tornPageTurns}) " : "")}{(poisonTurns == 0 && silenceTurns == 0 && tornPageTurns == 0 ? "NORMAL" : "")}",
                _pixelBodyStyle);

            if (battlePhase == BattlePhase.AlgorithmicChallenge && !hintUsedOnQuestion)
            {
                if (DrawPixelButton(new Rect(col1.x + 12, col1.yMax - 40, col1.width - 24, 32), "SCHOLAR'S HINT [H] (-10 MP)", new Color(0.38f, 0.32f, 0.14f), new Color(0.95f, 0.82f, 0.28f)))
                {
                    UseScholarHint();
                }
            }

            // COLUMN 2: COMMANDS / SPELLS / ITEMS
            DrawPixelPanel(col2, new Color(0.08f, 0.09f, 0.15f, 0.96f), new Color(0.88f, 0.72f, 0.24f));

            if (battlePhase == BattlePhase.PlayerCommand)
            {
                GUI.Label(new Rect(col2.x + 14, col2.y + 8, col2.width - 28, 24), "PILIH TINDAKAN PERTARUNGAN:", _pixelHeaderStyle);
                float bw = (col2.width - 36f) * 0.5f;
                float bh = 68f;

                if (DrawPixelButton(new Rect(col2.x + 12, col2.y + 40, bw, bh), "⚔ BASIC SLASH\n(1.0x ATK + 12 Focus)", new Color(0.22f, 0.36f, 0.54f), new Color(0.55f, 0.78f, 0.98f)))
                {
                    ExecuteBasicAttack();
                }
                if (DrawPixelButton(new Rect(col2.x + 24 + bw, col2.y + 40, bw, bh), "✦ GRIMOIRE SPELLS\n(8 Mantra & Limit Break)", new Color(0.32f, 0.20f, 0.48f), new Color(0.82f, 0.58f, 0.98f)))
                {
                    GrimoireAudioManager.Instance?.PlaySfx("select");
                    battlePhase = BattlePhase.SpellSelect;
                }
                if (DrawPixelButton(new Rect(col2.x + 12, col2.y + 118, bw, bh), "🧪 CONSUMABLE ITEMS\n(HP/MP Vials & Scrolls)", new Color(0.18f, 0.38f, 0.28f), new Color(0.45f, 0.88f, 0.64f)))
                {
                    GrimoireAudioManager.Instance?.PlaySfx("select");
                    battlePhase = BattlePhase.ItemSelect;
                }
                if (DrawPixelButton(new Rect(col2.x + 24 + bw, col2.y + 118, bw, bh), "📖 GRIMOIRE CODEX [C]\n(Buka Rumus & Pseudocode)", new Color(0.38f, 0.28f, 0.16f), new Color(0.95f, 0.78f, 0.32f)))
                {
                    ToggleModal(ModalOverlay.CodexBestiary);
                }
            }
            else if (battlePhase == BattlePhase.SpellSelect)
            {
                GUI.Label(new Rect(col2.x + 12, col2.y + 6, col2.width - 110, 22), "PILIH MANTRA ALGORITMA GRIMOIRE:", _pixelHeaderStyle);
                if (DrawPixelButton(new Rect(col2.xMax - 92, col2.y + 6, 80, 24), "<< BACK", new Color(0.35f, 0.2f, 0.2f), Color.gray))
                {
                    battlePhase = BattlePhase.PlayerCommand;
                }

                float swBtn = (col2.width - 30f) * 0.5f;
                float shBtn = 38f;
                for (int i = 0; i < GrimoireDatabase.Spells.Count; i++)
                {
                    var sp = GrimoireDatabase.Spells[i];
                    int r = i / 2;
                    int c = i % 2;
                    Rect sr = new Rect(col2.x + 10 + c * (swBtn + 10f), col2.y + 34 + r * (shBtn + 5f), swBtn, shBtn);
                    int cost = GetEffectiveMpCost(sp);
                    string costTag = sp.focusCost > 0 ? $"{sp.focusCost} FOCUS" : $"{cost} MP";
                    Color bg = sp.focusCost > 0 ? new Color(0.48f, 0.32f, 0.10f) : new Color(0.20f, 0.26f, 0.44f);

                    if (DrawPixelButton(sr, $"{sp.name} [{costTag}] ({sp.tpCode})", bg, new Color(0.75f, 0.68f, 0.95f)))
                    {
                        BeginSpellCast(sp);
                    }
                }
            }
            else if (battlePhase == BattlePhase.ItemSelect)
            {
                GUI.Label(new Rect(col2.x + 12, col2.y + 6, col2.width - 110, 22), "PILIH ITEM CONSUMABLE:", _pixelHeaderStyle);
                if (DrawPixelButton(new Rect(col2.xMax - 92, col2.y + 6, 80, 24), "<< BACK", new Color(0.35f, 0.2f, 0.2f), Color.gray))
                {
                    battlePhase = BattlePhase.PlayerCommand;
                }

                var itemKeys = new[] { "pot_hp", "pot_mp", "pot_elixir", "scroll_clarity", "smoke_scroll", "golden_die" };
                float iw = (col2.width - 30f) * 0.5f;
                for (int i = 0; i < itemKeys.Length; i++)
                {
                    string ik = itemKeys[i];
                    var item = GrimoireDatabase.Items[ik];
                    int qty = consumables.TryGetValue(ik, out int q) ? q : 0;
                    Rect ir = new Rect(col2.x + 10 + (i % 2) * (iw + 10f), col2.y + 36 + (i / 2) * 52f, iw, 46f);
                    if (DrawPixelButton(ir, $"{item.name} (x{qty})\n{item.desc}",
                        qty > 0 ? new Color(0.16f, 0.34f, 0.26f) : new Color(0.18f, 0.18f, 0.22f), new Color(0.45f, 0.85f, 0.62f)))
                    {
                        if (qty > 0) UseConsumableItem(ik);
                    }
                }
            }
            else
            {
                GUI.Label(new Rect(col2.x + 14, col2.y + 12, col2.width - 28, col2.height - 24),
                    isChallengeForSpell
                        ? $"MANTRA AKTIF: {pendingSpell?.name}\nJawab tantangan algoritma di atas dengan benar untuk memicu daya hancur penuh!"
                        : $"ACTIVE DEFEND PHASE — Serangan {activeMonster.name} datang!\nJawab di ★ GOLD PARRY ZONE (<=35% waktu) untuk 0 DMG + Counter 1.5x ATK!\nTekan [H] untuk menggunakan Scholar's Hint (-10 MP).",
                    _pixelBodyStyle);
            }

            // COLUMN 3: COMBAT LOG
            DrawPixelPanel(col3, new Color(0.08f, 0.09f, 0.15f, 0.96f), new Color(0.42f, 0.48f, 0.62f));
            GUI.Label(new Rect(col3.x + 10, col3.y + 6, col3.width - 20, 22), "COMBAT & TELEMETRY LOG:", _pixelHeaderStyle);
            string logJoined = string.Join("\n", _combatLog.Take(7));
            GUI.Label(new Rect(col3.x + 10, col3.y + 30, col3.width - 20, col3.height - 36), logJoined, _pixelBodyStyle);
        }

        private void DrawStatBar(Rect r, string label, float ratio, Color fillCol)
        {
            DrawPixelPanel(r, new Color(0.12f, 0.13f, 0.20f), new Color(0.4f, 0.45f, 0.6f));
            GUI.color = fillCol;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(ratio), r.height - 4), _whiteTex);
            GUI.color = Color.white;
            GUI.Label(r, label, _pixelBadgeStyle);
        }

        // ---------------------------------------------------------------------
        // 4. GRAND VICTORY SCREEN
        // ---------------------------------------------------------------------
        private void DrawGrandVictoryScreen(float sw, float sh)
        {
            GUI.DrawTexture(new Rect(0, 0, sw, sh), GetTex("bg/bg_title"), ScaleMode.StretchToFill);
            Rect box = new Rect((sw - 760) * 0.5f, (sh - 420) * 0.5f, 760, 420);
            DrawPixelPanel(box, new Color(0.08f, 0.10f, 0.18f, 0.96f), new Color(1.0f, 0.86f, 0.25f));

            GUI.Label(new Rect(box.x + 24, box.y + 24, box.width - 48, 36), "★ THE GREAT INDEX RESTORED — ORDER RESTORED! ★",
                new GUIStyle(_pixelHeaderStyle) { fontSize = 24, alignment = TextAnchor.MiddleCenter });

            GUI.Label(new Rect(box.x + 32, box.y + 76, box.width - 64, 180),
                "Selamat, Ksatria Cendekia Aegis!\n\nKamu telah menaklukkan The Dusty Archives (Linear & Binary Search), The Catacombs of Chaos (Bubble, Selection, Insertion Sort), serta mengalahkan Entropy, The Chaos King di puncak The Tower of Entropy (Merge Sort, Quick Sort, dan evaluasi kompleksitas Big-O).\n\nSeluruh katalog pengetahuan Alexandria-Nova kembali terurut sempurna!",
                new GUIStyle(_pixelBodyStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 15 });

            if (DrawPixelButton(new Rect(box.x + 120, box.yMax - 70, 240, 44), "BUKA TEACHER ANALYTICS CSV", new Color(0.24f, 0.44f, 0.32f), new Color(0.95f, 0.82f, 0.28f)))
            {
                activeModal = ModalOverlay.TeacherAnalytics;
            }
            if (DrawPixelButton(new Rect(box.xMax - 360, box.yMax - 70, 240, 44), "KEMBALI KE TITLE SCREEN", new Color(0.24f, 0.30f, 0.52f), new Color(0.95f, 0.82f, 0.28f)))
            {
                viewMode = GameViewMode.Title;
            }
        }

        // ---------------------------------------------------------------------
        // 5. MODAL OVERLAYS (CODEX/BESTIARY, SHOP/INVENTORY, TEACHER CSV)
        // ---------------------------------------------------------------------
        private void DrawModalOverlay(float sw, float sh)
        {
            DrawPixelPanel(new Rect(0, 0, sw, sh), new Color(0.02f, 0.02f, 0.05f, 0.78f), Color.clear);
            Rect m = new Rect(40, 32, sw - 80, sh - 64);
            DrawPixelPanel(m, new Color(0.08f, 0.10f, 0.16f, 0.98f), new Color(0.95f, 0.78f, 0.26f));

            if (DrawPixelButton(new Rect(m.xMax - 110, m.y + 12, 96, 32), "CLOSE [ESC]", new Color(0.48f, 0.18f, 0.20f), new Color(0.95f, 0.45f, 0.45f)))
            {
                activeModal = ModalOverlay.None;
                return;
            }

            if (activeModal == ModalOverlay.CodexBestiary)
            {
                DrawCodexAndBestiaryModal(m);
            }
            else if (activeModal == ModalOverlay.ShopInventory)
            {
                DrawShopAndInventoryModal(m);
            }
            else if (activeModal == ModalOverlay.TeacherAnalytics)
            {
                DrawTeacherAnalyticsModal(m);
            }
        }

        private void DrawCodexAndBestiaryModal(Rect m)
        {
            GUI.Label(new Rect(m.x + 20, m.y + 14, 460, 28), "THE LIVING GRIMOIRE — CODEX (TP-01..TP-12) & 13 BESTIARY", _pixelHeaderStyle);

            if (DrawPixelButton(new Rect(m.x + 480, m.y + 12, 180, 32), "12 TP ALGORITHM CODEX",
                _codexTab == 0 ? new Color(0.28f, 0.44f, 0.68f) : new Color(0.16f, 0.18f, 0.26f), Color.cyan))
            {
                _codexTab = 0;
            }
            if (DrawPixelButton(new Rect(m.x + 670, m.y + 12, 180, 32), "13 MONSTER BESTIARY",
                _codexTab == 1 ? new Color(0.48f, 0.24f, 0.48f) : new Color(0.16f, 0.18f, 0.26f), Color.magenta))
            {
                _codexTab = 1;
            }

            Rect viewArea = new Rect(m.x + 16, m.y + 54, m.width - 32, m.height - 70);
            if (_codexTab == 0)
            {
                Rect content = new Rect(0, 0, viewArea.width - 24, GrimoireDatabase.Codex.Count * 115f);
                _codexScroll = GUI.BeginScrollView(viewArea, _codexScroll, content);
                for (int i = 0; i < GrimoireDatabase.Codex.Count; i++)
                {
                    var c = GrimoireDatabase.Codex[i];
                    Rect cr = new Rect(4, i * 112f, content.width - 8, 104f);
                    DrawPixelPanel(cr, new Color(0.12f, 0.14f, 0.22f), new Color(0.45f, 0.68f, 0.95f));
                    GUI.Label(new Rect(cr.x + 12, cr.y + 6, cr.width - 24, 22), $"{c.tp} [{c.bloom}] — {c.title}   |   {c.complexity}", _pixelHeaderStyle);
                    GUI.Label(new Rect(cr.x + 12, cr.y + 30, cr.width * 0.58f, 68), c.summary, _pixelBodyStyle);
                    GUI.Label(new Rect(cr.x + cr.width * 0.60f, cr.y + 28, cr.width * 0.38f, 70), $"Pseudocode:\n{c.pseudocode}", _pixelBodyStyle);
                }
                GUI.EndScrollView();
            }
            else
            {
                Rect content = new Rect(0, 0, viewArea.width - 24, GrimoireDatabase.MonsterOrder.Length * 106f);
                _codexScroll = GUI.BeginScrollView(viewArea, _codexScroll, content);
                for (int i = 0; i < GrimoireDatabase.MonsterOrder.Length; i++)
                {
                    var mon = GrimoireDatabase.Monsters[GrimoireDatabase.MonsterOrder[i]];
                    Rect mr = new Rect(4, i * 104f, content.width - 8, 96f);
                    DrawPixelPanel(mr, new Color(0.13f, 0.12f, 0.20f), new Color(0.85f, 0.55f, 0.32f));
                    GUI.DrawTexture(new Rect(mr.x + 10, mr.y + 8, 80, 80), GetTex($"enemies/{mon.id}"), ScaleMode.ScaleToFit);
                    GUI.Label(new Rect(mr.x + 104, mr.y + 6, mr.width - 260, 24),
                        $"#{i + 1} {mon.name} (Act {mon.act} — {mon.role}) | HP:{mon.hp} ATK:{mon.atk} DEF:{mon.def} | TP: {string.Join(", ", mon.tpPool)}", _pixelHeaderStyle);
                    GUI.Label(new Rect(mr.x + 104, mr.y + 32, mr.width - 260, 58), $"{mon.mechanicDesc}\nLore: \"{mon.lore}\"", _pixelBodyStyle);

                    if (DrawPixelButton(new Rect(mr.xMax - 145, mr.y + 28, 132, 36), "SPAR / TEST", new Color(0.45f, 0.20f, 0.24f), new Color(0.95f, 0.45f, 0.45f)))
                    {
                        activeModal = ModalOverlay.None;
                        StartBattleEncounter(mon.id, mon.role != "Regular");
                    }
                }
                GUI.EndScrollView();
            }
        }

        private void DrawShopAndInventoryModal(Rect m)
        {
            GUI.Label(new Rect(m.x + 20, m.y + 14, m.width - 160, 28),
                $"SCHOLAR MERCHANT & EQUIPMENT — Gold: {gold}G | Equipped: {GrimoireDatabase.Items[equippedWeapon].name} + {(string.IsNullOrEmpty(equippedAccessory) ? "None" : GrimoireDatabase.Items[equippedAccessory].name)}",
                _pixelHeaderStyle);

            Rect viewArea = new Rect(m.x + 16, m.y + 54, m.width - 32, m.height - 70);
            var allItems = GrimoireDatabase.Items.Values.ToList();
            Rect content = new Rect(0, 0, viewArea.width - 24, allItems.Count * 74f);
            _shopScroll = GUI.BeginScrollView(viewArea, _shopScroll, content);

            for (int i = 0; i < allItems.Count; i++)
            {
                var it = allItems[i];
                Rect ir = new Rect(4, i * 72f, content.width - 8, 66f);
                DrawPixelPanel(ir, new Color(0.12f, 0.15f, 0.22f), new Color(0.45f, 0.78f, 0.62f));
                GUI.DrawTexture(new Rect(ir.x + 10, ir.y + 9, 48, 48), GetTex($"items/{it.iconKey}"), ScaleMode.ScaleToFit);

                GUI.Label(new Rect(ir.x + 70, ir.y + 6, ir.width - 260, 22), $"{it.name} [{it.kind.ToUpper()}] — Harga: {it.price}G", _pixelHeaderStyle);
                GUI.Label(new Rect(ir.x + 70, ir.y + 30, ir.width - 260, 30), it.desc, _pixelBodyStyle);

                Rect btnR = new Rect(ir.xMax - 175, ir.y + 14, 160, 38);
                if (it.kind == "weapon" || it.kind == "accessory")
                {
                    bool owned = ownedEquipment.Contains(it.id);
                    bool equipped = (equippedWeapon == it.id || equippedAccessory == it.id);
                    if (equipped)
                    {
                        DrawPixelButton(btnR, "EQUIPPED", new Color(0.22f, 0.48f, 0.32f), Color.green);
                    }
                    else if (owned)
                    {
                        if (DrawPixelButton(btnR, "EQUIP ITEM", new Color(0.24f, 0.36f, 0.58f), Color.cyan))
                        {
                            if (it.kind == "weapon") equippedWeapon = it.id;
                            else equippedAccessory = it.id;
                            GrimoireAudioManager.Instance?.PlaySfx("select");
                        }
                    }
                    else
                    {
                        if (DrawPixelButton(btnR, $"BUY ({it.price}G)", new Color(0.45f, 0.34f, 0.14f), Color.yellow))
                        {
                            if (gold >= it.price)
                            {
                                gold -= it.price;
                                ownedEquipment.Add(it.id);
                                if (it.kind == "weapon") equippedWeapon = it.id;
                                else equippedAccessory = it.id;
                                GrimoireAudioManager.Instance?.PlaySfx("coin");
                            }
                            else GrimoireAudioManager.Instance?.PlaySfx("error");
                        }
                    }
                }
                else
                {
                    int qty = consumables.TryGetValue(it.id, out int q) ? q : 0;
                    if (DrawPixelButton(btnR, $"BUY ({it.price}G) [Owned:{qty}]", new Color(0.20f, 0.38f, 0.28f), Color.green))
                    {
                        if (gold >= it.price)
                        {
                            gold -= it.price;
                            consumables[it.id] = qty + 1;
                            GrimoireAudioManager.Instance?.PlaySfx("coin");
                        }
                        else GrimoireAudioManager.Instance?.PlaySfx("error");
                    }
                }
            }
            GUI.EndScrollView();
        }

        private void DrawTeacherAnalyticsModal(Rect m)
        {
            GUI.Label(new Rect(m.x + 20, m.y + 14, 580, 28),
                $"TEACHER ANALYTICS & DIAGNOSTIC TELEMETRY (Total Rekaman: {telemetryLog.Count})", _pixelHeaderStyle);

            if (DrawPixelButton(new Rect(m.x + 600, m.y + 12, 220, 32), "EXPORT CSV TO DISK", new Color(0.22f, 0.48f, 0.32f), new Color(0.45f, 0.95f, 0.62f)))
            {
                string csv = ProceduralQuestionEngine.ExportTelemetryToCsv(telemetryLog);
                string path = Path.Combine(Application.persistentDataPath, "GrimoireAegis_Student_Telemetry.csv");
                File.WriteAllText(path, csv);
                lastExportedCsvPath = path;
                GrimoireAudioManager.Instance?.PlaySfx("levelup");
            }

            if (!string.IsNullOrEmpty(lastExportedCsvPath))
            {
                GUI.Label(new Rect(m.x + 20, m.y + 46, m.width - 40, 22), $"CSV berhasil diekspor ke: {lastExportedCsvPath}",
                    new GUIStyle(_pixelBodyStyle) { normal = { textColor = new Color(0.45f, 0.95f, 0.62f) } });
            }

            Rect viewArea = new Rect(m.x + 16, m.y + 72, m.width - 32, m.height - 88);
            Rect content = new Rect(0, 0, viewArea.width - 24, Mathf.Max(12, GrimoireDatabase.Codex.Count) * 46f + telemetryLog.Count * 32f + 80f);
            _telemetryScroll = GUI.BeginScrollView(viewArea, _telemetryScroll, content);

            float y = 4f;
            GUI.Label(new Rect(8, y, content.width - 16, 24), "RINGKASAN CAPAIAN PER TUJUAN PEMBELAJARAN (TP-01..TP-12):", _pixelHeaderStyle);
            y += 28f;

            foreach (var c in GrimoireDatabase.Codex)
            {
                var recs = telemetryLog.Where(r => r.tp == c.tp).ToList();
                int total = recs.Count;
                int cor = recs.Count(r => r.isCorrect);
                float acc = total > 0 ? (cor * 100f / total) : 0f;
                string lastMis = recs.LastOrDefault(r => !r.isCorrect)?.misconceptionTag ?? "Tidak ada miskonsepsi terdeteksi";

                Rect row = new Rect(4, y, content.width - 8, 36f);
                DrawPixelPanel(row, new Color(0.12f, 0.14f, 0.22f), new Color(0.38f, 0.48f, 0.68f));
                GUI.Label(new Rect(row.x + 10, row.y + 6, row.width - 20, 24),
                    $"{c.tp} ({c.bloom}) | Percobaan: {total} | Akurasi: {acc:F0}% | Diagnostik Terakhir: {lastMis}", _pixelBodyStyle);
                y += 40f;
            }

            GUI.EndScrollView();
        }
    }
}
