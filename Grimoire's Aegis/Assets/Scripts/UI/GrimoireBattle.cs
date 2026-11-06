using System;
using System.Collections.Generic;
using System.Linq;
using GrimoireAegis.Core;
using UnityEngine;

namespace GrimoireAegis.UI
{
    /// <summary>Turn-based battle rules: Active Defend (GDD §7), statuses (§9, §15), difficulty (§12).</summary>
    public partial class GrimoireGameController
    {
        // --- Monster & battle state ---
        public MonsterDef activeMonster;
        public int monsterMaxHp = 100;
        public int monsterHp = 100;
        public int monsterAtk = 10;
        public int bossPhase = 1;
        public bool isBossEncounter = false;
        public bool aegisCharmUsedThisBattle = false;
        public bool battleFlawless = true;
        private int _enemyTurn;
        private int _volleyLeft, _volleyTotal = 1;
        private BoardTileNode _battleTile;

        // --- Status effects: id -> turns (or stacks) ---
        private readonly Dictionary<string, int> _pStatus = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _mStatus = new Dictionary<string, int>();
        public int panicStacks = 0;   // kept separately: also shown as status "panic"

        public bool HasP(string id) => _pStatus.TryGetValue(id, out int v) && v > 0;
        public bool HasM(string id) => _mStatus.TryGetValue(id, out int v) && v > 0;
        public int PVal(string id) => _pStatus.TryGetValue(id, out int v) ? v : 0;
        public int MVal(string id) => _mStatus.TryGetValue(id, out int v) ? v : 0;

        /// <summary>Statuses to show on the knight (turn/stack count).</summary>
        public IEnumerable<KeyValuePair<string, int>> PlayerStatuses()
        {
            foreach (var kv in _pStatus) if (kv.Value > 0) yield return kv;
            if (panicStacks > 0) yield return new KeyValuePair<string, int>("panic", panicStacks);
            if (hp > 0 && hp < maxHp * 0.2f) yield return new KeyValuePair<string, int>("adrenaline", 0);
            if (focus >= 100) yield return new KeyValuePair<string, int>("limit", 0);
        }

        public IEnumerable<KeyValuePair<string, int>> MonsterStatuses()
        {
            foreach (var kv in _mStatus) if (kv.Value > 0) yield return kv;
        }

        // --- Question state ---
        public bool isChallengeForSpell = false;
        public SpellDef pendingSpell = null;
        public QuestionPayload currentQuestion = null;
        public float questionTimer = 0f;
        public float questionMaxTime = 12f;
        public bool NoTimer => difficulty == Difficulty.Easy;
        public List<int> eliminatedOptions = new List<int>();
        public bool hintUsedOnQuestion = false;
        public int obscuredOptionIdx = -1;
        public string lastResolutionTitle = "";
        public string lastResolutionDetail = "";
        public string lastResolutionGrade = "";
        public Color lastResolutionColor = Color.white;
        public CodexEntry mercyCodexEntry = null;
        private float _readingUntil;
        private float _blindUntil;
        private int[] _optionOrder = { 0, 1, 2, 3 };
        private const float PerfectGuardRemaining = 0.60f;

        // --- Victory summary ---
        public int winExp, winGold, winFrag;
        public bool winLevelUp, winFlawless;

        // --- Animation & battle FX state ---
        private readonly AnimState _heroAnim = new AnimState { actor = "hero" };
        private readonly AnimState _monAnim = new AnimState { actor = "corrupted_slime" };
        private readonly List<VfxInstance> _vfx = new List<VfxInstance>();
        private readonly List<FloatingText> _floaters = new List<FloatingText>();
        private readonly List<KeyValuePair<float, Action>> _scheduled = new List<KeyValuePair<float, Action>>();
        private float _shakeUntil, _shakeMag;
        private float _flashUntil;
        private Color _flashColor = Color.clear;
        private float _popupStart;
        private Rect _heroRect = new Rect(40, 130, 96, 96), _monRect = new Rect(500, 110, 96, 96);
        private readonly List<string> _combatLog = new List<string>();

        private string MonIdle => (isBossEncounter && bossPhase >= 2 && AnimLibrary.Durations(activeMonster.id + "_idle2") != null) ? "idle2" : "idle";

