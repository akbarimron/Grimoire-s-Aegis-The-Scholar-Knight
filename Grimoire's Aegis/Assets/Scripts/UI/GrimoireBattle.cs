using System;
using System.Collections.Generic;
using System.Linq;
using GrimoireAegis.Core;
using UnityEngine;

namespace GrimoireAegis.UI
{
    /// <summary>
    /// Turn-based battle (GDD §6-§9). Answer flow: pick an answer -> the attack plays out
    /// (blocked or hits) -> only then a short card says right/wrong and shows the correct answer.
    /// </summary>
    public partial class GrimoireGameController
    {
        // --- Monster & battle state ---
        [NonSerialized] public MonsterDef activeMonster;
        public int monsterMaxHp = 100;
        public int monsterHp = 100;
        public int monsterAtk = 10;
        public int bossPhase = 1;
        public bool isBossEncounter = false;
        public bool aegisCharmUsedThisBattle = false;
        public bool battleFlawless = true;
        private int _enemyTurn;
        private int _volleyLeft, _volleyTotal = 1;
        private MapNode _battleNode;

        // --- Status effects: id -> turns (or stacks) ---
        private readonly Dictionary<string, int> _pStatus = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _mStatus = new Dictionary<string, int>();
        public int panicStacks = 0;

        public bool HasP(string id) => _pStatus.TryGetValue(id, out int v) && v > 0;
        public bool HasM(string id) => _mStatus.TryGetValue(id, out int v) && v > 0;
        public int PVal(string id) => _pStatus.TryGetValue(id, out int v) ? v : 0;

        public IEnumerable<KeyValuePair<string, int>> PlayerStatuses()
        {
            foreach (var kv in _pStatus) if (kv.Value > 0) yield return kv;
            if (panicStacks > 0) yield return new KeyValuePair<string, int>("panic", panicStacks);
            if (hp > 0 && hp < maxHp * 0.2f) yield return new KeyValuePair<string, int>("adrenaline", 0);
        }

        public IEnumerable<KeyValuePair<string, int>> MonsterStatuses()
        {
            foreach (var kv in _mStatus) if (kv.Value > 0) yield return kv;
        }

        // --- Question state ---
        public bool isChallengeForSpell = false;
        [NonSerialized] public SpellDef pendingSpell = null;
        [NonSerialized] public QuestionPayload currentQuestion = null;
        public float questionTimer = 0f;
        public float questionMaxTime = 12f;
        public bool NoTimer => difficulty == Difficulty.Easy;
        public List<int> eliminatedOptions = new List<int>();
        public bool hintUsedOnQuestion = false;
        public int obscuredOptionIdx = -1;
        [NonSerialized] public CodexEntry mercyCodexEntry = null;
        private float _readingUntil;
        private float _blindUntil;
        private int[] _optionOrder = { 0, 1, 2, 3 };
        private const float PerfectGuardRemaining = 0.60f;
        private int _lockedChoice = -1;          // the answer picked, shown while the attack plays

        // --- Reveal card (after the attack) ---
        public bool revealCorrect;
        public string revealTitle = "", revealEffect = "", revealChosen = "", revealAnswer = "", revealExplain = "";
        public Color revealColor = Color.white;
        private bool _pendingVictory;
        private string _pendingMercyTp;

        // --- Victory summary ---
        public int winExp, winGold, winFrag;
        public bool winLevelUp, winFlawless, winKey;

        // --- Animation & FX ---
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
        private string _ribbon = "";
        private float _ribbonAt;

        private string MonIdle => (isBossEncounter && bossPhase >= 2 && AnimLibrary.Durations(activeMonster.id + "_idle2") != null) ? "idle2" : "idle";

        /// <summary>One short line at the top of the battle stage (replaces the combat log).</summary>
        private void Ribbon(string msg) { _ribbon = msg; _ribbonAt = Time.time; }
        public string RibbonText => Time.time - _ribbonAt < 3.5f ? _ribbon : "";

