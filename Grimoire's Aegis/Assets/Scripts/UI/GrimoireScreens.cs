using System.Collections.Generic;
using System.Linq;
using GrimoireAegis.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using static GrimoireAegis.UI.PixelUI;

namespace GrimoireAegis.UI
{
    /// <summary>
    /// All screens on the 640x360 pixel canvas. Design rule: one focal point per moment.
    /// Board: the map and your next move. Battle: the monster, then the question, then what happened.
    /// </summary>
    public partial class GrimoireGameController
    {
        // ---- UI state ----
        private int _titleMode;      // 0 main, 1 difficulty
        private int _titleSel, _diffSel = 1;
        private int _cmdSel;
        private int _listSel;
        private float _modalScroll;
        private int _shopTab, _codexTab, _guideChapter, _bestiaryAnim;
        private float _monHpShown = 1f, _heroHpShown = 1f;
        private float _tokenHopStart = -10f;
        private float _lockedAt = -10f;
        private Vector2 _camPos;
        private readonly List<FloatingText> _boardPops = new List<FloatingText>();

        private static readonly Color[] ActAccent =
        {
            new Color32(0xec, 0xe0, 0xc8, 0xff),
            new Color32(0x9f, 0xc8, 0xff, 0xff),   // Act I pale lantern blue (GDD §17)
            new Color32(0xe8, 0xd4, 0x9a, 0xff),   // Act II bone / candle yellow
            new Color32(0xc8, 0x9a, 0xff, 0xff),   // Act III broken-magic purple
        };
        private Color Accent => ActAccent[Mathf.Clamp(currentAct, 1, 3)];
        private static string ActRoman(int a) => a == 1 ? "I" : (a == 2 ? "II" : "III");

        // =====================================================================
        private void OnGUI()
        {
            Begin();
            InputBlocked = activeModal != ModalOverlay.None || DialogOpen || (viewMode == GameViewMode.Board && PuzzleOpen);
            switch (viewMode)
            {
                case GameViewMode.Title: DrawTitle(); break;
                case GameViewMode.ActIntro: DrawActIntro(); break;
                case GameViewMode.Board: DrawBoard(); break;
                case GameViewMode.Battle: DrawBattle(); break;
                case GameViewMode.Victory: DrawEnding(); break;
            }
            if (activeModal != ModalOverlay.None)
            {
                InputBlocked = DialogOpen;
                DrawModal();
            }
            DrawScreenOverlays();
            InputBlocked = false;
            DrawDialog();
            End();
        }

        // =====================================================================
        // KEYBOARD
        // =====================================================================
        private static bool Pressed(Key k) => Keyboard.current != null && Keyboard.current[k].wasPressedThisFrame;
        private static bool Confirm => Pressed(Key.Enter) || Pressed(Key.Space) || Pressed(Key.NumpadEnter);
        private static bool Up => Pressed(Key.UpArrow) || Pressed(Key.W);
        private static bool Down => Pressed(Key.DownArrow) || Pressed(Key.S);
        private static bool Left => Pressed(Key.LeftArrow) || Pressed(Key.A);
        private static bool Right => Pressed(Key.RightArrow) || Pressed(Key.D);

        private void HandleKeyboard()
        {
            if (Keyboard.current == null) return;
            if (DialogOpen)
            {
                if (Confirm) AdvanceDialog();
                else if (Pressed(Key.Escape)) SkipDialog();
                return;
            }
            bool answering = viewMode == GameViewMode.Battle && battlePhase == BattlePhase.AlgorithmicChallenge && activeModal == ModalOverlay.None;
            if (answering)
            {
                for (int i = 0; i < 4; i++) if (Pressed(Key.Digit1 + i) || Pressed(Key.Numpad1 + i)) { SubmitSlot(i); return; }
                if (Pressed(Key.H)) UseScholarHint();
                else if (Pressed(Key.Escape)) ToggleModal(ModalOverlay.Pause);
                return;
            }
            if (PuzzleOpen && activeModal == ModalOverlay.None)
            {
                if (_puzzleChosen < 0) { for (int i = 0; i < 4; i++) if (Pressed(Key.Digit1 + i)) AnswerPuzzle(i); }
                else if (Confirm) ClosePuzzle();
                return;
            }

            if (Pressed(Key.Escape))
            {
                if (activeModal != ModalOverlay.None) { activeModal = ModalOverlay.None; return; }
                if (viewMode == GameViewMode.Title && _titleMode != 0) { _titleMode = 0; return; }
                if (viewMode == GameViewMode.Battle && (battlePhase == BattlePhase.SpellSelect || battlePhase == BattlePhase.ItemSelect)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 0; return; }
                if (viewMode == GameViewMode.Battle && battlePhase == BattlePhase.Analyze) { CloseAnalyze(); return; }
                if (viewMode == GameViewMode.Board || viewMode == GameViewMode.Battle) { ToggleModal(ModalOverlay.Pause); return; }
            }
            if (Pressed(Key.G)) { ToggleModal(ModalOverlay.Guide); return; }
            if (Pressed(Key.I) && viewMode == GameViewMode.Board) { _shopTab = 1; ToggleModal(ModalOverlay.ShopInventory); return; }

            if (activeModal != ModalOverlay.None) { HandleModalKeys(); return; }

            switch (viewMode)
            {
                case GameViewMode.Title: HandleTitleKeys(); break;
                case GameViewMode.ActIntro: if (Confirm && Time.time - _actIntroStart > 0.4f) EnterBoardFromIntro(); break;
                case GameViewMode.Board:
                    if (AwaitingChoice)
                    {
                        if (Up) ChooseDirection(0, -1);
                        else if (Down) ChooseDirection(0, 1);
                        else if (Left) ChooseDirection(-1, 0);
                        else if (Right) ChooseDirection(1, 0);
                        else if (Confirm && _choices.Count > 0) ChooseDirection(_choices[0]);
                    }
                    else if (Pressed(Key.Space) || Confirm) RollExplorationDice();
                    break;
                case GameViewMode.Battle: HandleBattleKeys(); break;
            }
        }

        private void HandleTitleKeys()
        {
            if (_titleMode == 0)
            {
                var items = TitleItems();
                if (Up) _titleSel = (_titleSel + items.Count - 1) % items.Count;
                if (Down) _titleSel = (_titleSel + 1) % items.Count;
                if (Confirm) TitleAction(items[_titleSel]);
            }
            else
            {
                if (Left) _diffSel = (_diffSel + 2) % 3;
                if (Right) _diffSel = (_diffSel + 1) % 3;
                if (Confirm) StartNewRun((Difficulty)_diffSel);
            }
        }

        private void HandleBattleKeys()
        {
            switch (battlePhase)
            {
                case BattlePhase.PlayerCommand:
                    if (Left || Up) _cmdSel = (_cmdSel + 3) % 4;
                    if (Right || Down) _cmdSel = (_cmdSel + 1) % 4;
                    for (int i = 0; i < 4; i++) if (Pressed(Key.Digit1 + i)) { _cmdSel = i; RunCommand(i); return; }
                    if (Confirm) RunCommand(_cmdSel);
                    break;
                case BattlePhase.SpellSelect:
                {
                    var sp = SpellsAvailable();
                    if (sp.Count == 0) break;
                    if (Up || Left) _cmdSel = (_cmdSel + sp.Count - 1) % sp.Count;
                    if (Down || Right) _cmdSel = (_cmdSel + 1) % sp.Count;
                    _cmdSel = Mathf.Clamp(_cmdSel, 0, sp.Count - 1);
                    if (Confirm) BeginSpellCast(sp[_cmdSel]);
                    if (Pressed(Key.Backspace)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 1; }
                    break;
                }
                case BattlePhase.ItemSelect:
                {
                    var items = BattleItems();
                    if (items.Count == 0) { if (Confirm || Pressed(Key.Backspace)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 2; } break; }
                    if (Up || Left) _cmdSel = (_cmdSel + items.Count - 1) % items.Count;
                    if (Down || Right) _cmdSel = (_cmdSel + 1) % items.Count;
                    _cmdSel = Mathf.Clamp(_cmdSel, 0, items.Count - 1);
                    if (Confirm) UseConsumableItem(items[_cmdSel]);
                    if (Pressed(Key.Backspace)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 2; }
                    break;
                }
                case BattlePhase.TurnResolution: if (Confirm) ContinueFromResolution(); break;
                case BattlePhase.MercyIntervention: if (Confirm) CloseMercy(); break;
                case BattlePhase.Analyze: if (Confirm) CloseAnalyze(); break;
                case BattlePhase.BattleWon: if (Confirm) ContinueAfterVictory(); break;
            }
        }