        // =====================================================================
        // ENCOUNTER
        // =====================================================================
        public void StartBattleEncounter(string monsterId, bool isBoss, BoardTileNode sourceTile = null)
        {
            if (!GrimoireDatabase.Monsters.TryGetValue(monsterId, out MonsterDef def))
            {
                def = GrimoireDatabase.Monsters["corrupted_slime"];
            }

            activeMonster = def;
            float hpMul = difficulty == Difficulty.Hard ? 1.15f : (difficulty == Difficulty.Easy ? 0.9f : 1f);
            monsterMaxHp = Mathf.RoundToInt(def.hp * hpMul);
            monsterHp = monsterMaxHp;
            monsterAtk = Mathf.RoundToInt(def.atk * (difficulty == Difficulty.Hard ? 1.15f : 1f));
            _monHpShown = 1f;
            bossPhase = 1;
            isBossEncounter = isBoss || def.role == "Boss" || def.role == "Final Boss" || def.role == "Mid-Boss";
            aegisCharmUsedThisBattle = false;
            battleFlawless = true;
            _enemyTurn = 0;
            _battleTile = sourceTile;
            monstersSeen.Add(def.id);

            // statuses: knight's debuffs from earlier fights fade, monster passives apply
            _pStatus.Remove("silence"); _pStatus.Remove("blind"); _pStatus.Remove("buried");
            _pStatus.Remove("split"); _pStatus.Remove("death_seal"); _pStatus.Remove("ward");
            _mStatus.Clear();
            if (def.id == "the_abyss_golem") _mStatus["armored"] = 99;
            if (def.id == "pivot_wraith") _mStatus["false_pivot"] = 99;

            _scheduled.Clear();
            _vfx.Clear();
            _floaters.Clear();
            _monAnim.actor = def.id;
            _monAnim.Play("spawn", "idle");
            _heroAnim.Play("idle");
            _cmdSel = 0;

            viewMode = GameViewMode.Battle;
            activeModal = ModalOverlay.None;
            battlePhase = BattlePhase.Animating;
            Schedule(0.6f, () => battlePhase = BattlePhase.PlayerCommand);
            _combatLog.Clear();
            AddCombatLog($"{{#f05a50}}{def.name}{{/}} muncul!");
            if (HasM("armored")) AddCombatLog("{#8a8ea8}Zirah batu: serangan fisik hanya 30%.{/}");

            GrimoireAudioManager.Instance?.PlayMusic("music_battle");
            GrimoireAudioManager.Instance?.PlaySfx("select");
        }

        private void AddCombatLog(string msg)
        {
            _combatLog.Insert(0, msg);
            if (_combatLog.Count > 14) _combatLog.RemoveAt(_combatLog.Count - 1);
        }

        // =====================================================================
        // PLAYER COMMANDS (GDD §6)
        // =====================================================================
        private int MonsterDef_ => Mathf.RoundToInt(activeMonster.def * (HasM("ordered") ? 0.75f : 1f));

        /// <summary>Applies damage modifiers that belong to the target/attacker state.</summary>
        private int DealToMonster(int dmg, bool physical, out string note)
        {
            note = "";
            if (physical && HasM("armored")) { dmg = Mathf.RoundToInt(dmg * 0.30f); note += " (zirah batu)"; }
            if (HasM("marked")) { dmg = Mathf.RoundToInt(dmg * 1.5f); _mStatus.Remove("marked"); note += " KRITIS!"; }
            if (HasP("split")) { dmg = Mathf.RoundToInt(dmg * 0.5f); _pStatus.Remove("split"); note += " (terbelah 50%)"; }
            dmg = Mathf.Max(1, dmg);
            monsterHp = Mathf.Max(0, monsterHp - dmg);
            return dmg;
        }

        public void ExecuteBasicAttack()
        {
            if (battlePhase != BattlePhase.PlayerCommand) return;
            battlePhase = BattlePhase.Animating;
            _heroAnim.Play("attack");

            Schedule(0.27f, () =>
            {
                GrimoireAudioManager.Instance?.PlaySfx("hit");
                int raw = Mathf.Max(4, Mathf.RoundToInt((TotalAtk - MonsterDef_ * 0.45f) * UnityEngine.Random.Range(0.9f, 1.1f)));
                int dmg = DealToMonster(raw, true, out string note);
                focus = Mathf.Min(100, focus + 12);
                HitMonster(dmg, "slash", new Color(1f, 0.95f, 0.8f));
                AddCombatLog($"Aegis menebas: {{#ffd86a}}{dmg}{{/}} damage{note}.");

                CheckBossPhaseTransition();
                if (monsterHp <= 0) { HandleBattleVictory(); return; }
                Schedule(0.55f, StartEnemyTurn);
            });
        }

        public bool SpellLocked(SpellDef s) => HasP("silence") && s.focusCost == 0;