        // =====================================================================
        // ENCOUNTER
        // =====================================================================
        public void StartBattleEncounter(string monsterId, bool isElite, MapNode node = null)
        {
            if (!GrimoireDatabase.Monsters.TryGetValue(monsterId, out MonsterDef def)) def = GrimoireDatabase.Monsters["corrupted_slime"];
            activeMonster = def;
            float hpMul = difficulty == Difficulty.Hard ? 1.15f : (difficulty == Difficulty.Easy ? 0.9f : 1f);
            monsterMaxHp = Mathf.RoundToInt(def.hp * hpMul);
            monsterHp = monsterMaxHp;
            monsterAtk = Mathf.RoundToInt(def.atk * (difficulty == Difficulty.Hard ? 1.15f : 1f));
            _monHpShown = 1f;
            bossPhase = 1;
            isBossEncounter = isElite || def.role != "Regular";
            aegisCharmUsedThisBattle = false;
            battleFlawless = true;
            _enemyTurn = 0;
            _battleNode = node;
            _pendingVictory = false; _pendingMercyTp = null;
            monstersSeen.Add(def.id);

            foreach (var k in new[] { "silence", "blind", "buried", "split", "death_seal", "ward" }) _pStatus.Remove(k);
            _mStatus.Clear();
            if (def.id == "the_abyss_golem") _mStatus["armored"] = 99;
            if (def.id == "pivot_wraith") _mStatus["false_pivot"] = 99;

            _scheduled.Clear(); _vfx.Clear(); _floaters.Clear();
            _monAnim.actor = def.id;
            _monAnim.Play("spawn", "idle");
            _heroAnim.Play("idle");
            _cmdSel = 0;

            viewMode = GameViewMode.Battle;
            activeModal = ModalOverlay.None;
            battlePhase = BattlePhase.Animating;
            Ribbon($"{def.name} menghadang!");
            Schedule(0.7f, () => { battlePhase = BattlePhase.PlayerCommand; TipFirstBattle(); });
            GrimoireAudioManager.Instance?.PlayMusic("music_battle");
            GrimoireAudioManager.Instance?.PlaySfx("select");
        }

        // =====================================================================
        // PLAYER COMMANDS
        // =====================================================================
        private int MonsterDefense => Mathf.RoundToInt(activeMonster.def * (HasM("ordered") ? 0.75f : 1f));

        private int DealToMonster(int dmg, bool physical, out string note)
        {
            note = "";
            if (physical && HasM("armored")) { dmg = Mathf.RoundToInt(dmg * 0.30f); note = " (zirah batu)"; }
            if (HasM("marked")) { dmg = Mathf.RoundToInt(dmg * 1.5f); _mStatus.Remove("marked"); note += " Kritis!"; }
            if (HasP("split")) { dmg = Mathf.RoundToInt(dmg * 0.5f); _pStatus.Remove("split"); }
            dmg = Mathf.Max(1, dmg);
            monsterHp = Mathf.Max(0, monsterHp - dmg);
            return dmg;
        }

        public void ExecuteBasicAttack()
        {
            if (battlePhase != BattlePhase.PlayerCommand || DialogOpen) return;
            battlePhase = BattlePhase.Animating;
            _heroAnim.Play("attack");
            Schedule(0.27f, () =>
            {
                GrimoireAudioManager.Instance?.PlaySfx("hit");
                int raw = Mathf.Max(4, Mathf.RoundToInt((TotalAtk - MonsterDefense * 0.45f) * UnityEngine.Random.Range(0.9f, 1.1f)));
                int dmg = DealToMonster(raw, true, out string note);
                focus = Mathf.Min(100, focus + 12);
                HitMonster(dmg, "slash", new Color(1f, 0.95f, 0.8f));
                if (note.Length > 0) Ribbon("Tebasan" + note);
                CheckBossPhaseTransition();
                if (monsterHp <= 0) { Schedule(0.5f, HandleBattleVictory); return; }
                Schedule(0.6f, StartEnemyTurn);
            });
        }

        /// <summary>Spells unlock Act by Act (GDD §6); the Limit Break appears when Focus is full.</summary>
        public List<SpellDef> SpellsAvailable()
        {
            var unlocked = new HashSet<string> { "linear_slash", "binary_pierce" };
            int act = Mathf.Max(currentAct, activeMonster != null ? activeMonster.act : 1);
            if (act >= 2) { unlocked.Add("bubble_shield"); unlocked.Add("selection_strike"); unlocked.Add("insertion_lance"); }
            if (act >= 3) { unlocked.Add("merge_tempest"); unlocked.Add("quick_judgment"); }
            if (focus >= 100) unlocked.Add("order_restored");
            return GrimoireDatabase.Spells.Where(s => unlocked.Contains(s.id)).ToList();
        }

        public bool SpellLocked(SpellDef s) => HasP("silence") && s.focusCost == 0;
        public bool CanCast(SpellDef s) => !SpellLocked(s) && mp >= GetEffectiveMpCost(s) && focus >= s.focusCost;