        private void RunCommand(int i)
        {
            switch (i)
            {
                case 0: ExecuteBasicAttack(); break;
                case 1: battlePhase = BattlePhase.SpellSelect; _cmdSel = 0; TipSpells(); break;
                case 2:
                    if (ItemsLocked) { GrimoireAudioManager.Instance?.PlaySfx("error"); Ribbon("Tasmu terkubur!"); break; }
                    battlePhase = BattlePhase.ItemSelect; _cmdSel = 0; break;
                case 3: TryFlee(); break;
            }
        }

        private List<string> BattleItems() =>
            new[] { "pot_hp", "pot_mp", "pot_elixir", "scroll_clarity" }
                .Where(k => GrimoireDatabase.Items.ContainsKey(k) && consumables.TryGetValue(k, out int q) && q > 0).ToList();

        // =====================================================================
        // TITLE
        // =====================================================================
        private List<string> TitleItems()
        {
            var l = new List<string>();
            if (runStarted) l.Add("Lanjutkan");
            l.Add(runStarted ? "Petualangan Baru" : "Mulai Petualangan");
            l.AddRange(new[] { "Buku Panduan", "Pengaturan", "Keluar" });
            return l;
        }

        private void TitleAction(string item)
        {
            switch (item)
            {
                case "Lanjutkan": viewMode = GameViewMode.Board; GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey); break;
                case "Mulai Petualangan":
                case "Petualangan Baru": _titleMode = 1; _diffSel = (int)difficulty; break;
                case "Buku Panduan": ToggleModal(ModalOverlay.Guide); break;
                case "Pengaturan": ToggleModal(ModalOverlay.Settings); break;
                case "Keluar": Application.Quit(); break;
            }
        }