        public void BeginSpellCast(SpellDef spell)
        {
            if (SpellLocked(spell))
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                AddCombatLog("{#f05a50}Bisu!{/} Mantra biasa terkunci.");
                return;
            }
            int cost = GetEffectiveMpCost(spell);
            if (mp < cost || focus < spell.focusCost)
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                AddCombatLog("MP atau Focus belum cukup.");
                return;
            }
            mp -= cost;
            focus = Mathf.Max(0, focus - spell.focusCost);
            GrimoireAudioManager.Instance?.PlaySfx("magic");
            _heroAnim.Play("cast", "cast");
            SpawnVfx("arcane", _heroRect, 0.8f);
            BeginAlgorithmicChallenge(true, spell);
        }

        public int GetEffectiveMpCost(SpellDef spell)
        {
            return equippedAccessory == "acc_ring_comp" ? Mathf.Max(0, Mathf.RoundToInt(spell.mpCost * 0.80f)) : spell.mpCost;
        }

        /// <summary>GDD §6 Analyze: reveals topic, pattern and weakness. Costs the turn.</summary>
        public void AnalyzeMonster()
        {
            if (battlePhase != BattlePhase.PlayerCommand) return;
            monstersAnalyzed.Add(activeMonster.id);
            GrimoireAudioManager.Instance?.PlaySfx("magic");
            battlePhase = BattlePhase.Analyze;
        }

        public void CloseAnalyze()
        {
            AddCombatLog($"Analisis {activeMonster.name}: kelemahan tercatat di Bestiary.");
            battlePhase = BattlePhase.Animating;
            Schedule(0.2f, StartEnemyTurn);
        }

        public float FleeChance => 0.40f + (equippedAccessory == "acc_boots" ? 0.10f : 0f);

        public void TryFlee()
        {
            if (battlePhase != BattlePhase.PlayerCommand) return;
            if (isBossEncounter)
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                AddCombatLog("Tidak bisa kabur dari boss!");
                return;
            }
            if (UnityEngine.Random.value < FleeChance)
            {
                GrimoireAudioManager.Instance?.PlaySfx("cancel");
                _scheduled.Clear();
                viewMode = GameViewMode.Board;
                GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey);
                boardBannerMessage = "Aegis berhasil kabur kembali ke labirin.";
                return;
            }
            AddCombatLog("{#f05a50}Gagal kabur!{/}");
            battlePhase = BattlePhase.Animating;
            Schedule(0.3f, StartEnemyTurn);
        }

        public bool ItemsLocked => HasP("buried");

        public void UseConsumableItem(string itemId)
        {
            if (!consumables.TryGetValue(itemId, out int count) || count <= 0) return;
            if (!GrimoireDatabase.Items.TryGetValue(itemId, out ItemDef item)) return;
            bool inBattle = viewMode == GameViewMode.Battle;
            if (inBattle && ItemsLocked)
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                AddCombatLog("{#f05a50}Terkubur!{/} Tas tidak bisa dibuka.");
                return;
            }

            consumables[itemId] = count - 1;
            GrimoireAudioManager.Instance?.PlaySfx("heal");

            if (item.healHp > 0) hp = Mathf.Min(maxHp, hp + item.healHp);
            if (item.healMp > 0) mp = Mathf.Min(maxMp, mp + item.healMp);
            if (itemId == "pot_hp" || itemId == "pot_elixir") _pStatus.Remove("poison");
            if (itemId == "pot_mp" || itemId == "pot_elixir") _pStatus.Remove("silence");
            if (itemId == "pot_elixir") { _pStatus.Remove("torn_page"); _pStatus.Remove("crushed"); }
            if (itemId == "scroll_clarity") focus = Mathf.Min(100, focus + 50);
            if (itemId == "golden_die") forceGoldenDie = true;

            if (!inBattle) return;
            if (itemId == "smoke_scroll" && !isBossEncounter)
            {
                _scheduled.Clear();
                viewMode = GameViewMode.Board;
                GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey);
                boardBannerMessage = "Teleport Bookmark membawa Aegis kembali ke labirin.";
                return;
            }
            SpawnVfx("heal", _heroRect, 0.9f);
            if (item.healHp > 0) SpawnText($"+{item.healHp}", _heroRect, PixelUI.Green, 2);
            AddCombatLog($"Memakai {{#8ae05a}}{item.name}{{/}}.");
            // GDD §6: using an item spends the turn
            battlePhase = BattlePhase.Animating;
            Schedule(0.6f, StartEnemyTurn);
        }

        // =====================================================================
        // ENEMY TURN -> ACTIVE DEFEND
        // =====================================================================
        private void StartEnemyTurn()
        {
            if (monsterHp <= 0) return;
            _enemyTurn++;
            // Hydra regrows at the start of its turn (GDD: heals 10%)
            if (HasM("regen"))
            {
                int heal = Mathf.RoundToInt(monsterMaxHp * 0.10f);
                monsterHp = Mathf.Min(monsterMaxHp, monsterHp + heal);
                SpawnVfx("heal", _monRect, 0.9f);
                SpawnText($"+{heal}", _monRect, PixelUI.Green, 2);
                AddCombatLog($"{activeMonster.name} beregenerasi +{heal} HP.");
            }
            _volleyTotal = _volleyLeft = GrimoireStatus.Volley(activeMonster.id);
            BeginAlgorithmicChallenge(false, null);
        }

        public void BeginAlgorithmicChallenge(bool forSpell, SpellDef spell)
        {
            isChallengeForSpell = forSpell;
            pendingSpell = spell;
            eliminatedOptions.Clear();
            hintUsedOnQuestion = false;
            if (!forSpell) _volleyLeft = Mathf.Max(0, _volleyLeft - 1);

            string targetTp;
            if (forSpell && spell != null && !string.IsNullOrEmpty(spell.tpCode)) targetTp = spell.tpCode;
            else if (activeMonster != null && activeMonster.tpPool != null && activeMonster.tpPool.Length > 0)
                targetTp = activeMonster.tpPool[UnityEngine.Random.Range(0, activeMonster.tpPool.Length)];
            else targetTp = "TP-01";

            string tier = ddaEnabled ? ddaTier : "Standard";
            currentQuestion = ProceduralQuestionEngine.GenerateQuestion(targetTp, tier, TotalSpd, AccessoryTimerBonus);

            // Timer (GDD §7): base * difficulty * accessibility, + Ordered / adrenaline bonuses
            float t = currentQuestion.timeLimitSec;
            if (difficulty == Difficulty.Hard) t *= 0.8f;
            t *= timerMultiplier;
            if (HasM("ordered")) t += 2f;
            if (hp < maxHp * 0.20f) t += 1f;
            questionMaxTime = t;
            questionTimer = t;

            float reading = difficulty == Difficulty.Hard ? 1.0f : 1.5f;
            _readingUntil = Time.time + (NoTimer ? 0f : reading);
            _popupStart = Time.time;
            // Blind (GDD Lost Wisp): options appear 1 s after the question
            _blindUntil = HasP("blind") ? Time.time + (NoTimer ? 0f : reading) + 1.0f : 0f;
            if (HasP("blind")) _pStatus.Remove("blind");

            _optionOrder = new[] { 0, 1, 2, 3 };
            if (panicStacks >= 3)
            {
                for (int i = 3; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (_optionOrder[i], _optionOrder[j]) = (_optionOrder[j], _optionOrder[i]);
                }
            }

            obscuredOptionIdx = -1;
            if (HasP("torn_page") && equippedAccessory != "acc_cloak")
            {
                do obscuredOptionIdx = UnityEngine.Random.Range(0, 4); while (obscuredOptionIdx == currentQuestion.correctIndex && UnityEngine.Random.value < 0.7f);
            }

            if (!forSpell)
            {
                // Monsters with a status skill use their "cast" pose every other turn; the swing freezes mid-air (bullet-time).
                bool skill = GrimoireStatus.OnBreak(activeMonster.id, bossPhase) != null && _enemyTurn % 2 == 0
                             && AnimLibrary.Durations(activeMonster.id + "_cast") != null;
                _monAnim.Play(skill ? "cast" : "attack", MonIdle, hold: skill ? 2 : 1);
                _heroAnim.Play("defend", "defend");
            }
            _cmdSel = 0;
            battlePhase = BattlePhase.AlgorithmicChallenge;
        }

        /// <summary>"Miskonsepsi Bubble Sort: ..." -> "Bubble Sort: ..." (the label already says it).</summary>
        public static string CleanMis(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return "-";
            return tag.StartsWith("Miskonsepsi ") ? tag.Substring(12) : tag;
        }

        public bool OptionsHidden => Time.time < _blindUntil;
        public bool Reading => Time.time < _readingUntil;

        private void TickQuestionTimer()
        {
            if (viewMode != GameViewMode.Battle || battlePhase != BattlePhase.AlgorithmicChallenge) return;
            if (activeModal != ModalOverlay.None || NoTimer || Time.time < _readingUntil) return;
            questionTimer -= Time.deltaTime;
            if (questionTimer <= 0f)
            {
                questionTimer = 0f;
                SubmitAnswer(-1); // Timeout
            }
        }

        public int HintCost => difficulty == Difficulty.Easy ? 5 : 10;

        public void UseScholarHint()
        {
            if (currentQuestion == null || hintUsedOnQuestion || battlePhase != BattlePhase.AlgorithmicChallenge) return;
            if (mp < HintCost)
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                AddCombatLog($"MP tidak cukup untuk Hint ({HintCost} MP).");
                return;
            }
            mp -= HintCost;
            hintUsedOnQuestion = true;
            GrimoireAudioManager.Instance?.PlaySfx("magic");
            int toEliminate = equippedAccessory == "acc_monocle" ? 2 : 1;
            for (int i = 0; i < 4 && eliminatedOptions.Count < toEliminate; i++)
            {
                if (i != currentQuestion.correctIndex && !eliminatedOptions.Contains(i)) eliminatedOptions.Add(i);
            }
        }

        public void SubmitSlot(int slot)
        {
            if (slot < 0 || slot > 3 || OptionsHidden) return;
            int real = _optionOrder[slot];
            if (eliminatedOptions.Contains(real)) return;
            SubmitAnswer(real);
        }

        public void SubmitAnswer(int chosenIndex)
        {
            if (currentQuestion == null || battlePhase != BattlePhase.AlgorithmicChallenge) return;

            float elapsed = NoTimer ? Time.time - _popupStart : Mathf.Clamp(questionMaxTime - questionTimer, 0.1f, questionMaxTime);
            float remainingRatio = NoTimer ? 1f : Mathf.Clamp01(questionTimer / Mathf.Max(0.1f, questionMaxTime));
            bool isCorrect = chosenIndex == currentQuestion.correctIndex;

            string tp = currentQuestion.tp;
            if (!tpFailStreaks.ContainsKey(tp)) tpFailStreaks[tp] = 0;

            string grade;
            if (isCorrect)
            {
                consecutiveCorrect++;
                consecutiveWrong = 0;
                tpFailStreaks[tp] = 0;
                if (ddaEnabled && consecutiveCorrect >= 3) ddaTier = ddaTier == "Assisted" ? "Standard" : "Mastery";
                grade = remainingRatio >= PerfectGuardRemaining ? "PERFECT GUARD" : "GUARD";
            }
            else
            {
                consecutiveWrong++;
                consecutiveCorrect = 0;
                tpFailStreaks[tp]++;
                battleFlawless = false;
                if (ddaEnabled && consecutiveWrong >= 2) ddaTier = ddaTier == "Mastery" ? "Standard" : "Assisted";
                grade = "BREAK";
            }

            string misTag = CleanMis((chosenIndex >= 0 && chosenIndex < 4) ? currentQuestion.misconceptionTags[chosenIndex] : "Waktu habis sebelum menjawab");

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
                timeLimitSec = NoTimer ? 0f : questionMaxTime,
                misconceptionTag = misTag
            });

            lastResolutionGrade = grade;
            if (isChallengeForSpell && pendingSpell != null) ResolveSpellCastOutcome(isCorrect, grade, elapsed, misTag);
            else ResolveActiveDefendOutcome(isCorrect, grade, elapsed, misTag);
        }

        private void ResolveSpellCastOutcome(bool isCorrect, string grade, float elapsed, string misTag)
        {
            var spell = pendingSpell;
            if (isCorrect)
            {
                GrimoireAudioManager.Instance?.PlaySfx("magic");
                float speedBonus = grade == "PERFECT GUARD" ? 1.20f : 1.0f;
                float defFactor = spell.id == "order_restored" ? 0f : (spell.id == "binary_pierce" ? 0.5f : 1.0f);
                float intMul = 1f + TotalInt / 40f;
                bool weak = monstersAnalyzed.Contains(activeMonster.id) && activeMonster.tpPool.Contains(spell.tpCode);
                float raw = (TotalAtk * spell.multiplier * speedBonus * intMul) - (MonsterDef_ * 0.4f * defFactor);
                if (weak) raw *= 1.5f;
                int dmg = DealToMonster(Mathf.Max(8, Mathf.RoundToInt(raw)), false, out string note);
                if (weak) note += " KELEMAHAN!";
                focus = Mathf.Min(100, focus + (grade == "PERFECT GUARD" ? 25 : 15));
                _heroAnim.Play("attack");
                Schedule(0.27f, () => HitMonster(dmg, "arcane", new Color(0.82f, 0.66f, 1f)));

                // spell side-effects mimic the algorithm (GDD §6)
                string extra = "";
                switch (spell.id)
                {
                    case "bubble_shield":
                        hp = Mathf.Min(maxHp, hp + 25);
                        _pStatus.Remove("poison"); _pStatus.Remove("torn_page");
                        SpawnVfx("heal", _heroRect, 0.9f);
                        extra = "\n+25 HP, Racun & Halaman Robek hilang.";
                        break;
                    case "selection_strike":
                        _mStatus["marked"] = 2;
                        extra = "\nMusuh DITANDAI: serangan berikutnya kritis.";
                        break;
                    case "insertion_lance":
                        _pStatus["ward"] = 9;
                        extra = "\nInsertion Ward aktif: damage berikutnya -30%.";
                        break;
                    case "order_restored":
                        hp = Mathf.Min(maxHp, hp + Mathf.RoundToInt(maxHp * 0.30f));
                        foreach (var k in new[] { "poison", "silence", "torn_page", "blind", "buried", "crushed", "split" }) _pStatus.Remove(k);
                        _mStatus["ordered"] = GrimoireStatus.Duration("ordered");
                        SpawnVfx("heal", _heroRect, 1.0f);
                        extra = "\nSemua debuff hilang, +30% HP. Musuh TERURUT: DEF -25%, soal +2 detik.";
                        break;
                }

                lastResolutionTitle = $"{spell.name.ToUpper()} BERHASIL!";
                lastResolutionDetail = $"{{#2a1a10}}{dmg} damage{note}.{extra}{{/}}\n\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(0.16f, 0.48f, 0.22f);
                AddCombatLog($"[MANTRA] {spell.name}: {{#d0a0ff}}{dmg}{{/}} damage{note}.");
            }
            else
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                int fizzleDmg = DealToMonster(Mathf.Max(3, Mathf.RoundToInt(TotalAtk * 0.4f)), false, out _);
                _heroAnim.Play("idle");
                SpawnVfx("spark", _monRect, 0.4f, new Color(0.7f, 0.6f, 1f, 0.8f));
                SpawnText($"{fizzleDmg}", _monRect, new Color(0.75f, 0.7f, 0.9f), 1);
                if (activeMonster.id == "hydra_of_division") _mStatus["regen"] = 1;

                lastResolutionTitle = "MANTRA GAGAL (FIZZLE)";
                lastResolutionDetail = $"Hanya {fizzleDmg} damage. Diagnosis: {{#8a1c1c}}{misTag}{{/}}\n\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(0.62f, 0.14f, 0.14f);
                AddCombatLog($"[GAGAL] {spell.name} ({misTag}).");
            }

            CheckBossPhaseTransition();
            if (monsterHp <= 0) { HandleBattleVictory(); return; }
            if (!isCorrect && tpFailStreaks.TryGetValue(currentQuestion.tp, out int streak) && streak >= 3)
            {
                TriggerMercyGuidance(currentQuestion.tp);
                return;
            }
            battlePhase = BattlePhase.TurnResolution;
        }

        private void ResolveActiveDefendOutcome(bool isCorrect, string grade, float elapsed, string misTag)
        {
            // Easy: no clock, so any correct answer is a Perfect Guard
            if (difficulty == Difficulty.Easy && grade == "GUARD") grade = "PERFECT GUARD";
            lastResolutionGrade = grade;

            if (grade == "BREAK" && equippedAccessory == "acc_aegis_charm" && !aegisCharmUsedThisBattle)
            {
                aegisCharmUsedThisBattle = true;
                grade = lastResolutionGrade = "GUARD";
                AddCombatLog("[AEGIS CHARM] Jimat menahan Break!");
            }

            _monAnim.Release();
            Rect heroR = _heroRect;
            string volleyTag = _volleyTotal > 1 ? $" ({_volleyTotal - _volleyLeft}/{_volleyTotal})" : "";

            if (grade == "PERFECT GUARD")
            {
                GrimoireAudioManager.Instance?.PlaySfx("parry");
                int counterRaw = Mathf.Max(10, Mathf.RoundToInt(TotalAtk * 1.5f - MonsterDef_ * 0.3f));
                int counterDmg = DealToMonster(counterRaw, true, out string note);
                focus = Mathf.Min(100, focus + 25);
                logicFragments++;
                if (panicStacks > 0) panicStacks--;

                Schedule(0.18f, () =>
                {
                    _heroAnim.Play("parry");
                    SpawnVfx("hex_shield", heroR, 1.1f);
                    SpawnText("AEGIS ACTIVE!", heroR, PixelUI.Cyan, 1, -16f);
                    Flash(new Color(0.5f, 0.8f, 1f, 0.25f), 0.15f);
                });
                Schedule(0.45f, () => HitMonster(counterDmg, "slash", PixelUI.Gold));

                lastResolutionTitle = "PERFECT GUARD - AEGIS ACTIVE!" + volleyTag;
                lastResolutionDetail = $"0 damage diterima. Serangan balik {counterDmg}{note}. +1 Logic Fragment, +25 Focus{(NoTimer ? "" : $" ({elapsed:F1} dtk)")}.\n\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(0.55f, 0.38f, 0.04f);
                AddCombatLog($"{{#ffd86a}}PERFECT GUARD!{{/}} Balasan {counterDmg} damage.");
            }
            else if (grade == "GUARD")
            {
                GrimoireAudioManager.Instance?.PlaySfx("guard");
                focus = Mathf.Min(100, focus + 10);
                Schedule(0.18f, () =>
                {
                    _heroAnim.Play("defend");
                    SpawnVfx("spark", new Rect(heroR.xMax - heroR.width * 0.35f, heroR.y + heroR.height * 0.3f, heroR.width * 0.4f, heroR.height * 0.4f), 1.2f, new Color(0.75f, 0.95f, 1f));
                    SpawnText("GUARD", heroR, PixelUI.Cyan, 1, -10f);
                });
                lastResolutionTitle = "GUARD - SERANGAN DITAHAN" + volleyTag;
                lastResolutionDetail = $"Benar, tetapi sisa waktu < 60%. 0 damage, +10 Focus. Jawab lebih cepat untuk Perfect Guard!\n\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(0.10f, 0.34f, 0.50f);
                AddCombatLog("{#7fd8ff}GUARD:{/} serangan ditahan.");
            }
            else
            {
                // GDD §7 Lethal Strike: min(3 x ATK - DEF, cap) ; Easy cap 25%, otherwise 50% max HP
                GrimoireAudioManager.Instance?.PlaySfx("hit");
                int atk = monsterAtk + (bossPhase - 1) * 4;
                float crit = HasM("false_pivot") ? 1.35f : 1.0f;
                int formula = Mathf.RoundToInt((3 * atk - TotalDef) * crit);
                int cap = Mathf.RoundToInt(maxHp * (difficulty == Difficulty.Easy ? 0.25f : 0.5f));
                int dmg = Mathf.Clamp(formula, 1, cap);
                if (_volleyTotal > 1) dmg = Mathf.Max(1, dmg / _volleyTotal);          // each arrow = 1/3 Break
                bool warded = HasP("ward");
                if (warded) { dmg = Mathf.RoundToInt(dmg * 0.7f); _pStatus.Remove("ward"); }
                hp = Mathf.Max(0, hp - dmg);
                panicStacks = Mathf.Min(3, panicStacks + 1);

                string statusMsg = ApplyBreakStatus();
                Schedule(0.18f, () =>
                {
                    _heroAnim.Play("hurt", "idle");
                    SpawnVfx("shatter", heroR, 1.0f);
                    SpawnText($"-{dmg}", heroR, PixelUI.Red, 2);
                    Shake(6f, 0.35f);
                    Flash(new Color(0.85f, 0.05f, 0.08f, 0.35f), 0.35f);
                });

                lastResolutionTitle = "BREAK! PERISAI PECAH" + volleyTag;
                lastResolutionDetail = $"-{dmg} HP{(warded ? " (Ward -30%)" : "")}, Panik {panicStacks}/3.{statusMsg}\nDiagnosis: {{#8a1c1c}}{misTag}{{/}}\n\n{currentQuestion.explanation}";
                lastResolutionColor = new Color(0.62f, 0.10f, 0.10f);
                AddCombatLog($"{{#f05a50}}BREAK!{{/}} -{dmg} HP.");
            }

            CheckBossPhaseTransition();
            if (monsterHp <= 0) { HandleBattleVictory(); return; }

            if (hp <= 0)
            {
                // Scholar's Resilience: the knight falls, the Shade of the Order revives them with Mercy guidance
                _heroAnim.Play("death", "idle");
                hp = Mathf.RoundToInt(maxHp * 0.5f);
                mp = Mathf.RoundToInt(maxMp * 0.5f);
                _volleyLeft = 0;
                TriggerMercyGuidance(currentQuestion.tp);
                return;
            }

            if (!isCorrect && tpFailStreaks.TryGetValue(currentQuestion.tp, out int streak) && streak >= 3)
            {
                _volleyLeft = 0;
                TriggerMercyGuidance(currentQuestion.tp);
                return;
            }
            battlePhase = BattlePhase.TurnResolution;
        }

        /// <summary>Monster-specific failure effect (GDD §9). Returns text for the result panel.</summary>
        private string ApplyBreakStatus()
        {
            string id = GrimoireStatus.OnBreak(activeMonster.id, bossPhase);
            if (activeMonster.id == "hydra_of_division") { _mStatus["regen"] = 1; return " Hydra akan beregenerasi."; }
            if (id == null) return "";
            if ((id == "silence" || id == "poison") && equippedAccessory == "acc_pendant") return " (Pendant menangkal efek.)";
            if (id == "torn_page" && equippedAccessory == "acc_cloak") return " (Cloak menangkal Halaman Robek.)";

            var info = GrimoireStatus.Get(id);
            if (id == "death_seal")
            {
                int seals = PVal("death_seal") + 1;
                if (seals >= 3)
                {
                    hp = Mathf.Min(hp, 1);
                    _pStatus["death_seal"] = 0;
                    Schedule(0.5f, () => { SpawnText("DEATH SENTENCE", _heroRect, PixelUI.Red, 1, -22f); Shake(10f, 0.4f); });
                    return " {#8a1c1c}3 Segel menyala: DEATH SENTENCE - HP tersisa 1!{/}";
                }
                _pStatus["death_seal"] = seals;
                return $" Segel Kematian {seals}/3 menyala.";
            }
            _pStatus[id] = GrimoireStatus.Duration(id);
            if (id == "poison") Schedule(0.4f, () => SpawnVfx("poison", _heroRect, 0.8f));
            return $" Terkena {{#8a1c1c}}{info?.name}{{/}}: {info?.effect}";
        }

        /// <summary>"Next" on the result panel: more arrows, the enemy's turn, or back to commands.</summary>
        public void ContinueFromResolution()
        {
            if (battlePhase != BattlePhase.TurnResolution) return;
            if (isChallengeForSpell)
            {
                battlePhase = BattlePhase.Animating;
                Schedule(0.2f, StartEnemyTurn);
                return;
            }
            if (_volleyLeft > 0)
            {
                AddCombatLog($"Serangan beruntun berikutnya!");
                BeginAlgorithmicChallenge(false, null);
                return;
            }
            EndEnemyTurn();
        }

        private void EndEnemyTurn()
        {
            // poison damage, then every timed status ticks down
            if (HasP("poison"))
            {
                int psn = Mathf.Max(2, Mathf.RoundToInt(maxHp * 0.05f));
                hp = Mathf.Max(1, hp - psn);
                SpawnVfx("poison", _heroRect, 0.8f);
                SpawnText($"-{psn}", _heroRect, new Color(0.6f, 0.9f, 0.4f), 1);
                AddCombatLog($"{{#8ae05a}}Racun{{/}} -{psn} HP.");
            }
            foreach (var k in _pStatus.Keys.ToList())
            {
                var info = GrimoireStatus.Get(k);
                if (info != null && info.stacks) continue;
                if (k == "ward") continue;            // consumed on hit
                _pStatus[k]--;
                if (_pStatus[k] <= 0) _pStatus.Remove(k);
            }
            foreach (var k in _mStatus.Keys.ToList())
            {
                if (_mStatus[k] >= 90) continue;      // passives
                if (k == "marked") continue;
                _mStatus[k]--;
                if (_mStatus[k] <= 0) _mStatus.Remove(k);
            }
            _heroAnim.Play("idle");
            battlePhase = BattlePhase.PlayerCommand;
        }

        private void CheckBossPhaseTransition()
        {
            if (activeMonster == null || monsterHp <= 0) return;
            float ratio = (float)monsterHp / Mathf.Max(1, monsterMaxHp);
            int newPhase = bossPhase;
            if (activeMonster.id == "entropy_chaos_king")
            {
                if (ratio <= 0.33f) newPhase = 3;
                else if (ratio <= 0.66f) newPhase = Mathf.Max(newPhase, 2);
            }
            else if (isBossEncounter && ratio <= 0.50f) newPhase = Mathf.Max(newPhase, 2);
            if (newPhase == bossPhase) return;

            bossPhase = newPhase;
            _mStatus["enraged"] = 90 + bossPhase;
            if (activeMonster.id == "the_blind_archivist") _mStatus["fog"] = 99;
            Schedule(0.5f, () =>
            {
                if (AnimLibrary.Durations(activeMonster.id + "_enrage") != null) _monAnim.Play("enrage", MonIdle);
                SpawnText($"FASE {bossPhase}!", _monRect, new Color(1f, 0.45f, 0.25f), 2, -24f);
                Shake(5f, 0.5f);
                Flash(new Color(1f, 0.3f, 0.1f, 0.2f), 0.3f);
                GrimoireAudioManager.Instance?.PlaySfx("magic");
            });
            AddCombatLog($"{{#ff7a40}}{activeMonster.name} memasuki FASE {bossPhase}!{{/}}");
        }

        private void TriggerMercyGuidance(string tp)
        {
            tpFailStreaks[tp] = 0;
            if (ddaEnabled) ddaTier = "Assisted";
            int healHp = Mathf.RoundToInt(maxHp * 0.25f);
            int healMp = Mathf.RoundToInt(maxMp * 0.30f);
            hp = Mathf.Min(maxHp, hp + healHp);
            mp = Mathf.Min(maxMp, mp + healMp);
            mercyCodexEntry = GrimoireDatabase.Codex.FirstOrDefault(c => c.tp == tp) ?? GrimoireDatabase.Codex[0];
            codexOpened.Add(mercyCodexEntry.tp);
            GrimoireAudioManager.Instance?.PlaySfx("heal");
            AddCombatLog($"Bayang-bayang Ordo menolong: +{healHp} HP, +{healMp} MP.");
            battlePhase = BattlePhase.MercyIntervention;
        }

        public void CloseMercy()
        {
            _heroAnim.Play("idle");
            battlePhase = BattlePhase.PlayerCommand;
        }

        // =====================================================================
        // VICTORY
        // =====================================================================
        private void HandleBattleVictory()
        {
            battlePhase = BattlePhase.Animating;
            Schedule(0.65f, () => _monAnim.Play("death", "death"));
            Schedule(0.80f, () => _heroAnim.Play("victory", "idle"));
            Schedule(1.95f, FinishBattleVictory);
        }

        private void FinishBattleVictory()
        {
            panicStacks = 0;
            _pStatus.Remove("death_seal");
            winFlawless = battleFlawless;
            winExp = activeMonster.exp + (battleFlawless ? Mathf.RoundToInt(activeMonster.exp * 0.2f) : 0);
            winGold = activeMonster.gold;
            winFrag = battleFlawless ? 1 : 0;
            logicFragments += winFrag;
            winLevelUp = GainExpAndGold(winExp, winGold);
            if (_battleTile != null) _battleTile.cleared = true;
            GrimoireAudioManager.Instance?.PlaySfx("victory");
            battlePhase = BattlePhase.BattleWon;
        }

        public void ContinueAfterVictory()
        {
            if (battlePhase != BattlePhase.BattleWon) return;
            if (activeMonster.id == "entropy_chaos_king")
            {
                viewMode = GameViewMode.Victory;
                GrimoireAudioManager.Instance?.PlayMusic("music_title");
                return;
            }
            if (activeMonster.id == "the_blind_archivist" && currentAct == 1) { StartActExpedition(2); return; }
            if (activeMonster.id == "the_abyss_golem" && currentAct == 2) { StartActExpedition(3); return; }

            viewMode = GameViewMode.Board;
            GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey);
            boardBannerMessage = $"{{#8ae05a}}MENANG!{{/}} {activeMonster.name} dikalahkan (+{winExp} EXP, +{winGold} Gold).";
        }

        // =====================================================================
        // FX helpers
        // =====================================================================
        private void Schedule(float delay, Action a) => _scheduled.Add(new KeyValuePair<float, Action>(Time.time + delay, a));

        private void SpawnVfx(string name, Rect anchor, float scale = 1f, Color? tint = null)
        {
            float size = Mathf.Round(Mathf.Max(anchor.width, anchor.height) * scale);
            _vfx.Add(new VfxInstance { name = name, center = anchor.center, size = size, start = Time.time, tint = tint ?? Color.white });
        }

        /// <summary>scale: 1 = normal pixel font, 2 = big damage numbers.</summary>
        private void SpawnText(string text, Rect anchor, Color col, int scale = 1, float yOff = 0f)
        {
            _floaters.Add(new FloatingText
            {
                text = text, color = col, fontSize = scale, start = Time.time,
                pos = new Vector2(anchor.center.x + UnityEngine.Random.Range(-8f, 8f), anchor.y + anchor.height * 0.25f + yOff)
            });
        }

        private void Shake(float mag, float dur)
        {
            if (accessibilityNoShake) return;
            _shakeMag = mag;
            _shakeUntil = Time.time + dur;
        }

        private void Flash(Color c, float dur)
        {
            if (accessibilityNoShake) return;
            _flashColor = c;
            _flashUntil = Time.time + dur;
        }

        private void HitMonster(int dmg, string vfx, Color numberCol)
        {
            _monAnim.Play("hurt", monsterHp <= 0 ? "death" : MonIdle);
            SpawnVfx(vfx, _monRect, 0.9f);
            SpawnVfx("spark", _monRect, 0.5f);
            SpawnText(dmg.ToString(), _monRect, numberCol, 2);
            Shake(2f, 0.12f);
        }
    }
}