        public void BeginSpellCast(SpellDef spell)
        {
            if (!CanCast(spell))
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                Ribbon(SpellLocked(spell) ? "Kamu sedang Bisu, mantra terkunci." : "MP tidak cukup.");
                return;
            }
            mp -= GetEffectiveMpCost(spell);
            focus = Mathf.Max(0, focus - spell.focusCost);
            GrimoireAudioManager.Instance?.PlaySfx("magic");
            _heroAnim.Play("cast", "cast");
            SpawnVfx("arcane", _heroRect, 0.8f);
            BeginAlgorithmicChallenge(true, spell);
        }

        public int GetEffectiveMpCost(SpellDef spell) =>
            equippedAccessory == "acc_ring_comp" ? Mathf.Max(0, Mathf.RoundToInt(spell.mpCost * 0.80f)) : spell.mpCost;

        /// <summary>Monster info card. Free: opened by clicking the monster's name.</summary>
        public void AnalyzeMonster()
        {
            if (battlePhase != BattlePhase.PlayerCommand || DialogOpen) return;
            monstersAnalyzed.Add(activeMonster.id);
            GrimoireAudioManager.Instance?.PlaySfx("select");
            battlePhase = BattlePhase.Analyze;
        }

        public void CloseAnalyze()
        {
            if (battlePhase == BattlePhase.Analyze) battlePhase = BattlePhase.PlayerCommand;
        }

        public float FleeChance => 0.50f + (equippedAccessory == "acc_boots" ? 0.15f : 0f);