        private void DrawTitle()
        {
            Image(new Rect(0, 0, W, H), T("bg/bg_title"));
            Shade(new Rect(0, 0, W, H), 0.45f);
            for (int i = 0; i < 22; i++)
            {
                float x = (i * 97 + Time.time * (6 + i % 5)) % W;
                float y = (i * 53 + 360 - (Time.time * (3 + i % 4) + i * 30) % 360);
                Fill(new Rect(Mathf.Round(x), Mathf.Round(y), 1, 1), new Color(1f, 0.86f, 0.42f, 0.25f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time + i))));
            }
            TextCenter(new Rect(0, 26, W, 28), "GRIMOIRE'S AEGIS", Gold, 3);
            TextCenter(new Rect(0, 58, W, 14), "The Scholar Knight", Cyan, 1);

            float glow = 0.18f + 0.06f * Mathf.Sin(Time.time * 2f);
            for (int r = 0; r < 6; r++) Fill(new Rect(108 - r * 8, 296 - r, 120 + r * 16, 2 + r * 2 / 3), new Color(0.4f, 0.7f, 1f, glow * (1 - r / 6f)));
            Strip(250, 92, "anim/entropy_chaos_king_idle", "entropy_chaos_king_idle", Time.time, 2, new Color(1, 1, 1, 0.3f));
            Strip(96, 154, "anim/hero_idle", "hero_idle", Time.time, 3);

            if (_titleMode == 0) DrawTitleMenu();
            else DrawDifficultyPicker();
            TextRight(632, 348, "Kelompok 3", Dim);
        }

        private void DrawTitleMenu()
        {
            var items = TitleItems();
            Rect p = new Rect(436, 150, 172, 14 + items.Count * 22);
            Panel(p);
            for (int i = 0; i < items.Count; i++)
            {
                Rect r = new Rect(p.x + 8, p.y + 7 + i * 22, p.width - 16, 20);
                if (Hover(r)) _titleSel = i;
                if (Button(r, items[i], null, true, _titleSel == i, i == 0 ? "gold" : "normal", align: 1)) TitleAction(items[i]);
            }
        }

        private void DrawDifficultyPicker()
        {
            Shade(new Rect(0, 0, W, H), 0.6f);
            TextCenter(new Rect(0, 96, W, 16), "Pilih tingkat kesulitan", Gold, 2);
            string[] icons = { "icons/heart_full", "icons/timer", "icons/skull" };
            string[] lines = { "Tanpa batas waktu.\nSantai, pikirkan baik-baik.", "Ada waktu untuk menjawab.\nSeperti di kelas.", "Waktu lebih singkat,\nmonster lebih kuat." };
            for (int i = 0; i < 3; i++)
            {
                Rect c = new Rect(64 + i * 176, 128, 160, 132);
                if (Hover(c)) _diffSel = i;
                bool sel = _diffSel == i;
                Panel(c, sel ? "gold" : "panel");
                float bob = sel ? Mathf.Round(Mathf.Sin(Time.time * 4f) * 1.5f) : 0;
                Icon(icons[i], c.center.x - 10, c.y + 14 + bob, 2);
                TextCenter(new Rect(c.x, c.y + 40, c.width, 16), GrimoireStatus.DifficultyName((Difficulty)i), sel ? Gold : Ink, 2);
                TextCenter(new Rect(c.x, c.y + 64, c.width, 24), lines[i], Dim);
                if (Button(new Rect(c.x + 30, c.yMax - 26, c.width - 60, 18), "Pilih", null, true, sel, sel ? "gold" : "normal", align: 1))
                    StartNewRun((Difficulty)i);
            }
            TextCenter(new Rect(0, 272, W, 12), "Bisa diganti nanti di Pengaturan.", Dim);
            if (Button(new Rect(280, 292, 80, 18), "Kembali", null, true, false, align: 1)) _titleMode = 0;
        }

        // =====================================================================
        // ACT INTRO CARD
        // =====================================================================
        private void DrawActIntro()
        {
            Image(new Rect(0, 0, W, H), T($"bg/bg_s{Mathf.Clamp(currentAct, 1, 3)}"));
            Shade(new Rect(0, 0, W, H), 0.58f);
            if (Time.time < _actIntroStart)
            {
                if (!DialogOpen) { _actIntroStart = Time.time; }
                else
                {
                    TextCenter(new Rect(0, 54, W, 14), $"Act {ActRoman(currentAct)} - {ActName(currentAct)}", Accent, 2);
                    Strip(204, 118, "anim/hero_idle", "hero_idle", Time.time, 2);
                    Strip(316, 108, "npc/lyra", "npc_lyra_idle", Time.time, 2);
                    return;
                }
            }
            float a = Mathf.Clamp01((Time.time - _actIntroStart) / 0.6f);
            Color dim = new Color(Dim.r, Dim.g, Dim.b, a), ink = new Color(Ink.r, Ink.g, Ink.b, a), acc = Accent; acc.a = a;
            Panel(new Rect(88, 68, 464, 224), "panel", a);
            TextCenter(new Rect(0, 86, W, 12), $"Act {ActRoman(currentAct)}", dim);
            TextCenter(new Rect(0, 102, W, 24), ActName(currentAct), acc, 2);
            TextCenter(new Rect(0, 130, W, 12), ActTopic(currentAct), new Color(Gold.r, Gold.g, Gold.b, a));
            var lines = PixelUI.Font != null ? PixelUI.Font.Wrap(ActIntroText(currentAct), 420) : new List<string>();
            for (int i = 0; i < lines.Count; i++) TextCenter(new Rect(0, 156 + i * 12, W, 12), lines[i], ink);
            if (Button(new Rect(245, 240, 150, 22), "Mulai Eksplorasi", null, true, true, "gold", "Spasi", align: 1))
            {
                EnterBoardFromIntro();
                return;
            }
            if (Mathf.Sin(Time.time * 3f) > -0.3f) TextCenter(new Rect(0, 270, W, 12), "- klik / spasi -", dim);
            if (!InputBlocked && Event.current.type == EventType.MouseUp && Time.time - _actIntroStart > 0.35f) { Event.current.Use(); EnterBoardFromIntro(); }
        }

        // =====================================================================
        // ADVENTURE BOARD
        // =====================================================================
        private const int Cell = 32;
        private static readonly Rect BoardView = new Rect(0, 26, 640, 282);

        private Vector2 NodeWorld(MapNode n) => new Vector2(n.gx * Cell + Cell * 0.5f, n.gy * Cell + Cell * 0.5f);

        private void PopBoard(string text, Color col)
        {
            var n = map?.Get(currentNode);
            if (n == null) return;
            _boardPops.Add(new FloatingText { text = text, color = col, start = Time.time, pos = NodeWorld(n) + new Vector2(0, -40), life = 1.4f });
        }

        private static int Hash(int x, int y, int s) { unchecked { int h = x * 73856093 ^ y * 19349663 ^ s * 83492791; h ^= h >> 13; return (h * 1274126177) & 0x7fffffff; } }

        private void DrawBoard()
        {
            if (map == null) return;
            Vector2 sh = ShakeOffset();
            var cur = map.Get(currentNode);

            // camera follows the hero, clamped to the map
            float mw = map.width * Cell, mh = map.height * Cell;
            Vector2 target = NodeWorld(cur);
            _camPos = Vector2.Lerp(_camPos, target, Mathf.Clamp01(Time.deltaTime * 6f));
            float cx = mw <= BoardView.width ? mw * 0.5f : Mathf.Clamp(_camPos.x, BoardView.width * 0.5f, mw - BoardView.width * 0.5f);
            float cy = mh <= BoardView.height ? mh * 0.5f : Mathf.Clamp(_camPos.y, BoardView.height * 0.5f, mh - BoardView.height * 0.5f);
            float ox = Mathf.Round(BoardView.x + BoardView.width * 0.5f - cx + sh.x), oy = Mathf.Round(BoardView.y + BoardView.height * 0.5f - cy + sh.y);

            Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.02f, 0.04f));
            BeginClip(BoardView);
            float bx = ox - BoardView.x, by = oy - BoardView.y;
            // ground
            var ground = T($"tiles/ground_s{currentAct}");
            for (float gy = (by % 64) - 64; gy < BoardView.height; gy += 64)
            for (float gx = (bx % 64) - 64; gx < BoardView.width; gx += 64)
                Image(new Rect(gx, gy, 64, 64), ground);

            int x0 = Mathf.Max(0, Mathf.FloorToInt(-bx / Cell) - 1), x1 = Mathf.Min(map.width, Mathf.CeilToInt((BoardView.width - bx) / Cell) + 1);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(-by / Cell) - 1), y1 = Mathf.Min(map.height, Mathf.CeilToInt((BoardView.height - by) / Cell) + 2);
            var revealed = map.nodes.Where(n => n.revealed).ToList();
            bool Seen(int x, int y) { foreach (var n in revealed) if (Mathf.Abs(n.gx - x) + Mathf.Abs(n.gy - y) <= 2) return true; return false; }

            // roads
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                if (!map.IsPath(x, y)) continue;
                Rect r = new Rect(bx + x * Cell, by + y * Cell, Cell, Cell);
                Image(r, T($"world/road_s{currentAct}_{Hash(x, y, 1) % 3}"));
                if (!map.IsPath(x, y + 1)) Fill(new Rect(r.x, r.yMax - 2, Cell, 2), new Color(0, 0, 0, 0.45f));
                if (!map.IsPath(x, y - 1)) Fill(new Rect(r.x, r.y, Cell, 1), new Color(1, 1, 1, 0.08f));
                if (!map.IsPath(x - 1, y)) Fill(new Rect(r.x, r.y, 1, Cell), new Color(0, 0, 0, 0.35f));
                if (!map.IsPath(x + 1, y)) Fill(new Rect(r.xMax - 1, r.y, 1, Cell), new Color(0, 0, 0, 0.35f));
            }
            // scenery props (row by row so taller props overlap correctly)
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                if (map.IsPath(x, y) || Hash(x, y, currentAct) % 100 >= 30) continue;
                var prop = T($"world/prop_s{currentAct}_{Hash(x, y, 7) % 5}");
                if (prop == null) continue;
                if (prop.height > Cell && (map.IsPath(x, y - 1))) continue;       // tall props never cover the road
                Image(new Rect(bx + x * Cell, by + y * Cell + Cell - prop.height, prop.width, prop.height), prop);
            }
            // fog of war
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                if (!Seen(x, y)) Fill(new Rect(bx + x * Cell, by + y * Cell, Cell, Cell), new Color(0.01f, 0.01f, 0.03f, 0.88f));

            // nodes
            MapNode hovered = null;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            foreach (var n in map.nodes)
            {
                if (!n.revealed) continue;
                Rect r = new Rect(bx + n.gx * Cell, by + n.gy * Cell, Cell, Cell);
                if (r.xMax < -64 || r.x > BoardView.width + 64) continue;
                DrawNode(n, r, pulse);
                if (Hover(r) && n.type != NodeType.Path) hovered = n;
            }
            // fork choices
            foreach (int id in _choices.ToArray())
            {
                var n = map.Get(id);
                if (n == null) continue;
                Rect r = new Rect(bx + n.gx * Cell, by + n.gy * Cell, Cell, Cell);
                Image(r, T("world/ring_gold"), new Color(1, 1, 1, 0.5f + 0.5f * pulse));
                Vector2 d = new Vector2(n.gx - cur.gx, n.gy - cur.gy);
                string arrow = d.x > 0 ? ">" : d.x < 0 ? "<" : d.y > 0 ? "v" : "^";
                TextCenter(new Rect(r.x, r.y - 2 - pulse * 2, Cell, Cell), arrow, Gold, 2);
                if (!InputBlocked && Event.current.type == EventType.MouseUp && Event.current.button == 0 && r.Contains(PixelUI.Mouse))
                {
                    Event.current.Use();
                    GrimoireAudioManager.Instance?.PlaySfx("select");
                    ChooseDirection(id);
                    break;
                }
            }
            // hero token
            {
                Rect r = new Rect(bx + cur.gx * Cell, by + cur.gy * Cell, Cell, Cell);
                float hop = Mathf.Clamp01((Time.time - _tokenHopStart) / 0.2f);
                float lift = Mathf.Sin(hop * Mathf.PI) * 6f;
                string anim = (_walking || hop < 1f) ? "walk" : "idle";
                Strip(Mathf.Round(r.center.x - 24), Mathf.Round(r.yMax - 46 - lift), $"anim/hero_{anim}", "hero_" + anim, Time.time);
            }
            // pops
            for (int i = _boardPops.Count - 1; i >= 0; i--)
            {
                var p = _boardPops[i];
                float t = (Time.time - p.start) / p.life;
                if (t >= 1f) { _boardPops.RemoveAt(i); continue; }
                float a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                string s = p.text;
                Text(Mathf.Round(bx + p.pos.x - Measure(s) * 0.5f), Mathf.Round(by + p.pos.y - GuiSprite.EaseOutCubic(t) * 18f), s, new Color(p.color.r, p.color.g, p.color.b, a));
            }
            EndClip();

            if (hovered != null && !DialogOpen)
            {
                string extra = hovered.monsterId != null && GrimoireDatabase.Monsters.TryGetValue(hovered.monsterId, out var md) ? $" - {md.name}" : "";
                Tooltip($"{{#ffd86a}}{AdventureMaps.NodeName(hovered.type)}{extra}{{/}}\n{AdventureMaps.NodeDesc(hovered.type)}");
            }

            DrawBoardTopBar();
            DrawBoardBottom();
            if (PuzzleOpen)
            {
                InputBlocked = activeModal != ModalOverlay.None || DialogOpen;
                DrawPuzzle();
            }
        }

        private void DrawNode(MapNode n, Rect r, float pulse)
        {
            if (n.cleared && n.type != NodeType.Gate && n.type != NodeType.Merchant) return;
            switch (n.type)
            {
                case NodeType.Path:
                case NodeType.Start:
                    return;
                case NodeType.Enemy:
                case NodeType.Elite:
                {
                    if (n.type == NodeType.Elite) Image(r, T("world/ring_gold"));
                    else Image(r, T("world/ring_red"), new Color(1, 1, 1, 0.6f));
                    var tex = T($"anim/{n.monsterId}_idle");
                    float s = tex != null ? tex.height : 48;
                    Frame(new Rect(Mathf.Round(r.center.x - s * 0.5f), r.yMax - s + 4, s, s), tex, AnimLibrary.FrameAt(n.monsterId + "_idle", Time.time + n.id * 0.37f, true));
                    if (n.type == NodeType.Elite) PixelUI.Frame(new Rect(r.center.x - 8, r.yMax - s - 10, 16, 16), T("icons/key_spin"), Mathf.FloorToInt(Time.time * 6f) % 4);
                    return;
                }
                case NodeType.Boss:
                {
                    var tex = T($"anim/{n.monsterId}_idle");
                    Frame(new Rect(r.center.x - 32, r.yMax - 60, 64, 64), tex, AnimLibrary.FrameAt(n.monsterId + "_idle", Time.time, true));
                    return;
                }
                case NodeType.Gate:
                {
                    var tex = T(n.cleared ? "world/gate_open" : $"world/gate_{Mathf.Clamp(keysCollected, 0, 3)}");
                    Image(new Rect(r.center.x - 24, r.yMax - 46, 48, 48), tex);
                    return;
                }
                case NodeType.Puzzle:
                case NodeType.KeyChest:
                    Image(r, T("world/ring_gold"), new Color(1, 1, 1, 0.6f + 0.4f * pulse));
                    Icon(n.type == NodeType.Puzzle ? "tiles/icon_puzzle" : "tiles/icon_treasure", r.x + 8, r.y + 6 - Mathf.Round(pulse * 2));
                    return;
                default:
                {
                    string icon = n.type == NodeType.Treasure ? "tiles/icon_treasure" : n.type == NodeType.Rest ? "tiles/icon_rest" :
                                  n.type == NodeType.Merchant ? "tiles/icon_merchant" : n.type == NodeType.Shrine ? "tiles/icon_shrine" :
                                  n.type == NodeType.Event ? "tiles/icon_event" : "tiles/icon_trap";
                    Image(r, T("world/ring_blue"), new Color(1, 1, 1, 0.45f));
                    Icon(icon, r.x + 8, r.y + 7);
                    return;
                }
            }
        }

        private void DrawBoardTopBar()
        {
            Rect bar = new Rect(0, 0, W, 26);
            Panel(bar);
            Text(8, 8, $"Act {ActRoman(currentAct)}  {ActName(currentAct)}", Accent);
            float x = 214;
            Icon("icons/hp", x, 8); Bar(new Rect(x + 12, 9, 70, 8), (float)hp / maxHp, new Color(0.85f, 0.2f, 0.25f)); Text(x + 86, 9, $"{hp}/{maxHp}", Ink);
            x += 140;
            Icon("icons/gold", x, 8); Text(x + 12, 9, $"{gold}", Gold);
            x += 52;
            Text(x, 9, "Kunci", Dim);
            for (int k = 0; k < 3; k++)
                Icon("icons/key", x + 30 + k * 16, 5, 1, k < keysCollected ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.9f));
            if (Button(new Rect(W - 66, 5, 60, 16), "Menu", null, true, false, align: 1, hotkey: null)) ToggleModal(ModalOverlay.Pause);
        }

        private void DrawBoardBottom()
        {
            Rect p = new Rect(0, 308, W, 52);
            Panel(p);
            Rect die = new Rect(8, 312, 44, 44);
            NineSlice(die, T("ui2/slot_normal"), 4);
            int face = DiceRolling ? 1 + Mathf.FloorToInt(Time.time * 18f) % 6 : Mathf.Clamp(diceValue, 1, 6);
            float wob = DiceRolling ? Mathf.Sin(Time.time * 40f) * 2f : 0f;
            Image(new Rect(die.x + 10 + wob, die.y + 10, 24, 24), T($"dice/die_{face}"));

            if (AwaitingChoice)
            {
                var cur = map.Get(currentNode);
                float bx = 60;
                foreach (int id in _choices.ToArray())
                {
                    var n = map.Get(id);
                    if (n == null || cur == null) continue;
                    int dx = n.gx - cur.gx, dy = n.gy - cur.gy;
                    string dirLabel = dy < 0 ? "^ Atas" : dy > 0 ? "v Bawah" : dx < 0 ? "< Kiri" : "> Kanan";
                    if (Button(new Rect(bx, 315, 62, 22), dirLabel, null, true, true, "gold", align: 1))
                    {
                        ChooseDirection(id);
                        break;
                    }
                    bx += 66;
                }
                if (stepsRemaining > 0) Text(62, 341, $"Pilih arah ({stepsRemaining} langkah lagi)", Gold);
            }
            else if (Button(new Rect(60, 316, 126, 22), "Lempar Dadu", null, !BoardBusy, !BoardBusy, "gold", "Spasi"))
                RollExplorationDice();
            else if (stepsRemaining > 0)
                Text(64, 342, $"{stepsRemaining} langkah lagi", Dim);

            float msgX = AwaitingChoice ? Mathf.Max(200, 64 + _choices.Count * 66) : 200;
            TextWrap(new Rect(msgX, 316, W - msgX - 10, 36), boardMessage, Ink, 1, true, 3);
        }

        private void DrawPuzzle()
        {
            var q = puzzleQuestion;
            Shade(new Rect(0, 0, W, H), 0.55f);
            float promptH = Mathf.Min(WrapHeight(q.prompt, 356), 60);
            bool hasArr = q.arrayData != null && q.arrayData.Length > 0;
            float h = Mathf.Min(300, 30 + (hasArr ? 70 : 0) + promptH + 70);
            Rect p = new Rect(130, Mathf.Round((H - h) * 0.5f), 380, h);
            Panel(p, "parchment");
            float x = p.x + 12, w = p.width - 24, y = p.y + 10;
            Text(x, y, "Rak Terkunci", ParchInk, 1, false);
            TextRight(x + w, y, "tanpa batas waktu", ParchDim, 1, false);
            y += 16;
            if (hasArr) { DrawThemedArray(p.center.x, y + 48, q.arrayData, 34, ParchInk, ParchDim, q); y += 70; }
            y += TextWrap(new Rect(x, y, w, 60), q.prompt, ParchInk, 1, false, 5) + 6;
            float ow = (w - 6) * 0.5f, oh = 26;
            for (int i = 0; i < 4; i++)
            {
                Rect r = new Rect(x + (i % 2) * (ow + 6), y + (i / 2) * (oh + 3), ow, oh);
                bool done = _puzzleChosen >= 0;
                bool isAns = i == q.correctIndex, isPick = i == _puzzleChosen;
                string tex = done ? (isAns ? "ui2/btn_gold" : isPick ? "ui2/btn_danger" : "ui2/btn_disabled") : (Hover(r) ? "ui2/btn_hover" : "ui2/btn_normal");
                NineSlice(r, T(tex), 4);
                Text(r.x + 5, r.y + 2, $"{i + 1}", Dim);
                DrawOptionLabel(r, q.options[i], done && !isAns && !isPick ? new Color(0.5f, 0.5f, 0.56f) : Ink);
                if (!InputBlocked && !done && Event.current.type == EventType.MouseUp && r.Contains(PixelUI.Mouse)) { Event.current.Use(); AnswerPuzzle(i); }
            }
            if (_puzzleChosen >= 0)
            {
                bool ok = _puzzleChosen == q.correctIndex;
                Rect res = new Rect(p.x, p.yMax + 4, p.width, 46);
                Panel(res, ok ? "gold" : "red");
                Text(res.x + 10, res.y + 8, ok ? "Rak terbuka!" : "Belum tepat. Coba soal lain.", ok ? Gold : Red);
                TextWrap(new Rect(res.x + 10, res.y + 20, res.width - 130, 24), Short(q.explanation, 120), Ink, 1, true, 2);
                if (Button(new Rect(res.xMax - 112, res.y + 14, 102, 18), ok ? "Ambil kunci" : "Coba lagi", null, true, true, "gold", "Spasi")) ClosePuzzle();
            }
        }

        // =====================================================================
        // BATTLE
        // =====================================================================
        private Vector2 ShakeOffset()
        {
            if (Time.time >= _shakeUntil) return Vector2.zero;
            float k = Mathf.Clamp01((_shakeUntil - Time.time) / 0.35f);
            return new Vector2(Mathf.Round(UnityEngine.Random.Range(-1f, 1f) * _shakeMag * k), Mathf.Round(UnityEngine.Random.Range(-1f, 1f) * _shakeMag * k));
        }

        private void DrawBattle()
        {
            if (activeMonster == null) return;
            Vector2 sh = ShakeOffset();
            Image(new Rect(sh.x - 4, sh.y - 4, W + 8, H + 8), T($"bg/bg_s{Mathf.Clamp(activeMonster.act, 1, 3)}"));
            Shade(new Rect(0, 0, W, 244), 0.25f);
            Fill(new Rect(0, 226, W, 18), new Color(0.03f, 0.03f, 0.06f, 0.45f));

            int hf = _heroAnim.CurrentFrame();
            _heroRect = new Rect(40 + sh.x, 236 - 96 + sh.y, 96, 96);
            Frame(_heroRect, T($"anim/hero_{_heroAnim.anim}"), hf);

            int mf = _monAnim.CurrentFrame();
            var monTex = T($"anim/{activeMonster.id}_{_monAnim.anim}");
            float ms = (monTex != null ? monTex.height : 48) * 2;
            _monRect = new Rect(600 - ms + sh.x, 236 - ms + sh.y, ms, ms);
            Frame(_monRect, monTex, mf);

            DrawMonsterPlate();
            DrawStatusRow();
            if (Button(new Rect(W - 66, 6, 60, 16), "Menu", null, true, false, align: 1)) ToggleModal(ModalOverlay.Pause);
            string rib = RibbonText;
            if (rib.Length > 0 && battlePhase != BattlePhase.AlgorithmicChallenge && battlePhase != BattlePhase.TurnResolution)
            {
                float w = Measure(rib) + 20;
                Panel(new Rect(Mathf.Round((W - w) * 0.5f), 8, w, 20), "dark");
                TextCenter(new Rect(0, 8, W, 20), rib, Ink);
            }
            DrawBattleFx();
            DrawBattleHud();

            switch (battlePhase)
            {
                case BattlePhase.AlgorithmicChallenge: DrawQuestionPopup(); break;
                case BattlePhase.Revealing: if (Time.time - _lockedAt < 0.4f) DrawQuestionPopup(); break;
                case BattlePhase.TurnResolution: DrawRevealCard(); break;
                case BattlePhase.MercyIntervention: DrawMercyPopup(); break;
                case BattlePhase.Analyze: DrawAnalyzePopup(); break;
                case BattlePhase.BattleWon: DrawWonPopup(); break;
            }
        }

        private void DrawMonsterPlate()
        {
            if (monsterHp <= 0 && _monAnim.anim == "death") return;
            float cx = _monRect.center.x;
            float y = Mathf.Max(40, _monRect.y - 6);
            Rect nameR = new Rect(cx - 70, y - 22, 140, 22);
            bool clickable = battlePhase == BattlePhase.PlayerCommand;
            TextCenter(new Rect(cx - 90, y - 22, 180, 10), activeMonster.name, isBossEncounter ? new Color(1f, 0.55f, 0.45f) : Ink);
            Rect hb = new Rect(Mathf.Round(cx - 45), y - 10, 90, 6);
            float ratio = Mathf.Clamp01((float)monsterHp / Mathf.Max(1, monsterMaxHp));
            _monHpShown = Mathf.MoveTowards(_monHpShown, ratio, Time.deltaTime * 0.6f);
            Bar(hb, ratio, new Color(0.85f, 0.2f, 0.25f), _monHpShown);
            float sx = cx - (MonsterStatuses().Count() * 14) * 0.5f;
            foreach (var kv in MonsterStatuses())
            {
                Rect ir = new Rect(sx, hb.yMax + 2, 12, 12);
                Icon($"status/{kv.Key}", ir.x, ir.y);
                if (Hover(ir)) { var info = GrimoireStatus.Get(kv.Key); if (info != null) Tooltip($"{{#ffd86a}}{info.name}{{/}}\n{info.effect}"); }
                sx += 14;
            }
            if (clickable && Hover(nameR))
            {
                Tooltip("Klik untuk melihat info monster");
                if (!InputBlocked && Event.current.type == EventType.MouseUp) { Event.current.Use(); AnalyzeMonster(); }
            }
        }

        /// <summary>Only what affects you right now: icon + turns, details on hover.</summary>
        private void DrawStatusRow()
        {
            var list = PlayerStatuses().ToList();
            if (list.Count == 0) return;
            float x = 8;
            foreach (var kv in list)
            {
                var info = GrimoireStatus.Get(kv.Key);
                if (info == null) continue;
                string n = info.stacks ? $"{kv.Value}" : (kv.Value > 0 && kv.Value < 90 ? $"{kv.Value}" : "");
                float w = 14 + Measure(info.name) + (n.Length > 0 ? Measure(n) + 4 : 0) + 8;
                Rect r = new Rect(x, 6, w, 16);
                Panel(r, "dark");
                Icon($"status/{info.id}", r.x + 2, r.y + 2);
                Text(r.x + 16, r.y + 4, info.name, info.buff ? Green : Ink);
                if (n.Length > 0) TextRight(r.xMax - 4, r.y + 4, n, Dim);
                if (Hover(r)) Tooltip($"{{#ffd86a}}{info.name}{{/}}\n{info.effect}\n{{#8a8ea8}}{info.cure}{{/}}");
                x += w + 3;
            }
        }

        private void DrawBattleFx()
        {
            for (int i = _vfx.Count - 1; i >= 0; i--)
            {
                var v = _vfx[i];
                string key = "vfx_" + v.name;
                float el = Time.time - v.start;
                float len = AnimLibrary.Length(key);
                if (el >= (len > 0 ? len : 0.5f)) { _vfx.RemoveAt(i); continue; }
                float s = Mathf.Max(64, Mathf.Round(v.size / 64f) * 64f);
                Frame(new Rect(Mathf.Round(v.center.x - s * 0.5f), Mathf.Round(v.center.y - s * 0.5f), s, s), T("vfx/" + v.name), AnimLibrary.FrameAt(key, el, false), v.tint);
            }
            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                var ft = _floaters[i];
                float t = (Time.time - ft.start) / ft.life;
                if (t >= 1f) { _floaters.RemoveAt(i); continue; }
                float rise = GuiSprite.EaseOutCubic(t) * 26f;
                float a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                int sc = Mathf.Max(1, ft.fontSize);
                float w = Measure(ft.text, sc);
                Text(Mathf.Round(ft.pos.x - w * 0.5f), Mathf.Round(ft.pos.y - rise), ft.text, new Color(ft.color.r, ft.color.g, ft.color.b, a), sc);
            }
        }

        private void DrawScreenOverlays()
        {
            if (Time.time < _flashUntil)
            {
                float a = (_flashUntil - Time.time) / 0.35f;
                Fill(new Rect(0, 0, W, H), new Color(_flashColor.r, _flashColor.g, _flashColor.b, _flashColor.a * Mathf.Clamp01(a)));
            }
            if (viewMode != GameViewMode.Battle) return;
            bool lowTime = battlePhase == BattlePhase.AlgorithmicChallenge && !NoTimer && !Reading && questionTimer / Mathf.Max(0.1f, questionMaxTime) <= 0.30f;
            float edge = 0f;
            Color col = new Color(0.75f, 0.05f, 0.08f);
            if (lowTime && !accessibilityNoShake) edge = 0.35f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * (questionTimer < 2f ? 9f : 6f)));
            if (panicStacks >= 2 && !accessibilityNoShake) { edge = Mathf.Max(edge, 0.4f); if (!lowTime) col = new Color(0.12f, 0.02f, 0.10f); }
            if (edge <= 0f) return;
            for (int i = 0; i < 5; i++)
            {
                float w = 6 + i * 6;
                Color c = new Color(col.r, col.g, col.b, edge * (1f - i / 5f) * 0.35f);
                Fill(new Rect(0, 0, w, H), c); Fill(new Rect(W - w, 0, w, H), c);
                Fill(new Rect(0, 0, W, w), c); Fill(new Rect(0, H - w, W, w), c);
            }
        }

        // ---------------------------------------------------------------- HUD
        private void DrawBattleHud()
        {
            Rect left = new Rect(0, 248, 200, 112), right = new Rect(202, 248, 438, 112);
            Panel(left); Panel(right);

            Rect port = new Rect(left.x + 8, left.y + 8, 36, 36);
            NineSlice(port, T("ui2/slot_normal"), 4);
            var it = T("anim/hero_idle");
            if (it != null && IsRepaint)
            {
                int n = it.width / it.height;
                int f = AnimLibrary.FrameAt("hero_idle", Time.time, true);
                GUI.DrawTextureWithTexCoords(new Rect(port.x + 2, port.y + 2, 32, 32), it, new Rect((f + 8f / 48f) / n, 16f / 48f, (32f / 48f) / n, 32f / 48f), true);
            }
            Text(left.x + 50, left.y + 12, "Aegis", Gold);
            Text(left.x + 50, left.y + 25, $"Level {playerLevel}", Dim);

            float hr = (float)hp / maxHp;
            _heroHpShown = Mathf.MoveTowards(_heroHpShown, hr, Time.deltaTime * 0.6f);
            Icon("icons/hp", left.x + 8, left.y + 52);
            Bar(new Rect(left.x + 21, left.y + 53, 110, 9), hr, hr < 0.2f ? new Color(1f, 0.35f, 0.2f) : new Color(0.85f, 0.2f, 0.25f), _heroHpShown);
            Text(left.x + 136, left.y + 53, $"{hp}/{maxHp}", Ink);
            Icon("icons/mp", left.x + 8, left.y + 68);
            Bar(new Rect(left.x + 21, left.y + 69, 110, 9), (float)mp / maxMp, new Color(0.3f, 0.5f, 0.95f));
            Text(left.x + 136, left.y + 69, $"{mp}/{maxMp}", Ink);
            Icon("icons/focus", left.x + 8, left.y + 84);
            Bar(new Rect(left.x + 21, left.y + 85, 110, 9), focus / 100f, focus >= 100 ? Color.Lerp(Gold, Color.white, Mathf.PingPong(Time.time * 2f, 0.6f)) : new Color(0.95f, 0.75f, 0.2f));
            Text(left.x + 136, left.y + 85, focus >= 100 ? "SIAP!" : "Focus", focus >= 100 ? Gold : Dim);

            DrawCommands(right);
        }

        private static readonly string[] CmdNames = { "Serang", "Mantra", "Ramuan", "Kabur" };
        private static readonly string[] CmdIcons = { "icons/cmd_attack", "icons/cmd_spell", "icons/cmd_item", "icons/cmd_flee" };

        private bool BigCommand(Rect r, string label, string icon, bool enabled, bool selected, string key)
        {
            bool hover = enabled && Hover(r);
            NineSlice(r, T(!enabled ? "ui2/btn_disabled" : (hover || selected) ? "ui2/btn_hover" : "ui2/btn_normal"), 4);
            Icon(icon, r.center.x - 12, r.y + 6, 2, enabled ? Color.white : new Color(1, 1, 1, 0.3f));
            TextCenter(new Rect(r.x, r.yMax - 16, r.width, 12), label, !enabled ? new Color(0.4f, 0.4f, 0.46f) : (hover || selected) ? Gold : Ink);
            Text(r.x + 4, r.y + 3, key, Dim);
            if (!InputBlocked && enabled && Event.current.type == EventType.MouseUp && Event.current.button == 0 && r.Contains(PixelUI.Mouse))
            {
                Event.current.Use(); GrimoireAudioManager.Instance?.PlaySfx("select"); return true;
            }
            return false;
        }

        private void DrawCommands(Rect p)
        {
            string hint = "";
            switch (battlePhase)
            {
                case BattlePhase.PlayerCommand:
                {
                    bool[] en = { true, true, !ItemsLocked, !isBossEncounter };
                    string[] hints =
                    {
                        HasM("armored") ? "Kulit batunya keras. Mantra lebih ampuh!" : "Tebas dengan pedang.",
                        "Jawab soal untuk merapal mantra yang kuat.",
                        ItemsLocked ? "Tasmu terkubur, tidak bisa dibuka." : "Minum ramuan (memakai giliran).",
                        isBossEncounter ? "Tidak bisa kabur dari pertarungan ini." : "Mundur selangkah dari monster."
                    };
                    float bw = (p.width - 20 - 18) / 4f;
                    for (int i = 0; i < 4; i++)
                    {
                        Rect r = new Rect(p.x + 10 + i * (bw + 6), p.y + 10, bw, 66);
                        if (Hover(r)) _cmdSel = i;
                        if (BigCommand(r, CmdNames[i], CmdIcons[i], en[i], _cmdSel == i, $"{i + 1}")) RunCommand(i);
                    }
                    hint = hints[Mathf.Clamp(_cmdSel, 0, 3)];
                    break;
                }
                case BattlePhase.SpellSelect:
                {
                    var spells = SpellsAvailable();
                    float bw = (p.width - 26) / 2f;
                    for (int i = 0; i < spells.Count && i < 6; i++)
                    {
                        var sp = spells[i];
                        Rect r = new Rect(p.x + 10 + (i % 2) * (bw + 6), p.y + 8 + (i / 2) * 22, bw, 20);
                        if (Hover(r)) _cmdSel = i;
                        string cost = sp.focusCost > 0 ? "Focus" : $"{GetEffectiveMpCost(sp)} MP";
                        if (Button(r, sp.name.Replace(" [LIMIT]", ""), "icons/cmd_spell", CanCast(sp), _cmdSel == i, sp.focusCost > 0 ? "gold" : "normal", cost)) BeginSpellCast(sp);
                    }
                    if (Button(new Rect(p.xMax - 80, p.yMax - 24, 70, 16), "Kembali", null, true, false, align: 1)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 1; }
                    if (spells.Count > 0) { var s = spells[Mathf.Clamp(_cmdSel, 0, spells.Count - 1)]; hint = SpellLocked(s) ? "Kamu sedang Bisu." : s.desc; }
                    break;
                }
                case BattlePhase.ItemSelect:
                {
                    var items = BattleItems();
                    if (items.Count == 0) Text(p.x + 12, p.y + 14, "Tas kosong. Beli ramuan di lapak Mordecai.", Dim);
                    float bw = (p.width - 26) / 2f;
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = GrimoireDatabase.Items[items[i]];
                        Rect r = new Rect(p.x + 10 + (i % 2) * (bw + 6), p.y + 8 + (i / 2) * 22, bw, 20);
                        if (Hover(r)) _cmdSel = i;
                        if (Button(r, item.name, "items/" + item.iconKey, true, _cmdSel == i, "normal", $"x{consumables[items[i]]}")) UseConsumableItem(items[i]);
                    }
                    if (Button(new Rect(p.xMax - 80, p.yMax - 24, 70, 16), "Kembali", null, true, false, align: 1)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 2; }
                    if (items.Count > 0) hint = GrimoireDatabase.Items[items[Mathf.Clamp(_cmdSel, 0, items.Count - 1)]].desc;
                    break;
                }
                case BattlePhase.AlgorithmicChallenge:
                {
                    Text(p.x + 12, p.y + 14, "Pilih jawaban di perkamen  (tombol 1-4)", Ink);
                    bool canHint = !hintUsedOnQuestion && mp >= HintCost && !OptionsHidden;
                    if (Button(new Rect(p.x + 12, p.y + 32, 200, 20), $"Petunjuk: buang 1 jawaban ({HintCost} MP)", "icons/cmd_hint", canHint, false, "normal", "H")) UseScholarHint();
                    if (_volleyTotal > 1 && !isChallengeForSpell) Text(p.x + 12, p.y + 62, $"Serangan {_volleyTotal - _volleyLeft} dari {_volleyTotal}", Gold);
                    break;
                }
                default:
                    break;
            }
            if (hint.Length > 0) TextWrap(new Rect(p.x + 12, p.yMax - 22, p.width - 100, 14), hint, Dim, 1, true, 1);
        }

        // ---------------------------------------------------------------- question pop-up
        private void DrawThemedArray(float cx, float baseY, int[] arr, float maxH, Color valCol, Color idxCol, QuestionPayload q)
        {
            int n = arr.Length;
            int act = Mathf.Clamp(activeMonster != null && viewMode == GameViewMode.Battle ? activeMonster.act : currentAct, 1, 3);
            string theme = act == 1 ? "book" : (act == 2 ? "candle" : "crystal");
            Texture2D top = T($"array/{theme}_top"), body = T($"array/{theme}_body"), band = T($"array/{theme}_band"), bas = T($"array/{theme}_base");
            const float colW = 16, gap = 10;
            float x0 = Mathf.Round(cx - (n * colW + (n - 1) * gap) * 0.5f);
            int maxVal = Mathf.Max(1, arr.Max());
            Color[] spines = { new Color(0.85f, 0.45f, 0.42f), new Color(0.45f, 0.62f, 0.9f), new Color(0.55f, 0.8f, 0.55f), new Color(0.9f, 0.78f, 0.45f), new Color(0.72f, 0.55f, 0.88f) };
            bool blink = Mathf.FloorToInt(Time.time * 4f) % 2 == 0;
            for (int i = 0; i < n; i++)
            {
                float x = x0 + i * (colW + gap);
                bool hl = q != null && q.highlightIndices != null && q.highlightIndices.Contains(i);
                bool isMid = q != null && q.midIdx == i, isPivot = q != null && q.pivotIdx == i;
                bool isLow = q != null && q.lowIdx == i, isHigh = q != null && q.highIdx == i;
                bool fog = viewMode == GameViewMode.Battle && HasM("fog") && !isMid && (i == 0 || i == n - 1);
                Color tint = theme == "book" ? spines[i % spines.Length] : Color.white;
                if (isPivot) tint = new Color(1f, 0.55f, 1f);
                else if (isMid) tint = new Color(1f, 0.86f, 0.3f);
                else if (hl && blink) tint = new Color(1f, 0.95f, 0.45f);
                if (fog) tint = new Color(0.35f, 0.35f, 0.42f);
                int tiles = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, maxH / 4f - 2f, arr[i] / (float)maxVal)), 2, 30);
                float y = baseY - 4;
                Image(new Rect(x, y, 16, 4), bas);
                for (int k = 0; k < tiles; k++) { y -= 4; Image(new Rect(x, y, 16, 4), k % 4 == 2 ? band : body, tint); }
                if (top != null)
                {
                    int frames = Mathf.Max(1, top.width / 16);
                    y -= top.height;
                    Frame(new Rect(x, y, 16, top.height), top, frames > 1 ? (Mathf.FloorToInt(Time.time * 8f) + i) % frames : 0, theme == "candle" ? Color.white : tint);
                }
                string v = fog ? "??" : arr[i].ToString();
                Text(Mathf.Round(x + 8 - Measure(v) * 0.5f), y - 11, v, valCol, 1, false);
                string ix = $"[{i}]";
                Text(Mathf.Round(x + 8 - Measure(ix) * 0.5f), baseY + 2, ix, idxCol, 1, false);
                string tag = isPivot ? "PIVOT" : isMid ? "MID" : isLow ? "LOW" : isHigh ? "HIGH" : null;
                if (tag != null) Text(Mathf.Round(x + 8 - Measure(tag) * 0.5f), baseY + 12, tag, new Color(0.62f, 0.32f, 0.02f), 1, false);
            }
        }

        private void DrawOptionLabel(Rect r, string label, Color col)
        {
            int nLines = PixelUI.Font != null ? PixelUI.Font.Wrap(label, Mathf.FloorToInt(r.width - 18)).Count : 1;
            float ly = nLines <= 1 ? r.y + Mathf.Round((r.height - 9) * 0.5f) - 1 : r.y + 2;
            TextWrap(new Rect(r.x + 14, ly, r.width - 18, r.height - 4), label, col, 1, true, 2);
        }

        private Rect PopupRect(float h) => new Rect(150, Mathf.Round(Mathf.Max(6, 122 - h * 0.5f)), 340, h);

        private void DrawQuestionPopup()
        {
            var q = currentQuestion;
            if (q == null) return;
            bool locked = battlePhase == BattlePhase.Revealing;
            float pop = GuiSprite.EaseOutBack(Mathf.Clamp01((Time.time - _popupStart) / 0.25f));
            if (locked) pop = 1f - GuiSprite.EaseOutCubic(Mathf.Clamp01((Time.time - _lockedAt - 0.25f) / 0.15f));
            bool hasArr = q.arrayData != null && q.arrayData.Length > 0;
            float promptH = Mathf.Min(WrapHeight(q.prompt, 316), 60);
            float arrBlock = hasArr ? 70 : 4;
            float need = 10 + 30 + arrBlock + promptH + 6 + 55 + 10;
            if (need > 234 && hasArr) { arrBlock = Mathf.Max(46, arrBlock - (need - 234)); need = 234; }
            Rect full = PopupRect(Mathf.Min(234, need));
            Rect p = new Rect(full.center.x - full.width * 0.5f * pop, full.center.y - full.height * 0.5f * pop, full.width * pop, full.height * pop);
            Shade(new Rect(0, 0, W, 244), 0.35f * pop);
            if (pop < 0.05f) return;
            Panel(p, "parchment");
            if (pop < 0.98f) return;

            float x = p.x + 12, w = p.width - 24, y = p.y + 10;
            string head = isChallengeForSpell ? $"Rapal {pendingSpell?.name.Replace(" [LIMIT]", "")}!" : $"Tangkis serangan {activeMonster.name}!";
            Text(x, y, head, ParchInk, 1, false);
            TextRight(x + w, y, q.algorithmName, ParchDim, 1, false);
            y += 14;

            Rect tb = new Rect(x, y, w, 9);
            if (NoTimer) Text(x, y, "Tanpa batas waktu", new Color(0.2f, 0.45f, 0.2f), 1, false);
            else
            {
                float rem = Mathf.Clamp01(questionTimer / Mathf.Max(0.1f, questionMaxTime));
                Color bc = rem >= 0.6f ? new Color(0.36f, 0.62f, 1f) : (rem > 0.3f ? new Color(1f, 0.8f, 0.2f) : new Color(0.95f, 0.28f, 0.25f));
                Bar(tb, Reading ? 1f : rem, bc);
                if (Reading) TextCenter(new Rect(tb.x, tb.y - 1, tb.width, tb.height + 2), "baca dulu...", Ink, 1, true);
            }
            y += 16;

            if (hasArr) { DrawThemedArray(p.center.x, y + arrBlock - 22, q.arrayData, arrBlock - 36, ParchInk, ParchDim, q); y += arrBlock; }
            else y += 4;

            Vector2 j = panicStacks >= 1 && !accessibilityNoShake ? new Vector2(Mathf.Round(Mathf.Sin(Time.time * 37f) * panicStacks * 0.6f), Mathf.Round(Mathf.Cos(Time.time * 29f) * panicStacks * 0.4f)) : Vector2.zero;
            float ph = TextWrap(new Rect(x + j.x, y + j.y, w, 60), q.prompt, ParchInk, 1, false, 5);
            y = Mathf.Min(Mathf.Max(y + ph + 6, p.yMax - 65), p.yMax - 65);

            float ow = (w - 6) * 0.5f, oh = 26;
            for (int slot = 0; slot < 4; slot++)
            {
                int i = _optionOrder[slot];
                Rect r = new Rect(x + (slot % 2) * (ow + 6), y + (slot / 2) * (oh + 3), ow, oh);
                bool elim = eliminatedOptions.Contains(i);
                bool picked = locked && i == _lockedChoice;
                bool hover = !locked && !elim && !OptionsHidden && Hover(r);
                string tex = picked ? "ui2/btn_gold" : elim || (locked && !picked) ? "ui2/btn_disabled" : hover ? "ui2/btn_hover" : "ui2/btn_normal";
                NineSlice(r, T(tex), 4);
                Text(r.x + 5, r.y + 2, $"{slot + 1}", hover || picked ? Gold : Dim);
                string label = OptionsHidden ? "? ? ?" : elim ? "-" : obscuredOptionIdx == i ? "{#8a8ea8}~ sobek ~{/}" : q.options[i];
                DrawOptionLabel(r, label, elim || (locked && !picked) ? new Color(0.45f, 0.45f, 0.5f) : (hover || picked ? Gold : Ink));
                if (hover && Event.current.type == EventType.MouseUp && Event.current.button == 0 && r.Contains(PixelUI.Mouse)) { Event.current.Use(); SubmitSlot(slot); }
            }
        }

        /// <summary>Shown only after the attack has played out.</summary>
        private void DrawRevealCard()
        {
            float pop = GuiSprite.EaseOutBack(Mathf.Clamp01((Time.time - _popupStart) / 0.22f));
            float eh = WrapHeight(revealExplain, 316);
            float h = 62 + (revealCorrect ? 12 : 26) + eh + 30;
            Rect full = PopupRect(h);
            Rect p = new Rect(full.x, full.y + (1f - pop) * 10f, full.width, full.height);
            Panel(p, "parchment");
            float x = p.x + 12, w = p.width - 24, y = p.y + 10;
            Text(x, y, revealTitle, revealColor, 2, false);
            y += 22;
            if (revealEffect.Length > 0) { y += TextWrap(new Rect(x, y, w, 24), revealEffect, ParchDim, 1, false, 2) + 4; }
            Fill(new Rect(x, y, w, 1), new Color(0.4f, 0.3f, 0.2f, 0.3f)); y += 5;
            if (revealCorrect) { y += TextWrap(new Rect(x, y, w, 12), "Jawaban: " + revealAnswer, new Color(0.12f, 0.4f, 0.16f), 1, false, 1) + 2; }
            else
            {
                y += TextWrap(new Rect(x, y, w, 12), "Jawabanmu: " + revealChosen, new Color(0.55f, 0.12f, 0.12f), 1, false, 1) + 2;
                y += TextWrap(new Rect(x, y, w, 12), "Yang benar: " + revealAnswer, new Color(0.12f, 0.4f, 0.16f), 1, false, 1) + 2;
            }
            TextWrap(new Rect(x, y + 2, w, eh + 4), revealExplain, ParchInk, 1, false, 4);
            string next = isChallengeForSpell ? "Lanjut" : (_volleyLeft > 0 ? "Serangan berikutnya" : "Lanjut");
            if (Button(new Rect(p.xMax - 140, p.yMax - 24, 128, 18), next, null, true, true, "gold", "Spasi")) ContinueFromResolution();
        }

        private void DrawMercyPopup()
        {
            Rect p = PopupRect(150);
            Panel(p, "teal");
            Strip(p.x + 8, p.y + 10, "npc/shade", "npc_shade_idle", Time.time, 1, new Color(1, 1, 1, 0.8f + 0.15f * Mathf.Sin(Time.time * 2f)));
            Text(p.x + 80, p.y + 12, "Bayang-bayang Ordo", new Color(0.5f, 0.95f, 0.85f));
            TextWrap(new Rect(p.x + 80, p.y + 26, p.width - 92, 40), "\"Istirahat sebentar, Ksatria. Ayo ulang langkahnya bersama.\"", Ink);
            if (mercyCodexEntry != null)
            {
                Text(p.x + 12, p.y + 80, mercyCodexEntry.title, Gold);
                TextWrap(new Rect(p.x + 12, p.y + 92, p.width - 24, 36), Short(mercyCodexEntry.summary, 160), Ink, 1, true, 3);
            }
            if (Button(new Rect(p.xMax - 110, p.yMax - 24, 98, 18), "Mengerti", null, true, true, "gold", "Spasi")) CloseMercy();
        }

        private void DrawAnalyzePopup()
        {
            var m = activeMonster;
            Rect p = PopupRect(150);
            Panel(p);
            Strip(p.x + 8, p.y + 10, $"anim/{m.id}_idle", m.id + "_idle", Time.time, 1);
            Text(p.x + 76, p.y + 12, m.name, Gold);
            Text(p.x + 76, p.y + 26, difficulty == Difficulty.Hard ? "Topik soal: ???" : "Topik: " + string.Join(", ", m.tpPool.Select(tp => GrimoireDatabase.Codex.FirstOrDefault(c => c.tp == tp)?.title ?? tp).Take(2)), Cyan);
            string st = GrimoireStatus.OnBreak(m.id, bossPhase);
            if (st != null) { Icon($"status/{st}", p.x + 76, p.y + 40); Text(p.x + 91, p.y + 42, "Kalau kena: " + GrimoireStatus.Get(st).name, Red); }
            TextWrap(new Rect(p.x + 12, p.y + 82, p.width - 24, 40), "{#8a8ea8}\"" + m.lore + "\"{/}", Dim, 1, true, 3);
            var weak = SpellsAvailable().Where(s => m.tpPool.Contains(s.tpCode)).Select(s => s.name).ToList();
            if (weak.Count > 0) Text(p.x + 12, p.yMax - 20, "Mantra ampuh: " + string.Join(", ", weak), Green);
            if (Button(new Rect(p.xMax - 80, p.yMax - 24, 68, 18), "Tutup", null, true, true, "gold")) CloseAnalyze();
        }

        private void DrawWonPopup()
        {
            Rect p = PopupRect(winKey || winLevelUp ? 132 : 112);
            Panel(p, "gold");
            TextCenter(new Rect(p.x, p.y + 12, p.width, 20), "MENANG!", Gold, 2);
            float y = p.y + 42;
            Icon("icons/exp", p.x + 110, y); Text(p.x + 124, y + 1, $"+{winExp} EXP", Green);
            Icon("icons/gold", p.x + 190, y); Text(p.x + 204, y + 1, $"+{winGold}", Gold);
            y += 16;
            if (winFlawless) { TextCenter(new Rect(p.x, y, p.width, 12), "Tanpa salah! +1 Logic Fragment", Cyan); y += 14; }
            if (winKey) { Icon("icons/key", p.center.x - 52, y - 3); Text(p.center.x - 34, y, "Kunci Indeks didapat!", Gold); y += 14; }
            if (winLevelUp) TextCenter(new Rect(p.x, y, p.width, 12), $"Naik ke Level {playerLevel}!", Gold);
            if (Button(new Rect(p.center.x - 50, p.yMax - 26, 100, 18), "Lanjut", null, true, true, "gold", "Spasi")) ContinueAfterVictory();
        }

        // =====================================================================
        // ENDING
        // =====================================================================
        private void DrawEnding()
        {
            Image(new Rect(0, 0, W, H), T("bg/bg_title"));
            Shade(new Rect(0, 0, W, H), 0.55f);
            TextCenter(new Rect(0, 40, W, 24), "Katalog Agung kembali terurut", Gold, 2);
            Strip(272, 76, "anim/hero_victory", "hero_victory", Time.time, 2);
            string txt = "Entropy tidak bisa dihancurkan, hanya bisa diurutkan. Berkat searching dan sorting, Aegis menyusun kembali Katalog Agung. Matahari Algoria terbit lagi pada urutannya.";
            var lines = PixelUI.Font != null ? PixelUI.Font.Wrap(txt, 440) : new List<string>();
            for (int i = 0; i < lines.Count; i++) TextCenter(new Rect(0, 184 + i * 12, W, 12), lines[i], Ink);
            int total = telemetryLog.Count, cor = telemetryLog.Count(r => r.isCorrect);
            TextCenter(new Rect(0, 236, W, 12), $"Soal dijawab: {total}   Benar: {(total > 0 ? cor * 100 / total : 0)}%", Cyan);
            if (Button(new Rect(255, 266, 130, 20), "Ke Layar Judul", null, true, true, "gold", align: 1))
            {
                viewMode = GameViewMode.Title; _titleMode = 0; runStarted = false;
            }
        }

        /// <summary>QA hook for GrimoireScreenshotTour: jump straight to a UI state.</summary>
        public void TourSet(string what, int a = 0, int b = 0)
        {
            switch (what)
            {
                case "title": viewMode = GameViewMode.Title; activeModal = ModalOverlay.None; _titleMode = a; _diffSel = b; break;
                case "guide": activeModal = ModalOverlay.Guide; _guideChapter = _listSel = a; _modalScroll = b; break;
                case "codex": activeModal = ModalOverlay.CodexBestiary; _codexTab = a; _listSel = b; _bestiaryAnimStart = Time.time; break;
                case "bestiaryAnim": _bestiaryAnim = a; _bestiaryAnimStart = Time.time; break;
                case "shop": activeModal = ModalOverlay.ShopInventory; _shopTab = a; break;
                case "modal": activeModal = (ModalOverlay)a; break;
                case "closeModal": activeModal = ModalOverlay.None; break;
                case "spellMenu": battlePhase = BattlePhase.SpellSelect; _cmdSel = a; break;
                case "status": _pStatus[a == 0 ? "poison" : a == 1 ? "silence" : "torn_page"] = 2; break;
                case "skipDialog": SkipDialog(); break;
                case "keys": keysCollected = a; break;
                case "walkTo": { var n = map.nodes.FirstOrDefault(x => x.type == (NodeType)a); if (n != null) { currentNode = n.id; map.RevealAround(n.id, 3); _camPos = NodeWorld(n); } break; }
                case "revealAll": foreach (var n in map.nodes) n.revealed = true; break;
                case "puzzle": { var n = map.nodes.First(x => x.type == NodeType.Puzzle); _seenTips.Add("puzzle"); _choices.Clear(); stepsRemaining = 0; OpenPuzzle(n); break; }
            }
        }

        /// <summary>QA hook: run the story intro + act card synchronously skipped.</summary>
        public bool TourDialogOpen => DialogOpen;
        public void TourAdvanceDialog() => AdvanceDialog();
    }
}