        public void TryFlee()
        {
            if (battlePhase != BattlePhase.PlayerCommand || DialogOpen) return;
            if (isBossEncounter)
            {
                GrimoireAudioManager.Instance?.PlaySfx("error");
                Ribbon("Tidak bisa kabur dari pertarungan ini!");
                return;
            }
            if (UnityEngine.Random.value < FleeChance)
            {
                GrimoireAudioManager.Instance?.PlaySfx("cancel");
                _scheduled.Clear();
                StepBackAfterFlee();
                viewMode = GameViewMode.Board;
                GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey);
                boardMessage = "Aegis mundur selangkah. Monsternya masih menunggu.";
                return;
            }
            Ribbon("Gagal kabur!");
            battlePhase = BattlePhase.Animating;
            Schedule(0.4f, StartEnemyTurn);
        }

        public bool ItemsLocked => HasP("buried");

        public void UseConsumableItem(string itemId)
        {
            if (!consumables.TryGetValue(itemId, out int count) || count <= 0) return;
            if (!GrimoireDatabase.Items.TryGetValue(itemId, out ItemDef item)) return;
            bool inBattle = viewMode == GameViewMode.Battle;
            if (inBattle && ItemsLocked) { GrimoireAudioManager.Instance?.PlaySfx("error"); Ribbon("Tasmu terkubur!"); return; }

            consumables[itemId] = count - 1;
            GrimoireAudioManager.Instance?.PlaySfx("heal");
            if (item.healHp > 0) hp = Mathf.Min(maxHp, hp + item.healHp);
            if (item.healMp > 0) mp = Mathf.Min(maxMp, mp + item.healMp);
            if (itemId == "pot_hp" || itemId == "pot_elixir") _pStatus.Remove("poison");
            if (itemId == "pot_mp" || itemId == "pot_elixir") _pStatus.Remove("silence");
            if (itemId == "pot_elixir") { _pStatus.Remove("torn_page"); _pStatus.Remove("crushed"); }
            if (itemId == "scroll_clarity") focus = Mathf.Min(100, focus + 50);

            if (!inBattle) return;
            SpawnVfx("heal", _heroRect, 0.9f);
            if (item.healHp > 0) SpawnText($"+{item.healHp}", _heroRect, PixelUI.Green, 2);
            if (item.healMp > 0) SpawnText($"+{item.healMp} MP", _heroRect, PixelUI.Cyan, 1, -12f);
            battlePhase = BattlePhase.Animating;
            Schedule(0.7f, StartEnemyTurn);       // GDD §6: an item uses the turn
        }

        // =====================================================================
        // ENEMY TURN -> QUESTION
        // =====================================================================
        private void StartEnemyTurn()
        {
            if (monsterHp <= 0) return;
            _enemyTurn++;
            if (HasM("regen"))
            {
                int heal = Mathf.RoundToInt(monsterMaxHp * 0.10f);
                monsterHp = Mathf.Min(monsterMaxHp, monsterHp + heal);
                SpawnVfx("heal", _monRect, 0.9f);
                SpawnText($"+{heal}", _monRect, PixelUI.Green, 2);
            }
            _volleyTotal = _volleyLeft = GrimoireStatus.Volley(activeMonster.id);
            Ribbon(_volleyTotal > 1 ? $"{activeMonster.name} menyerang {_volleyTotal}x berturut-turut!" : $"{activeMonster.name} bersiap menyerang!");
            TipFirstDefend(() => BeginAlgorithmicChallenge(false, null));
        }

        public void BeginAlgorithmicChallenge(bool forSpell, SpellDef spell)
        {
            isChallengeForSpell = forSpell;
            pendingSpell = spell;
            eliminatedOptions.Clear();
            hintUsedOnQuestion = false;
            _lockedChoice = -1;
            if (!forSpell) _volleyLeft = Mathf.Max(0, _volleyLeft - 1);

            string tp = forSpell && spell != null ? spell.tpCode
                      : activeMonster.tpPool[UnityEngine.Random.Range(0, activeMonster.tpPool.Length)];
            currentQuestion = ProceduralQuestionEngine.GenerateQuestion(tp, ddaEnabled ? ddaTier : "Standard", TotalSpd, AccessoryTimerBonus);

            float t = currentQuestion.timeLimitSec;
            if (difficulty == Difficulty.Hard) t *= 0.8f;
            t *= timerMultiplier;
            if (HasM("ordered")) t += 2f;
            if (hp < maxHp * 0.20f) t += 1f;
            questionMaxTime = questionTimer = t;

            float reading = difficulty == Difficulty.Hard ? 1.0f : 1.5f;
            _readingUntil = Time.time + (NoTimer ? 0f : reading);
            _popupStart = Time.time;
            _blindUntil = HasP("blind") ? Time.time + (NoTimer ? 0f : reading) + 1.0f : 0f;
            _pStatus.Remove("blind");

            _optionOrder = new[] { 0, 1, 2, 3 };
            if (panicStacks >= 3)
                for (int i = 3; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); (_optionOrder[i], _optionOrder[j]) = (_optionOrder[j], _optionOrder[i]); }

            obscuredOptionIdx = -1;
            if (HasP("torn_page") && equippedAccessory != "acc_cloak")
                do obscuredOptionIdx = UnityEngine.Random.Range(0, 4); while (obscuredOptionIdx == currentQuestion.correctIndex);

            if (!forSpell)
            {
                bool skill = GrimoireStatus.OnBreak(activeMonster.id, bossPhase) != null && _enemyTurn % 2 == 0
                             && AnimLibrary.Durations(activeMonster.id + "_cast") != null;
                _monAnim.Play(skill ? "cast" : "attack", MonIdle, hold: skill ? 2 : 1);   // frozen mid-swing
                _heroAnim.Play("defend", "defend");
            }
            _cmdSel = 0;
            battlePhase = BattlePhase.AlgorithmicChallenge;
        }

        public bool OptionsHidden => Time.time < _blindUntil;
        public bool Reading => Time.time < _readingUntil;
        public int LockedChoice => _lockedChoice;

        private void TickQuestionTimer()
        {
            if (viewMode != GameViewMode.Battle || battlePhase != BattlePhase.AlgorithmicChallenge) return;
            if (activeModal != ModalOverlay.None || DialogOpen || NoTimer) return;
            if (Time.time < _readingUntil) return;
            questionTimer -= Time.deltaTime;
            if (questionTimer <= 0f) { questionTimer = 0f; SubmitAnswer(-1); }
        }

        public int HintCost => difficulty == Difficulty.Easy ? 5 : 10;

        public void UseScholarHint()
        {
            if (currentQuestion == null || hintUsedOnQuestion || battlePhase != BattlePhase.AlgorithmicChallenge) return;
            if (mp < HintCost) { GrimoireAudioManager.Instance?.PlaySfx("error"); return; }
            mp -= HintCost;
            hintUsedOnQuestion = true;
            GrimoireAudioManager.Instance?.PlaySfx("magic");
            int toEliminate = equippedAccessory == "acc_monocle" ? 2 : 1;
            for (int i = 0; i < 4 && eliminatedOptions.Count < toEliminate; i++)
                if (i != currentQuestion.correctIndex && !eliminatedOptions.Contains(i)) eliminatedOptions.Add(i);
        }

        public void SubmitSlot(int slot)
        {
            if (slot < 0 || slot > 3 || OptionsHidden) return;
            int real = _optionOrder[slot];
            if (eliminatedOptions.Contains(real)) return;
            SubmitAnswer(real);
        }

        public static string CleanMis(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return "-";
            return tag.StartsWith("Miskonsepsi ") ? tag.Substring(12) : tag;
        }

        /// <summary>First sentence of the explanation: enough to learn from, short enough to read.</summary>
        private static string Short(string s, int max = 170)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int dot = s.IndexOf(". ", StringComparison.Ordinal);
            string first = dot > 20 && dot < max ? s.Substring(0, dot + 1) : s;
            return first.Length > max ? first.Substring(0, max - 3).TrimEnd() + "..." : first;
        }

        /// <summary>Locks the answer. Nothing is revealed yet: the attack plays out first.</summary>
        public void SubmitAnswer(int chosenIndex)
        {
            if (currentQuestion == null || battlePhase != BattlePhase.AlgorithmicChallenge || DialogOpen) return;

            float elapsed = NoTimer ? Time.time - _popupStart : Mathf.Clamp(questionMaxTime - questionTimer, 0.1f, questionMaxTime);
            float remaining = NoTimer ? 1f : Mathf.Clamp01(questionTimer / Mathf.Max(0.1f, questionMaxTime));
            bool correct = chosenIndex == currentQuestion.correctIndex;
            string tp = currentQuestion.tp;
            if (!tpFailStreaks.ContainsKey(tp)) tpFailStreaks[tp] = 0;

            string grade;
            if (correct)
            {
                consecutiveCorrect++; consecutiveWrong = 0; tpFailStreaks[tp] = 0;
                if (ddaEnabled && consecutiveCorrect >= 3) ddaTier = ddaTier == "Assisted" ? "Standard" : "Mastery";
                grade = (remaining >= PerfectGuardRemaining || NoTimer) ? "PERFECT GUARD" : "GUARD";
            }
            else
            {
                consecutiveWrong++; consecutiveCorrect = 0; tpFailStreaks[tp]++;
                battleFlawless = false;
                if (ddaEnabled && consecutiveWrong >= 2) ddaTier = ddaTier == "Mastery" ? "Standard" : "Assisted";
                grade = "BREAK";
            }
            string misTag = CleanMis(chosenIndex >= 0 && chosenIndex < 4 ? currentQuestion.misconceptionTags[chosenIndex] : "Waktu habis");

            telemetryLog.Add(new TelemetryRecord
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), act = currentAct,
                monsterName = activeMonster != null ? activeMonster.name : "Latihan", tp = tp, bloom = currentQuestion.bloom,
                formatCode = currentQuestion.formatCode, ddaTier = ddaTier, isCorrect = correct, defenceGrade = grade,
                responseTimeSec = elapsed, timeLimitSec = NoTimer ? 0f : questionMaxTime, misconceptionTag = misTag
            });

            // reveal card text is prepared now, shown only after the attack
            revealCorrect = correct;
            revealChosen = chosenIndex >= 0 ? currentQuestion.options[chosenIndex] : "(waktu habis)";
            revealAnswer = currentQuestion.options[currentQuestion.correctIndex];
            revealExplain = Short(currentQuestion.explanation);

            _lockedChoice = chosenIndex;
            _lockedAt = Time.time;
            battlePhase = BattlePhase.Revealing;
            GrimoireAudioManager.Instance?.PlaySfx("select");
            bool spell = isChallengeForSpell && pendingSpell != null;
            // short beat with the choice highlighted, then the pop-up clears and the action plays
            Schedule(0.4f, () => { if (spell) PlaySpell(correct, grade); else PlayDefend(correct, grade); });
        }

        // ---------------------------------------------------------------- spell
        private void PlaySpell(bool correct, string grade)
        {
            var spell = pendingSpell;
            if (correct)
            {
                _heroAnim.Play("attack");
                Schedule(0.27f, () =>
                {
                    GrimoireAudioManager.Instance?.PlaySfx("magic");
                    float speed = grade == "PERFECT GUARD" ? 1.2f : 1f;
                    float defFactor = spell.id == "order_restored" ? 0f : (spell.id == "binary_pierce" ? 0.5f : 1f);
                    bool weak = monstersAnalyzed.Contains(activeMonster.id) && activeMonster.tpPool.Contains(spell.tpCode);
                    float raw = TotalAtk * spell.multiplier * speed * (1f + TotalInt / 40f) - MonsterDefense * 0.4f * defFactor;
                    if (weak) raw *= 1.5f;
                    int dmg = DealToMonster(Mathf.Max(8, Mathf.RoundToInt(raw)), false, out string note);
                    focus = Mathf.Min(100, focus + (grade == "PERFECT GUARD" ? 25 : 15));
                    HitMonster(dmg, "arcane", new Color(0.82f, 0.66f, 1f));
                    revealEffect = $"{spell.name}: {dmg} damage{(weak ? ", tepat di kelemahannya!" : "")}{note}";
                    switch (spell.id)
                    {
                        case "bubble_shield": hp = Mathf.Min(maxHp, hp + 25); _pStatus.Remove("poison"); _pStatus.Remove("torn_page"); SpawnVfx("heal", _heroRect, 0.9f); revealEffect += " +25 HP"; break;
                        case "selection_strike": _mStatus["marked"] = 2; revealEffect += ". Musuh ditandai"; break;
                        case "insertion_lance": _pStatus["ward"] = 9; revealEffect += ". Perisai pelindung aktif"; break;
                        case "order_restored":
                            hp = Mathf.Min(maxHp, hp + Mathf.RoundToInt(maxHp * 0.30f));
                            foreach (var k in new[] { "poison", "silence", "torn_page", "blind", "buried", "crushed", "split" }) _pStatus.Remove(k);
                            _mStatus["ordered"] = GrimoireStatus.Duration("ordered");
                            SpawnVfx("heal", _heroRect, 1.0f);
                            break;
                    }
                    CheckBossPhaseTransition();
                    if (monsterHp <= 0) _pendingVictory = true;
                });
                ShowReveal(1.3f, "BENAR!", new Color(0.16f, 0.48f, 0.22f));
            }
            else
            {
                _heroAnim.Play("hurt", "idle");              // the spell backfires as a puff
                Schedule(0.15f, () =>
                {
                    GrimoireAudioManager.Instance?.PlaySfx("error");
                    SpawnVfx("spark", _heroRect, 0.6f, new Color(0.7f, 0.6f, 1f, 0.8f));
                    SpawnText("gagal...", _heroRect, new Color(0.75f, 0.7f, 0.9f), 1, -10f);
                    if (activeMonster.id == "hydra_of_division") _mStatus["regen"] = 1;
                });
                revealEffect = $"{spell.name} gagal dirapal.";
                CheckMercyAfterWrong();
                ShowReveal(1.1f, "KURANG TEPAT", new Color(0.62f, 0.12f, 0.12f));
            }
        }

        // ---------------------------------------------------------------- defend
        private void PlayDefend(bool correct, string grade)
        {
            if (!correct && equippedAccessory == "acc_aegis_charm" && !aegisCharmUsedThisBattle)
            {
                aegisCharmUsedThisBattle = true;
                correct = true; grade = "GUARD";      // the charm catches the blow, the answer is still wrong
            }
            _monAnim.Release();                     // the frozen swing comes down
            Rect heroR = _heroRect;

            Schedule(0.25f, () =>
            {
                if (grade == "PERFECT GUARD")
                {
                    GrimoireAudioManager.Instance?.PlaySfx("parry");
                    _heroAnim.Play("parry");
                    SpawnVfx("hex_shield", heroR, 1.1f);
                    SpawnText("TERTANGKIS!", heroR, PixelUI.Cyan, 1, -16f);
                    Flash(new Color(0.5f, 0.8f, 1f, 0.25f), 0.15f);
                    int counter = DealToMonster(Mathf.Max(10, Mathf.RoundToInt(TotalAtk * 1.5f - MonsterDefense * 0.3f)), true, out _);
                    focus = Mathf.Min(100, focus + 25);
                    logicFragments++;
                    if (panicStacks > 0) panicStacks--;
                    Schedule(0.3f, () => HitMonster(counter, "slash", PixelUI.Gold));
                    revealEffect = $"Ditangkis dan dibalas {counter} damage. +1 Logic Fragment.";
                }
                else if (grade == "GUARD")
                {
                    GrimoireAudioManager.Instance?.PlaySfx("guard");
                    _heroAnim.Play("defend");
                    SpawnVfx("spark", new Rect(heroR.xMax - heroR.width * 0.35f, heroR.y + heroR.height * 0.3f, heroR.width * 0.4f, heroR.height * 0.4f), 1.2f, new Color(0.75f, 0.95f, 1f));
                    SpawnText("DITAHAN", heroR, PixelUI.Cyan, 1, -10f);
                    focus = Mathf.Min(100, focus + 10);
                    revealEffect = revealCorrect ? "Perisai menahan serangan. Jawab lebih cepat untuk membalas!" : "Aegis Charm menahan serangan untukmu.";
                }
                else
                {
                    GrimoireAudioManager.Instance?.PlaySfx("hit");
                    int atk = monsterAtk + (bossPhase - 1) * 4;
                    float crit = HasM("false_pivot") ? 1.35f : 1f;
                    int dmg = Mathf.Clamp(Mathf.RoundToInt((3 * atk - TotalDef) * crit), 1, Mathf.RoundToInt(maxHp * (difficulty == Difficulty.Easy ? 0.25f : 0.5f)));
                    if (_volleyTotal > 1) dmg = Mathf.Max(1, dmg / _volleyTotal);
                    if (HasP("ward")) { dmg = Mathf.RoundToInt(dmg * 0.7f); _pStatus.Remove("ward"); }
                    hp = Mathf.Max(0, hp - dmg);
                    panicStacks = Mathf.Min(3, panicStacks + 1);
                    string st = ApplyBreakStatus();
                    _heroAnim.Play("hurt", "idle");
                    SpawnVfx("shatter", heroR, 1.0f);
                    SpawnText($"-{dmg}", heroR, PixelUI.Red, 2);
                    Shake(6f, 0.35f);
                    Flash(new Color(0.85f, 0.05f, 0.08f, 0.35f), 0.35f);
                    revealEffect = $"Seranganmu tertembus: -{dmg} HP.{st}";
                    if (hp <= 0) _pendingMercyTp = currentQuestion.tp;
                }
                CheckBossPhaseTransition();
                if (monsterHp <= 0) _pendingVictory = true;
            });
            if (!revealCorrect) CheckMercyAfterWrong();
            bool perfect = grade == "PERFECT GUARD";
            ShowReveal(perfect ? 1.45f : 1.15f, revealCorrect ? (perfect ? "BENAR! SEMPURNA" : "BENAR!") : "KURANG TEPAT",
                       revealCorrect ? new Color(0.16f, 0.48f, 0.22f) : new Color(0.62f, 0.12f, 0.12f));
        }

        private void CheckMercyAfterWrong()
        {
            if (currentQuestion != null && tpFailStreaks.TryGetValue(currentQuestion.tp, out int s) && s >= 3) _pendingMercyTp = currentQuestion.tp;
        }

        private void ShowReveal(float delay, string title, Color col)
        {
            revealTitle = title;
            revealColor = col;
            Schedule(delay, () =>
            {
                _popupStart = Time.time;
                battlePhase = BattlePhase.TurnResolution;
                if (!revealCorrect) TipWrong();
            });
        }

        private string ApplyBreakStatus()
        {
            if (activeMonster.id == "hydra_of_division") { _mStatus["regen"] = 1; return " Hydra akan tumbuh kembali."; }
            string id = GrimoireStatus.OnBreak(activeMonster.id, bossPhase);
            if (id == null) return "";
            if ((id == "silence" || id == "poison") && equippedAccessory == "acc_pendant") return "";
            if (id == "torn_page" && equippedAccessory == "acc_cloak") return "";
            var info = GrimoireStatus.Get(id);
            if (id == "death_seal")
            {
                int seals = PVal("death_seal") + 1;
                if (seals >= 3)
                {
                    hp = Mathf.Min(hp, 1);
                    _pStatus["death_seal"] = 0;
                    return " Tiga segel menyala: HP tersisa 1!";
                }
                _pStatus["death_seal"] = seals;
                return $" Segel Kematian {seals}/3.";
            }
            _pStatus[id] = GrimoireStatus.Duration(id);
            Schedule(0.4f, () => SpawnVfx("poison", _heroRect, 0.8f));
            return $" Terkena {info?.name}.";
        }

        /// <summary>"Lanjut" on the reveal card.</summary>
        public void ContinueFromResolution()
        {
            if (battlePhase != BattlePhase.TurnResolution) return;
            if (_pendingVictory) { _pendingVictory = false; HandleBattleVictory(); return; }
            if (_pendingMercyTp != null)
            {
                string tp = _pendingMercyTp; _pendingMercyTp = null;
                if (hp <= 0) { _heroAnim.Play("death", "idle"); hp = Mathf.RoundToInt(maxHp * 0.5f); mp = Mathf.RoundToInt(maxMp * 0.5f); }
                _volleyLeft = 0;
                TriggerMercyGuidance(tp);
                return;
            }
            if (isChallengeForSpell) { battlePhase = BattlePhase.Animating; Schedule(0.25f, StartEnemyTurn); return; }
            if (_volleyLeft > 0) { BeginAlgorithmicChallenge(false, null); return; }
            EndEnemyTurn();
        }

        private void EndEnemyTurn()
        {
            if (HasP("poison"))
            {
                int psn = Mathf.Max(2, Mathf.RoundToInt(maxHp * 0.05f));
                hp = Mathf.Max(1, hp - psn);
                SpawnVfx("poison", _heroRect, 0.8f);
                SpawnText($"-{psn}", _heroRect, new Color(0.6f, 0.9f, 0.4f), 1);
            }
            foreach (var k in _pStatus.Keys.ToList())
            {
                var info = GrimoireStatus.Get(k);
                if ((info != null && info.stacks) || k == "ward") continue;
                if (--_pStatus[k] <= 0) _pStatus.Remove(k);
            }
            foreach (var k in _mStatus.Keys.ToList())
            {
                if (_mStatus[k] >= 90 || k == "marked") continue;
                if (--_mStatus[k] <= 0) _mStatus.Remove(k);
            }
            _heroAnim.Play("idle");
            battlePhase = BattlePhase.PlayerCommand;
        }

        private void CheckBossPhaseTransition()
        {
            if (activeMonster == null || monsterHp <= 0) return;
            float ratio = (float)monsterHp / Mathf.Max(1, monsterMaxHp);
            int p = bossPhase;
            if (activeMonster.id == "entropy_chaos_king") p = ratio <= 0.33f ? 3 : (ratio <= 0.66f ? Mathf.Max(p, 2) : p);
            else if (activeMonster.role != "Regular" && ratio <= 0.5f) p = Mathf.Max(p, 2);
            if (p == bossPhase) return;
            bossPhase = p;
            _mStatus["enraged"] = 90 + bossPhase;
            if (activeMonster.id == "the_blind_archivist") _mStatus["fog"] = 99;
            Schedule(0.5f, () =>
            {
                if (AnimLibrary.Durations(activeMonster.id + "_enrage") != null) _monAnim.Play("enrage", MonIdle);
                SpawnText($"FASE {bossPhase}!", _monRect, new Color(1f, 0.45f, 0.25f), 2, -24f);
                Shake(5f, 0.5f);
                GrimoireAudioManager.Instance?.PlaySfx("magic");
                Ribbon($"{activeMonster.name} mengamuk!");
            });
        }

        private void TriggerMercyGuidance(string tp)
        {
            tpFailStreaks[tp] = 0;
            if (ddaEnabled) ddaTier = "Assisted";
            hp = Mathf.Min(maxHp, hp + Mathf.RoundToInt(maxHp * 0.25f));
            mp = Mathf.Min(maxMp, mp + Mathf.RoundToInt(maxMp * 0.30f));
            mercyCodexEntry = GrimoireDatabase.Codex.FirstOrDefault(c => c.tp == tp) ?? GrimoireDatabase.Codex[0];
            codexOpened.Add(mercyCodexEntry.tp);
            GrimoireAudioManager.Instance?.PlaySfx("heal");
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
            Schedule(0.1f, () => _monAnim.Play("death", "death"));
            Schedule(0.3f, () => _heroAnim.Play("victory", "idle"));
            Schedule(1.4f, FinishBattleVictory);
        }

        private void FinishBattleVictory()
        {
            panicStacks = 0;
            _pStatus.Remove("death_seal");
            winFlawless = battleFlawless;
            winExp = activeMonster.exp + (battleFlawless ? Mathf.RoundToInt(activeMonster.exp * 0.2f) : 0);
            winGold = activeMonster.gold;
            winFrag = battleFlawless ? 1 : 0;
            winKey = _battleNode != null && _battleNode.type == NodeType.Elite;
            logicFragments += winFrag;
            winLevelUp = GainExpAndGold(winExp, winGold);
            if (_battleNode != null) _battleNode.cleared = true;
            GrimoireAudioManager.Instance?.PlaySfx("victory");
            _popupStart = Time.time;
            battlePhase = BattlePhase.BattleWon;
        }

        public void ContinueAfterVictory()
        {
            if (battlePhase != BattlePhase.BattleWon) return;
            bool boss = _battleNode != null && _battleNode.type == NodeType.Boss;
            if (activeMonster.id == "entropy_chaos_king" && boss)
            {
                viewMode = GameViewMode.Victory;
                GrimoireAudioManager.Instance?.PlayMusic("music_title");
                return;
            }
            if (boss)
            {
                battlePhase = BattlePhase.Animating;
                ActClear(() => StartActExpedition(currentAct + 1));
                return;
            }
            viewMode = GameViewMode.Board;
            GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey);
            boardMessage = $"{activeMonster.name} dikalahkan!";
            if (winKey) GainKey($"{activeMonster.name} menjatuhkan Kunci Indeks!");
            if (_battleNode == null) viewMode = runStarted ? GameViewMode.Board : GameViewMode.Title;   // sparring from the Bestiary
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
            _monAnim.Play("hurt", MonIdle);
            SpawnVfx(vfx, _monRect, 0.9f);
            SpawnVfx("spark", _monRect, 0.5f);
            SpawnText(dmg.ToString(), _monRect, numberCol, 2);
            Shake(2f, 0.12f);
        }
    }
}
