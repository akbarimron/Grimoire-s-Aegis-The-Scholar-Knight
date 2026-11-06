using System.Collections.Generic;
using System.Linq;
using GrimoireAegis.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using static GrimoireAegis.UI.PixelUI;

namespace GrimoireAegis.UI
{
    /// <summary>
    /// All screens, drawn on the 640x360 pixel canvas (PixelUI). Layout follows the
    /// Lost in Multiverse RPG pattern: side-view stage on top, three-panel HUD below,
    /// condition panel top-left, help/settings top-right.
    /// </summary>
    public partial class GrimoireGameController
    {
        // ---- UI state ----
        private int _titleMode;          // 0 main, 1 difficulty, 2 act select
        private int _titleSel, _diffSel = 1, _actSel;
        private int _cmdSel;             // battle command / list cursor
        private int _listSel;            // modal list cursor
        private float _modalScroll;
        private int _shopTab, _codexTab, _guideChapter, _bestiaryAnim;
        private float _monHpShown = 1f, _heroHpShown = 1f;
        private float _tokenHopStart = -10f;

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
            switch (viewMode)
            {
                case GameViewMode.Title: DrawTitle(); break;
                case GameViewMode.ActIntro: DrawActIntro(); break;
                case GameViewMode.Board: DrawBoard(); break;
                case GameViewMode.Battle: DrawBattle(); break;
                case GameViewMode.Victory: DrawEnding(); break;
            }
            if (activeModal != ModalOverlay.None) DrawModal();
            DrawScreenOverlays();
            End();
        }

        // =====================================================================
        // KEYBOARD (all screens)
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
            bool answering = viewMode == GameViewMode.Battle && battlePhase == BattlePhase.AlgorithmicChallenge && activeModal == ModalOverlay.None;

            if (answering)
            {
                if (Pressed(Key.Digit1) || Pressed(Key.Numpad1)) SubmitSlot(0);
                else if (Pressed(Key.Digit2) || Pressed(Key.Numpad2)) SubmitSlot(1);
                else if (Pressed(Key.Digit3) || Pressed(Key.Numpad3)) SubmitSlot(2);
                else if (Pressed(Key.Digit4) || Pressed(Key.Numpad4)) SubmitSlot(3);
                else if (Pressed(Key.H)) UseScholarHint();
                else if (Pressed(Key.Escape)) ToggleModal(ModalOverlay.Pause);
                return;
            }

            if (Pressed(Key.Escape))
            {
                if (activeModal != ModalOverlay.None) { activeModal = ModalOverlay.None; return; }
                if (viewMode == GameViewMode.Title && _titleMode != 0) { _titleMode = 0; return; }
                if (viewMode == GameViewMode.Battle && (battlePhase == BattlePhase.SpellSelect || battlePhase == BattlePhase.ItemSelect)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 0; return; }
                if (viewMode == GameViewMode.Board || viewMode == GameViewMode.Battle) { ToggleModal(ModalOverlay.Pause); return; }
            }
            if (Pressed(Key.G)) { ToggleModal(ModalOverlay.Guide); return; }
            if (Pressed(Key.C)) { ToggleModal(ModalOverlay.CodexBestiary); return; }
            if (Pressed(Key.O)) { ToggleModal(ModalOverlay.Settings); return; }
            if (Pressed(Key.T)) { ToggleModal(ModalOverlay.TeacherAnalytics); return; }
            if (Pressed(Key.I) && viewMode != GameViewMode.Battle) { _shopTab = 1; ToggleModal(ModalOverlay.ShopInventory); return; }

            if (activeModal != ModalOverlay.None) { HandleModalKeys(); return; }

            switch (viewMode)
            {
                case GameViewMode.Title: HandleTitleKeys(); break;
                case GameViewMode.ActIntro: if (Confirm && Time.time - _actIntroStart > 0.4f) EnterBoardFromIntro(); break;
                case GameViewMode.Board:
                    if (Pressed(Key.Space)) { if (stepsRemaining == 0) RollExplorationDice(); }
                    else if (Pressed(Key.Enter) && stepsRemaining > 0) { stepsRemaining = 0; ResolveBoardTileEvent(board.GetTile(currentTileId)); }
                    else if (Pressed(Key.UpArrow) || Pressed(Key.W)) StepDirection(0, -1);
                    else if (Pressed(Key.DownArrow) || Pressed(Key.S)) StepDirection(0, 1);
                    else if (Pressed(Key.LeftArrow) || Pressed(Key.A)) StepDirection(-1, 0);
                    else if (Pressed(Key.RightArrow) || Pressed(Key.D)) StepDirection(1, 0);
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
            else if (_titleMode == 1)
            {
                if (Left) _diffSel = (_diffSel + 2) % 3;
                if (Right) _diffSel = (_diffSel + 1) % 3;
                if (Confirm) StartNewRun((Difficulty)_diffSel, 1);
            }
            else
            {
                if (Left) _actSel = (_actSel + 2) % 3;
                if (Right) _actSel = (_actSel + 1) % 3;
                if (Confirm) StartActFromTitle(_actSel + 1);
            }
        }

        private void HandleBattleKeys()
        {
            switch (battlePhase)
            {
                case BattlePhase.PlayerCommand:
                {
                    if (Up) _cmdSel = (_cmdSel + 4) % 5;
                    if (Down) _cmdSel = (_cmdSel + 1) % 5;
                    for (int i = 0; i < 5; i++) if (Pressed(Key.Digit1 + i)) { _cmdSel = i; RunCommand(i); return; }
                    if (Confirm) RunCommand(_cmdSel);
                    break;
                }
                case BattlePhase.SpellSelect:
                {
                    int n = GrimoireDatabase.Spells.Count;
                    if (Up) _cmdSel = (_cmdSel + n - 1) % n;
                    if (Down) _cmdSel = (_cmdSel + 1) % n;
                    if (Confirm) BeginSpellCast(GrimoireDatabase.Spells[_cmdSel]);
                    if (Pressed(Key.Backspace)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 1; }
                    break;
                }
                case BattlePhase.ItemSelect:
                {
                    var items = BattleItems();
                    if (items.Count == 0) { if (Confirm || Pressed(Key.Backspace)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 2; } break; }
                    if (Up) _cmdSel = (_cmdSel + items.Count - 1) % items.Count;
                    if (Down) _cmdSel = (_cmdSel + 1) % items.Count;
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
                case 1: battlePhase = BattlePhase.SpellSelect; _cmdSel = 0; break;
                case 2:
                    if (ItemsLocked) { GrimoireAudioManager.Instance?.PlaySfx("error"); AddCombatLog("{#f05a50}Terkubur!{/} Tas terkunci."); break; }
                    battlePhase = BattlePhase.ItemSelect; _cmdSel = 0; break;
                case 3: AnalyzeMonster(); break;
                case 4: TryFlee(); break;
            }
        }

        private List<string> BattleItems() =>
            new[] { "pot_hp", "pot_mp", "pot_elixir", "scroll_clarity", "smoke_scroll" }
                .Where(k => GrimoireDatabase.Items.ContainsKey(k) && consumables.TryGetValue(k, out int q) && q > 0).ToList();

        // =====================================================================
        // TITLE
        // =====================================================================
        private List<string> TitleItems()
        {
            var l = new List<string>();
            if (runStarted) l.Add("Lanjutkan");
            l.AddRange(new[] { "Petualangan Baru", "Pilih Act", "Buku Panduan", "Codex & Bestiary", "Mode Guru", "Pengaturan", "Keluar" });
            return l;
        }

        private void TitleAction(string item)
        {
            switch (item)
            {
                case "Lanjutkan": viewMode = GameViewMode.Board; GrimoireAudioManager.Instance?.PlayMusic(GrimoireDatabase.Stages[currentAct].musicKey); break;
                case "Petualangan Baru": _titleMode = 1; _diffSel = (int)difficulty; break;
                case "Pilih Act": _titleMode = 2; _actSel = currentAct - 1; break;
                case "Buku Panduan": ToggleModal(ModalOverlay.Guide); break;
                case "Codex & Bestiary": ToggleModal(ModalOverlay.CodexBestiary); break;
                case "Mode Guru": ToggleModal(ModalOverlay.TeacherAnalytics); break;
                case "Pengaturan": ToggleModal(ModalOverlay.Settings); break;
                case "Keluar": Application.Quit(); break;
            }
        }

        private void StartActFromTitle(int act)
        {
            if (!runStarted) StartNewRun(difficulty, act);
            else StartActExpedition(act);
        }

        private void DrawTitle()
        {
            Image(new Rect(0, 0, W, H), T("bg/bg_title"));
            Shade(new Rect(0, 0, W, H), 0.45f);
            // drifting gold motes
            for (int i = 0; i < 26; i++)
            {
                float x = (i * 97 + Time.time * (6 + i % 5)) % W;
                float y = (i * 53 + Mathf.Sin(Time.time * 0.6f + i) * 6f + 360 - (Time.time * (3 + i % 4) + i * 30) % 360);
                Fill(new Rect(Mathf.Round(x), Mathf.Round(y), 1, 1), new Color(1f, 0.86f, 0.42f, 0.25f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time + i))));
            }

            TextCenter(new Rect(0, 22, W, 28), "GRIMOIRE'S AEGIS", Gold, 3);
            TextCenter(new Rect(0, 54, W, 14), "THE SCHOLAR KNIGHT  -  THE ORDER OF LOST INDEXES", Cyan, 1);
            TextCenter(new Rect(0, 70, W, 12), "\"Di dunia yang diacak kekacauan, hanya logika yang bisa menemukan jalan pulang.\"", Dim, 1);

            // hero on a glowing sigil, final boss looming behind
            float glow = 0.18f + 0.06f * Mathf.Sin(Time.time * 2f);
            for (int r = 0; r < 6; r++) Fill(new Rect(108 - r * 8, 296 - r, 120 + r * 16, 2 + r * 2 / 3), new Color(0.4f, 0.7f, 1f, glow * (1 - r / 6f)));
            Strip(250, 92, "anim/entropy_chaos_king_idle", "entropy_chaos_king_idle", Time.time, 2, new Color(1, 1, 1, 0.35f));
            Strip(96, 154, "anim/hero_idle", "hero_idle", Time.time, 3);

            if (_titleMode == 0) DrawTitleMenu();
            else if (_titleMode == 1) DrawDifficultyPicker();
            else DrawActPicker();

            Text(8, 348, "v2.0  -  [Panah] pilih  -  [Enter] konfirmasi  -  [G] panduan  -  [O] opsi  -  [Esc] kembali", Dim);
            TextRight(632, 348, "Kelompok 3 - Pengembangan APE", Dim);
        }

        private void DrawTitleMenu()
        {
            var items = TitleItems();
            float h = 14 + items.Count * 18;
            Rect p = new Rect(430, 110, 184, h);
            Panel(p);
            for (int i = 0; i < items.Count; i++)
            {
                Rect r = new Rect(p.x + 8, p.y + 7 + i * 18, p.width - 16, 16);
                if (Hover(r)) _titleSel = i;
                string icon = items[i] == "Buku Panduan" ? "icons/cmd_guide" : items[i] == "Pengaturan" ? "icons/cmd_settings" :
                              items[i] == "Codex & Bestiary" ? "icons/cmd_spell" : items[i] == "Mode Guru" ? "icons/cmd_analyze" : null;
                if (Button(r, items[i], icon, true, _titleSel == i, i == 0 ? "gold" : "normal", align: 1)) TitleAction(items[i]);
            }
            if (runStarted)
            {
                Panel(new Rect(430, p.yMax + 4, 184, 34), "dark");
                Text(438, p.yMax + 10, $"Act {ActRoman(currentAct)}  -  Lv.{playerLevel}  -  {GrimoireStatus.DifficultyName(difficulty)}", Ink);
                Text(438, p.yMax + 22, $"HP {hp}/{maxHp}   Gold {gold}   Fragmen {logicFragments}", Dim);
            }
        }

        private void DrawDifficultyPicker()
        {
            Shade(new Rect(0, 0, W, H), 0.55f);
            TextCenter(new Rect(0, 92, W, 16), "PILIH TINGKAT KESULITAN", Gold, 2);
            string[] icons = { "icons/heart_full", "icons/timer", "icons/skull" };
            string[] kinds = { "teal", "panel", "red" };
            for (int i = 0; i < 3; i++)
            {
                var d = (Difficulty)i;
                Rect c = new Rect(44 + i * 188, 120, 176, 172);
                if (Hover(c)) _diffSel = i;
                bool sel = _diffSel == i;
                Panel(c, sel ? "gold" : kinds[i]);
                float bob = sel ? Mathf.Round(Mathf.Sin(Time.time * 4f) * 1.5f) : 0;
                Icon(icons[i], c.x + 12, c.y + 12 + bob, 2);
                Text(c.x + 40, c.y + 14, GrimoireStatus.DifficultyName(d), sel ? Gold : Ink, 2);
                string sub = i == 0 ? "Tanpa batas waktu" : (i == 1 ? "Mode Scholar (standar)" : "Nightmare");
                Text(c.x + 40, c.y + 32, sub, i == 0 ? Green : (i == 2 ? Red : Cyan));
                TextWrap(new Rect(c.x + 10, c.y + 52, c.width - 20, 90), GrimoireStatus.DifficultyDesc(d), Ink);
                if (Button(new Rect(c.x + 26, c.yMax - 26, c.width - 52, 18), "Mulai", null, true, sel, sel ? "gold" : "normal", align: 1))
                    StartNewRun(d, 1);
            }
            TextCenter(new Rect(0, 300, W, 12), "Mode bisa diganti kapan saja di Pengaturan [O].", Dim);
            if (Button(new Rect(280, 318, 80, 18), "Kembali", null, true, false, align: 1)) _titleMode = 0;
        }

        private void DrawActPicker()
        {
            Shade(new Rect(0, 0, W, H), 0.55f);
            TextCenter(new Rect(0, 92, W, 16), "PILIH ACT", Gold, 2);
            for (int a = 1; a <= 3; a++)
            {
                var st = GrimoireDatabase.Stages[a];
                Rect c = new Rect(44 + (a - 1) * 188, 118, 176, 180);
                if (Hover(c)) _actSel = a - 1;
                bool sel = _actSel == a - 1;
                Panel(c, sel ? "gold" : "panel");
                Image(new Rect(c.x + 8, c.y + 8, 160, 90), T($"bg/bg_s{a}"));
                Frame(new Rect(c.x + 8, c.y + 8, 160, 90), new Color(0, 0, 0, 0.8f));
                Strip(c.x + 112, c.y + 34, $"anim/{st.bossId}_idle", st.bossId + "_idle", Time.time + a, 1);
                Text(c.x + 10, c.y + 104, $"ACT {ActRoman(a)}", ActAccent[a]);
                TextWrap(new Rect(c.x + 10, c.y + 116, c.width - 20, 24), st.title.Replace($"ACT {ActRoman(a)}: ", ""), Ink);
                TextWrap(new Rect(c.x + 10, c.y + 130, c.width - 20, 24), st.subtitle, Dim);
                if (Button(new Rect(c.x + 26, c.yMax - 24, c.width - 52, 18), $"Mulai Act {ActRoman(a)}", null, true, sel, sel ? "gold" : "normal", align: 1))
                    StartActFromTitle(a);
            }
            if (Button(new Rect(280, 318, 80, 18), "Kembali", null, true, false, align: 1)) _titleMode = 0;
        }

        // =====================================================================
        // ACT INTRO CARD (LIM "Stage 1 / KOTA KELAM" style)
        // =====================================================================
        private void DrawActIntro()
        {
            Fill(new Rect(0, 0, W, H), new Color(0.015f, 0.015f, 0.03f));
            var st = GrimoireDatabase.Stages[currentAct];
            float a = Mathf.Clamp01((Time.time - _actIntroStart) / 0.8f);
            Color dim = new Color(Dim.r, Dim.g, Dim.b, a), ink = new Color(Ink.r, Ink.g, Ink.b, a), acc = Accent; acc.a = a;
            TextCenter(new Rect(0, 104, W, 12), $"Act {ActRoman(currentAct)}", dim);
            TextCenter(new Rect(0, 120, W, 24), st.title.Replace($"ACT {ActRoman(currentAct)}: ", ""), acc, 2);
            TextCenter(new Rect(0, 148, W, 12), st.subtitle, new Color(Gold.r, Gold.g, Gold.b, a));
            var lines = PixelUI.Font != null ? PixelUI.Font.Wrap(st.intro, 440) : new List<string>();
            for (int i = 0; i < lines.Count; i++) TextCenter(new Rect(0, 172 + i * 12, W, 12), lines[i], ink);
            Strip(296, 214 + lines.Count * 4, $"anim/{st.bossId}_idle", st.bossId + "_idle", Time.time, 1, new Color(1, 1, 1, 0.55f * a));
            if (Mathf.Sin(Time.time * 3f) > -0.3f) TextCenter(new Rect(0, 300, W, 12), "- klik / spasi -", dim);
            if (Event.current.type == EventType.MouseUp && Time.time - _actIntroStart > 0.4f) { Event.current.Use(); EnterBoardFromIntro(); }
        }

        // =====================================================================
        // BOARD (labyrinth)
        // =====================================================================
        private void DrawTopStats(Rect bar)
        {
            Panel(bar);
            Text(bar.x + 8, bar.y + 9, $"ACT {ActRoman(currentAct)}", Accent);
            float x = bar.x + 44;
            Icon("icons/hp", x, bar.y + 8); Bar(new Rect(x + 12, bar.y + 9, 54, 8), (float)hp / maxHp, new Color(0.85f, 0.2f, 0.25f)); Text(x + 70, bar.y + 9, $"{hp}", Ink); x += 96;
            Icon("icons/mp", x, bar.y + 8); Bar(new Rect(x + 12, bar.y + 9, 40, 8), (float)mp / maxMp, new Color(0.3f, 0.5f, 0.95f)); Text(x + 56, bar.y + 9, $"{mp}", Ink); x += 78;
            Icon("icons/focus", x, bar.y + 8); Bar(new Rect(x + 12, bar.y + 9, 30, 8), focus / 100f, new Color(0.95f, 0.75f, 0.2f)); x += 48;
            Icon("icons/gold", x, bar.y + 8); Text(x + 12, bar.y + 9, $"{gold}", Gold); x += 40;
            Icon("icons/fragment", x, bar.y + 8); Text(x + 12, bar.y + 9, $"{logicFragments}", Cyan); x += 28;
            Text(x, bar.y + 9, $"Lv.{playerLevel}", Ink);
            float bx = bar.xMax - 4;
            if (Button(new Rect(bx -= 44, bar.y + 5, 42, 16), "Opsi", null, true, false, align: 1)) ToggleModal(ModalOverlay.Settings);
            if (Button(new Rect(bx -= 50, bar.y + 5, 48, 16), "Codex", null, true, false, align: 1)) ToggleModal(ModalOverlay.CodexBestiary);
            if (Button(new Rect(bx -= 58, bar.y + 5, 56, 16), "Panduan", null, true, false, align: 1)) ToggleModal(ModalOverlay.Guide);
            if (viewMode == GameViewMode.Board && Button(new Rect(bx -= 40, bar.y + 5, 38, 16), "Tas", null, true, false, align: 1)) { _shopTab = 1; ToggleModal(ModalOverlay.ShopInventory); }
        }

        private static string TileIconKey(BoardTileNode t)
        {
            if (t.cleared && t.type != BoardTileType.Boss && t.type != BoardTileType.Merchant && t.type != BoardTileType.Rest && t.type != BoardTileType.Shrine) return "icon_cleared";
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

        private void DrawBoard()
        {
            Vector2 sh = ShakeOffset();
            Image(new Rect(sh.x - 4, sh.y - 4, W + 8, H + 8), T($"bg/bg_s{currentAct}"));
            Shade(new Rect(0, 0, W, H), 0.5f);
            DrawTopStats(new Rect(0, 0, W, 28));
            if (board == null) return;

            const float tile = 32f, pitch = 42f;
            float totalW = (board.width - 1) * pitch + tile, totalH = (board.height - 1) * pitch + tile;
            float ox = Mathf.Round((W - totalW) * 0.5f), oy = Mathf.Round(34 + (256 - totalH) * 0.5f);
            var cur = board.GetTile(currentTileId);
            Vector2 C(BoardTileNode t) => new Vector2(ox + t.gx * pitch + tile * 0.5f, oy + t.gy * pitch + tile * 0.5f);

            // corridors
            foreach (var t in board.tiles)
            foreach (int nbId in t.neighbors)
            {
                if (nbId < t.id) continue;
                var nb = board.GetTile(nbId);
                if (!t.revealed && !nb.revealed) continue;
                Vector2 a = C(t), b = C(nb);
                bool step = stepsRemaining > 0 && (t.id == currentTileId || nb.id == currentTileId);
                Color col = step ? new Color(1f, 0.85f, 0.3f, 0.9f) : new Color(0.24f, 0.29f, 0.47f, 0.9f);
                Rect line = new Rect(Mathf.Min(a.x, b.x) - 2, Mathf.Min(a.y, b.y) - 2, Mathf.Abs(b.x - a.x) + 4, Mathf.Abs(b.y - a.y) + 4);
                Fill(new Rect(line.x, line.y + 1, line.width, line.height), new Color(0, 0, 0, 0.6f));
                Fill(line, col);
            }

            BoardTileNode hovered = null;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            foreach (var t in board.tiles)
            {
                Rect r = new Rect(ox + t.gx * pitch, oy + t.gy * pitch, tile, tile);
                bool isCur = t.id == currentTileId;
                bool next = stepsRemaining > 0 && cur != null && cur.neighbors.Contains(t.id);
                Image(r, T($"tiles/plate_s{currentAct}_{t.variant}"));
                if (!t.revealed && !isCur)
                {
                    Image(r, T("tiles/fog"));
                    TextCenter(r, "?", Dim);
                }
                else
                {
                    Icon("tiles/" + TileIconKey(t), r.x + 8, r.y + 8);
                    if (t.type == BoardTileType.Boss && !t.cleared) Frame(r, new Color(1f, 0.3f, 0.3f, 0.5f + 0.4f * pulse));
                }
                if (next)
                {
                    Frame(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), new Color(1f, 0.86f, 0.3f, 0.5f + 0.5f * pulse), 2);
                    if (Event.current.type == EventType.MouseUp && r.Contains(PixelUI.Mouse)) { Event.current.Use(); StepToNeighborTile(t.id); }
                }
                if (Hover(r) && (t.revealed || isCur)) hovered = t;
            }

            // hero token hops between tiles (ease-out arc)
            if (cur != null)
            {
                Vector2 c = C(cur);
                float hop = Mathf.Clamp01((Time.time - _tokenHopStart) / 0.22f);
                float lift = Mathf.Sin(hop * Mathf.PI) * 8f;
                string anim = stepsRemaining > 0 || hop < 1f ? "walk" : "idle";
                Strip(Mathf.Round(c.x - 24), Mathf.Round(c.y - 44 - lift), $"anim/hero_{anim}", "hero_" + anim, Time.time);
            }

            if (hovered != null) Tooltip($"{{#ffd86a}}{TileName(hovered.type)}{{/}}{(hovered.cleared ? " (selesai)" : "")}\n{TileDesc(hovered.type)}");

            DrawBoardBottom(cur);
        }

        private void DrawBoardBottom(BoardTileNode cur)
        {
            Rect p = new Rect(0, 292, W, 68);
            Panel(p);
            // dice: random faces while it rolls
            Rect die = new Rect(10, 300, 52, 52);
            Panel(die, "dark");
            int face = DiceRolling ? 1 + Mathf.FloorToInt(Time.time * 18f) % 6 : Mathf.Clamp(diceValue, 1, 6);
            float wob = DiceRolling ? Mathf.Sin(Time.time * 40f) * 2f : 0f;
            Image(new Rect(die.x + 2 + wob, die.y + 2, 48, 48), T($"dice/die_{face}"));

            if (stepsRemaining == 0)
            {
                if (Button(new Rect(70, 302, 132, 20), "Lempar Dadu", "icons/cmd_spell", !DiceRolling, true, "gold", "Spasi")) RollExplorationDice();
            }
            else
            {
                Text(74, 306, $"Sisa langkah: {stepsRemaining}", Gold);
                if (Button(new Rect(70, 318, 132, 18), "Berhenti di sini", null, true, false, "normal", "Enter"))
                {
                    stepsRemaining = 0;
                    ResolveBoardTileEvent(cur);
                }
            }
            var stage = GrimoireDatabase.Stages[currentAct];
            if (stepsRemaining == 0 && Button(new Rect(70, 326, 132, 18), "Tantang Boss", "icons/skull", !DiceRolling, false, "danger"))
                StartBattleEncounter(stage.bossId, true, board.GetTile(board.bossId));

            Rect msg = new Rect(212, 300, 300, 54);
            TextWrap(new Rect(msg.x, msg.y, msg.width, msg.height), boardBannerMessage, Ink, 1, true, 4);

            Panel(new Rect(520, 298, 114, 56), "dark");
            Text(526, 304, "Petak ini:", Dim);
            if (cur != null)
            {
                Icon("tiles/" + TileIconKey(cur), 526, 316);
                TextWrap(new Rect(546, 318, 84, 30), TileName(cur.type), Ink);
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
            int actBg = Mathf.Clamp(activeMonster.act, 1, 3);
            Image(new Rect(sh.x - 4, sh.y - 4, W + 8, H + 8), T($"bg/bg_s{actBg}"));
            Shade(new Rect(0, 0, W, 244), 0.25f);
            // floor strip so the actors stand on something
            Fill(new Rect(0, 226, W, 18), new Color(0.03f, 0.03f, 0.06f, 0.45f));
            Fill(new Rect(0, 226, W, 1), new Color(1f, 1f, 1f, 0.06f));

            // ---- hero (2x) ----
            int hf = _heroAnim.CurrentFrame();
            var heroTex = T($"anim/hero_{_heroAnim.anim}");
            _heroRect = new Rect(34 + sh.x, 236 - 96 + sh.y, 96, 96);
            Frame(_heroRect, heroTex, hf);
            if (HasP("poison") && Mathf.FloorToInt(Time.time * 2f) % 2 == 0)
                Frame(new Rect(_heroRect.x + 16, _heroRect.y + 8, 64, 64), T("vfx/poison"), Mathf.FloorToInt(Time.time * 8f) % 4, new Color(1, 1, 1, 0.7f));

            // ---- monster (2x; bosses 64 px frames => 128 px) ----
            int mf = _monAnim.CurrentFrame();
            var monTex = T($"anim/{activeMonster.id}_{_monAnim.anim}");
            float ms = (monTex != null ? monTex.height : 48) * 2;
            _monRect = new Rect(606 - ms + sh.x, 236 - ms + sh.y, ms, ms);
            Frame(_monRect, monTex, mf);

            DrawMonsterPlate();
            DrawConditionPanel();
            DrawBattleTopButtons();
            DrawBattleFx();
            DrawBattleHud();

            switch (battlePhase)
            {
                case BattlePhase.AlgorithmicChallenge: DrawQuestionPopup(); break;
                case BattlePhase.TurnResolution: DrawResolutionPopup(); break;
                case BattlePhase.MercyIntervention: DrawMercyPopup(); break;
                case BattlePhase.Analyze: DrawAnalyzePopup(); break;
                case BattlePhase.BattleWon: DrawWonPopup(); break;
            }
        }

        private void DrawMonsterPlate()
        {
            bool dead = monsterHp <= 0 && _monAnim.anim == "death";
            if (dead) return;
            float cx = _monRect.center.x;
            float y = Mathf.Max(30, _monRect.y - 8);
            string name = activeMonster.name;
            Color nc = isBossEncounter ? new Color(1f, 0.55f, 0.45f) : Ink;
            TextCenter(new Rect(cx - 90, y - 22, 180, 10), name, nc);
            Rect hb = new Rect(Mathf.Round(cx - 45), y - 10, 90, 6);
            float ratio = Mathf.Clamp01((float)monsterHp / Mathf.Max(1, monsterMaxHp));
            _monHpShown = Mathf.MoveTowards(_monHpShown, ratio, Time.deltaTime * 0.6f);
            Bar(hb, ratio, new Color(0.85f, 0.2f, 0.25f), _monHpShown);
            if (isBossEncounter)
            {
                int phases = activeMonster.id == "entropy_chaos_king" ? 3 : 2;
                for (int i = 0; i < phases; i++)
                    Fill(new Rect(hb.xMax + 3 + i * 5, hb.y + 1, 3, 4), i < bossPhase ? new Color(1f, 0.45f, 0.25f) : new Color(0.25f, 0.2f, 0.3f));
            }
            // monster statuses below the bar
            float sx = cx - (MonsterStatuses().Count() * 14) * 0.5f;
            foreach (var kv in MonsterStatuses())
            {
                Rect ir = new Rect(sx, hb.yMax + 2, 12, 12);
                Icon($"status/{kv.Key}", ir.x, ir.y);
                if (Hover(ir)) { var info = GrimoireStatus.Get(kv.Key); if (info != null) Tooltip($"{{#ffd86a}}{info.name}{{/}}\n{info.effect}"); }
                sx += 14;
            }
        }

        private void DrawConditionPanel()
        {
            var list = PlayerStatuses().ToList();
            float h = 22 + Mathf.Max(1, list.Count) * 13;
            Rect p = new Rect(4, 4, 150, h);
            Panel(p);
            Text(p.x + 7, p.y + 6, "KONDISI", Gold);
            TextRight(p.xMax - 7, p.y + 6, "Panduan [G]", Dim);
            if (list.Count == 0) { Text(p.x + 7, p.y + 19, "Tidak ada efek", Dim); return; }
            for (int i = 0; i < list.Count; i++)
            {
                var info = GrimoireStatus.Get(list[i].Key);
                if (info == null) continue;
                Rect row = new Rect(p.x + 6, p.y + 17 + i * 13, p.width - 12, 12);
                Icon($"status/{info.id}", row.x, row.y);
                Text(row.x + 15, row.y + 2, info.name, info.buff ? Green : Ink);
                string right = info.stacks ? $"{list[i].Value}/3" : (list[i].Value > 0 && list[i].Value < 90 ? $"{list[i].Value}g" : "");
                TextRight(row.xMax, row.y + 2, right, Dim);
                if (Hover(row)) Tooltip($"{{#ffd86a}}{info.name}{{/}} - {info.effect}\n{{#8a8ea8}}Sumber: {info.source}\nObat: {info.cure}{{/}}");
            }
        }

        private void DrawBattleTopButtons()
        {
            if (Button(new Rect(W - 186, 4, 58, 16), "Panduan", null, true, false, align: 1)) ToggleModal(ModalOverlay.Guide);
            if (Button(new Rect(W - 124, 4, 54, 16), "Codex", null, true, false, align: 1)) ToggleModal(ModalOverlay.CodexBestiary);
            if (Button(new Rect(W - 66, 4, 62, 16), "Opsi", null, true, false, align: 1)) ToggleModal(ModalOverlay.Settings);
            Text(W - 186, 24, $"{GrimoireStatus.DifficultyName(difficulty)}  -  DDA {(ddaEnabled ? ddaTier : "off")}", Dim);
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
                int f = AnimLibrary.FrameAt(key, el, false);
                float s = Mathf.Round(v.size / 64f) * 64f;
                if (s < 64) s = 64;
                Frame(new Rect(Mathf.Round(v.center.x - s * 0.5f), Mathf.Round(v.center.y - s * 0.5f), s, s), T("vfx/" + v.name), f, v.tint);
            }
            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                var ft = _floaters[i];
                float t = (Time.time - ft.start) / ft.life;
                if (t >= 1f) { _floaters.RemoveAt(i); continue; }
                float rise = GuiSprite.EaseOutCubic(t) * 26f;
                float pop = t < 0.12f ? 1f + (0.12f - t) * 3f : 1f;   // tiny pop on entry
                float a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                int sc = Mathf.Max(1, ft.fontSize);
                var c = new Color(ft.color.r, ft.color.g, ft.color.b, a);
                float w = Measure(ft.text, sc);
                Text(Mathf.Round(ft.pos.x - w * 0.5f), Mathf.Round(ft.pos.y - rise - (pop - 1f) * 6f), ft.text, c, sc);
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
            Rect left = new Rect(0, 244, 186, 116), mid = new Rect(188, 244, 226, 116), right = new Rect(416, 244, 224, 116);
            Panel(left); Panel(mid); Panel(right);

            // left: portrait + vitals
            Rect port = new Rect(left.x + 8, left.y + 8, 36, 36);
            NineSlice(port, T("ui2/slot_normal"), 4);
            var it = T("anim/hero_idle");
            if (it != null && IsRepaint)
            {
                int n = it.width / it.height;
                int f = AnimLibrary.FrameAt("hero_idle", Time.time, true);
                // crop the helm and shoulders out of the 48x48 frame
                GUI.DrawTextureWithTexCoords(new Rect(port.x + 2, port.y + 2, 32, 32), it, new Rect((f + 8f / 48f) / n, 16f / 48f, (32f / 48f) / n, 32f / 48f), true);
            }
            Text(left.x + 50, left.y + 10, "Aegis", Gold);
            Text(left.x + 50, left.y + 22, $"Lv.{playerLevel}  EXP {playerExp}/{expToNext}", Dim);
            Text(left.x + 50, left.y + 33, $"ATK {TotalAtk}  DEF {TotalDef}  INT {TotalInt}", Ink);

            float hr = (float)hp / maxHp;
            _heroHpShown = Mathf.MoveTowards(_heroHpShown, hr, Time.deltaTime * 0.6f);
            Icon("icons/hp", left.x + 8, left.y + 50);
            Bar(new Rect(left.x + 21, left.y + 51, 104, 8), hr, hr < 0.2f ? new Color(1f, 0.35f, 0.2f) : new Color(0.85f, 0.2f, 0.25f), _heroHpShown);
            Text(left.x + 129, left.y + 51, $"{hp}/{maxHp}", Ink);
            Icon("icons/mp", left.x + 8, left.y + 63);
            Bar(new Rect(left.x + 21, left.y + 64, 104, 8), (float)mp / maxMp, new Color(0.3f, 0.5f, 0.95f));
            Text(left.x + 129, left.y + 64, $"{mp}/{maxMp}", Ink);
            Icon("icons/focus", left.x + 8, left.y + 76);
            Bar(new Rect(left.x + 21, left.y + 77, 104, 8), focus / 100f, focus >= 100 ? Color.Lerp(Gold, Color.white, Mathf.PingPong(Time.time * 2f, 0.6f)) : new Color(0.95f, 0.75f, 0.2f));
            Text(left.x + 129, left.y + 77, focus >= 100 ? "LIMIT" : $"{focus}%", focus >= 100 ? Gold : Ink);
            Icon("icons/fragment", left.x + 8, left.y + 92); Text(left.x + 21, left.y + 93, $"{logicFragments}", Cyan);
            Icon("icons/gold", left.x + 46, left.y + 92); Text(left.x + 59, left.y + 93, $"{gold}", Gold);
            if (panicStacks > 0) { Icon("status/panic", left.x + 96, left.y + 91); Text(left.x + 110, left.y + 93, $"Panik {panicStacks}/3", Red); }

            DrawCommandPanel(mid, out string hint);

            // right: log + contextual hint (LIM style)
            Text(right.x + 8, right.y + 7, "CATATAN PERTARUNGAN", Dim);
            float y = right.y + 19;
            foreach (var line in _combatLog)
            {
                float hgt = WrapHeight(line, right.width - 16);
                if (y + hgt > right.yMax - 24) break;
                TextWrap(new Rect(right.x + 8, y, right.width - 16, hgt), line, y == right.y + 19 ? Ink : Dim);
                y += hgt;
            }
            Fill(new Rect(right.x + 6, right.yMax - 22, right.width - 12, 1), new Color(1, 1, 1, 0.08f));
            TextWrap(new Rect(right.x + 8, right.yMax - 19, right.width - 16, 16), hint, Gold, 1, true, 1);
        }

        private static readonly string[] CmdNames = { "Serang", "Grimoire", "Item", "Analisis", "Kabur" };
        private static readonly string[] CmdIcons = { "icons/cmd_attack", "icons/cmd_spell", "icons/cmd_item", "icons/cmd_analyze", "icons/cmd_flee" };

        private void DrawCommandPanel(Rect mid, out string hint)
        {
            hint = "";
            switch (battlePhase)
            {
                case BattlePhase.PlayerCommand:
                {
                    Text(mid.x + 8, mid.y + 7, "Apa yang akan Aegis lakukan?", Dim);
                    string[] hints =
                    {
                        $"Tebasan pedang: ATK - DEF musuh.{(HasM("armored") ? " Zirah: 30%!" : "")}",
                        HasP("silence") ? "Bisu: mantra biasa terkunci." : "Mantra algoritma: jawab soal untuk merapal.",
                        ItemsLocked ? "Terkubur: tas terkunci." : "Pakai ramuan (memakai giliran).",
                        "Lihat topik, pola serangan & kelemahan.",
                        isBossEncounter ? "Tidak bisa kabur dari boss." : $"Peluang kabur {Mathf.RoundToInt(FleeChance * 100)}%."
                    };
                    for (int i = 0; i < 5; i++)
                    {
                        Rect r = new Rect(mid.x + 10, mid.y + 19 + i * 18, mid.width - 20, 16);
                        bool en = !(i == 2 && ItemsLocked) && !(i == 4 && isBossEncounter);
                        if (Hover(r)) _cmdSel = i;
                        if (Row(r, CmdNames[i], _cmdSel == i, CmdIcons[i], en, $"{i + 1}")) RunCommand(i);
                    }
                    hint = hints[Mathf.Clamp(_cmdSel, 0, 4)];
                    break;
                }
                case BattlePhase.SpellSelect:
                {
                    Text(mid.x + 8, mid.y + 7, "Mantra Grimoire", Purple);
                    TextRight(mid.xMax - 8, mid.y + 7, $"MP {mp}", Cyan);
                    var spells = GrimoireDatabase.Spells;
                    int first = Mathf.Clamp(_cmdSel - 3, 0, Mathf.Max(0, spells.Count - 5));
                    for (int k = 0; k < 5 && first + k < spells.Count; k++)
                    {
                        int i = first + k;
                        var sp = spells[i];
                        int cost = GetEffectiveMpCost(sp);
                        bool can = !SpellLocked(sp) && mp >= cost && focus >= sp.focusCost;
                        string right = sp.focusCost > 0 ? $"{sp.focusCost} FOC" : $"{cost} MP";
                        Rect r = new Rect(mid.x + 10, mid.y + 19 + k * 16, mid.width - 20, 15);
                        if (Hover(r)) _cmdSel = i;
                        if (Row(r, sp.name.Replace(" [LIMIT]", ""), _cmdSel == i, null, can, right, sp.focusCost > 0 ? Gold : Cyan)) BeginSpellCast(sp);
                    }
                    if (Row(new Rect(mid.x + 10, mid.y + 99, 60, 13), "< Kembali", false)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 1; }
                    var sel = spells[Mathf.Clamp(_cmdSel, 0, spells.Count - 1)];
                    bool weak = monstersAnalyzed.Contains(activeMonster.id) && activeMonster.tpPool.Contains(sel.tpCode);
                    hint = $"{sel.tpCode}: {sel.desc}{(weak ? " {#8ae05a}KELEMAHAN x1.5{/}" : "")}";
                    if (Hover(new Rect(mid.x, mid.y, mid.width, mid.height))) Tooltip($"{{#d0a0ff}}{sel.name}{{/}} ({sel.tpCode})\n{sel.desc}");
                    break;
                }
                case BattlePhase.ItemSelect:
                {
                    Text(mid.x + 8, mid.y + 7, "Tas", Green);
                    var items = BattleItems();
                    if (items.Count == 0) Text(mid.x + 10, mid.y + 24, "Tas kosong.", Dim);
                    for (int i = 0; i < items.Count && i < 5; i++)
                    {
                        var item = GrimoireDatabase.Items[items[i]];
                        Rect r = new Rect(mid.x + 10, mid.y + 19 + i * 16, mid.width - 20, 15);
                        if (Hover(r)) _cmdSel = i;
                        if (Row(r, item.name, _cmdSel == i, null, true, $"x{consumables[items[i]]}")) UseConsumableItem(items[i]);
                    }
                    if (Row(new Rect(mid.x + 10, mid.y + 99, 60, 13), "< Kembali", false)) { battlePhase = BattlePhase.PlayerCommand; _cmdSel = 2; }
                    if (items.Count > 0) hint = GrimoireDatabase.Items[items[Mathf.Clamp(_cmdSel, 0, items.Count - 1)]].desc;
                    break;
                }
                case BattlePhase.AlgorithmicChallenge:
                {
                    Text(mid.x + 8, mid.y + 7, isChallengeForSpell ? $"Merapal {pendingSpell?.name}" : $"{activeMonster.name} menyerang!", isChallengeForSpell ? Purple : Red);
                    Text(mid.x + 10, mid.y + 22, "Jawab: [1]-[4] atau klik pilihan", Ink);
                    Text(mid.x + 10, mid.y + 34, NoTimer ? "Mode Mudah: tanpa batas waktu." : "Cepat & benar = PERFECT GUARD.", Dim);
                    bool canHint = !hintUsedOnQuestion && mp >= HintCost && !OptionsHidden;
                    if (Button(new Rect(mid.x + 10, mid.y + 52, mid.width - 20, 18), $"Scholar's Hint (-{HintCost} MP)", "icons/cmd_hint", canHint, false, "normal", "H"))
                        UseScholarHint();
                    if (_volleyTotal > 1 && !isChallengeForSpell)
                        Text(mid.x + 10, mid.y + 78, $"Serangan beruntun: {_volleyTotal - _volleyLeft}/{_volleyTotal}", Gold);
                    hint = $"{currentQuestion?.tp} - {currentQuestion?.algorithmName}";
                    break;
                }
                case BattlePhase.Animating:
                    Text(mid.x + 8, mid.y + 7, "...", Dim);
                    break;
                default:
                    Text(mid.x + 8, mid.y + 7, "Tekan [Spasi] untuk lanjut", Dim);
                    break;
            }
        }

        // ---------------------------------------------------------------- question pop-up
        private void DrawThemedArray(float cx, float baseY, int[] arr, float maxH, Color valCol, Color idxCol)
        {
            int n = arr.Length;
            int act = Mathf.Clamp(activeMonster != null ? activeMonster.act : currentAct, 1, 3);
            string theme = act == 1 ? "book" : (act == 2 ? "candle" : "crystal");
            Texture2D top = T($"array/{theme}_top"), body = T($"array/{theme}_body"), band = T($"array/{theme}_band"), bas = T($"array/{theme}_base");
            const float colW = 16, gap = 10;
            float totalW = n * colW + (n - 1) * gap;
            float x0 = Mathf.Round(cx - totalW * 0.5f);
            int maxVal = Mathf.Max(1, arr.Max());
            Color[] spines = { new Color(0.85f, 0.45f, 0.42f), new Color(0.45f, 0.62f, 0.9f), new Color(0.55f, 0.8f, 0.55f), new Color(0.9f, 0.78f, 0.45f), new Color(0.72f, 0.55f, 0.88f) };
            bool blink = Mathf.FloorToInt(Time.time * 4f) % 2 == 0;
            for (int i = 0; i < n; i++)
            {
                float x = x0 + i * (colW + gap);
                var q = currentQuestion;
                bool hl = q != null && q.highlightIndices != null && q.highlightIndices.Contains(i);
                bool isMid = q != null && q.midIdx == i, isPivot = q != null && q.pivotIdx == i;
                bool isLow = q != null && q.lowIdx == i, isHigh = q != null && q.highIdx == i;
                bool fog = HasM("fog") && !isMid && (i == 0 || i == n - 1);
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

        private Rect PopupRect(float h) => new Rect(150, Mathf.Round(Mathf.Max(6, 122 - h * 0.5f)), 340, h);

        private void DrawQuestionPopup()
        {
            var q = currentQuestion;
            if (q == null) return;
            float pop = GuiSprite.EaseOutBack(Mathf.Clamp01((Time.time - _popupStart) / 0.25f));
            bool hasArr = q.arrayData != null && q.arrayData.Length > 0;
            float promptH = Mathf.Min(WrapHeight(q.prompt, 316), 60);
            float arrBlock = hasArr ? 70 : 4;
            float need = 10 + 34 + arrBlock + promptH + 6 + 55 + 10;
            if (need > 234 && hasArr) { arrBlock = Mathf.Max(46, arrBlock - (need - 234)); need = 234; }
            float popH = Mathf.Min(234, need);
            Rect full = PopupRect(popH);
            Rect p = new Rect(full.center.x - full.width * 0.5f * pop, full.center.y - full.height * 0.5f * pop, full.width * pop, full.height * pop);
            Shade(new Rect(0, 0, W, 244), 0.35f * pop);
            Panel(p, "parchment");
            if (pop < 0.98f) return;

            float x = p.x + 12, w = p.width - 24, y = p.y + 10;
            Text(x, y, $"{q.tp}  -  {q.algorithmName}", ParchInk, 1, false);
            TextRight(x + w, y, string.IsNullOrEmpty(q.formatName) ? q.formatCode : q.formatName, ParchDim, 1, false);
            y += 13;

            // timer (GDD §17: blue -> yellow -> red); Easy has no clock
            Rect tb = new Rect(x, y, w, 9);
            if (NoTimer)
            {
                Rect eb = new Rect(tb.x, tb.y - 2, tb.width, 14);
                NineSlice(eb, T("ui2/bar_frame"), 2);
                Fill(new Rect(eb.x + 1, eb.y + 1, eb.width - 2, eb.height - 2), new Color(0.16f, 0.36f, 0.18f));
                TextCenter(eb, "MODE MUDAH  -  TANPA BATAS WAKTU, PIKIRKAN DENGAN TENANG", new Color(0.8f, 1f, 0.7f), 1, true);
            }
            else
            {
                float rem = Mathf.Clamp01(questionTimer / Mathf.Max(0.1f, questionMaxTime));
                Color bc = rem >= 0.6f ? new Color(0.36f, 0.62f, 1f) : (rem > 0.3f ? new Color(1f, 0.8f, 0.2f) : new Color(0.95f, 0.28f, 0.25f));
                Bar(tb, rem, bc);
                float pz = Mathf.Round((tb.width - 2) * (1f - PerfectGuardRemaining));
                Fill(new Rect(tb.x + 1 + (tb.width - 2) * PerfectGuardRemaining, tb.y - 2, 1, 2), new Color(0.6f, 0.4f, 0.05f));
                string lbl = Reading ? $"MASA BACA {(_readingUntil - Time.time):F1}" : $"{questionTimer:F1} dtk";
                TextRight(x + w, y + 11, lbl, ParchDim, 1, false);
                Text(x, y + 11, rem >= 0.6f && !Reading ? "Zona PERFECT GUARD" : (Reading ? "Baca soal dulu..." : "Zona GUARD"), ParchDim, 1, false);
            }
            y += 22;

            // themed array (values above, [index] below, pointer tag under that)
            if (hasArr)
            {
                DrawThemedArray(p.center.x, y + arrBlock - 22, q.arrayData, arrBlock - 36, ParchInk, ParchDim);
                y += arrBlock;
            }
            else y += 4;

            // prompt (Panic 1+: trembles)
            Vector2 j = panicStacks >= 1 && !accessibilityNoShake ? new Vector2(Mathf.Round(Mathf.Sin(Time.time * 37f) * panicStacks * 0.6f), Mathf.Round(Mathf.Cos(Time.time * 29f) * panicStacks * 0.4f)) : Vector2.zero;
            float ph = TextWrap(new Rect(x + j.x, y + j.y, w, 60), q.prompt, ParchInk, 1, false, 5);
            y = Mathf.Min(Mathf.Max(y + ph + 6, p.yMax - 65), p.yMax - 65);

            // 2x2 options
            string[] keys = { "1", "2", "3", "4" };
            float ow = (w - 6) * 0.5f, oh = 26;
            for (int slot = 0; slot < 4; slot++)
            {
                int i = _optionOrder[slot];
                Rect r = new Rect(x + (slot % 2) * (ow + 6), y + (slot / 2) * (oh + 3), ow, oh);
                bool elim = eliminatedOptions.Contains(i);
                bool hover = !elim && !OptionsHidden && Hover(r);
                NineSlice(r, T(elim ? "ui2/btn_disabled" : hover ? "ui2/btn_hover" : "ui2/btn_normal"), 4);
                Text(r.x + 5, r.y + 2, keys[slot], hover ? Gold : Dim);
                string label = OptionsHidden ? "? ? ?" : elim ? "(dieliminasi Hint)" :
                               obscuredOptionIdx == i ? "{#8a8ea8}~ halaman robek ~{/}" : q.options[i];
                int nLines = PixelUI.Font != null ? PixelUI.Font.Wrap(label, Mathf.FloorToInt(r.width - 18)).Count : 1;
                float ly = nLines <= 1 ? r.y + Mathf.Round((oh - 9) * 0.5f) - 1 : r.y + 2;
                TextWrap(new Rect(r.x + 14, ly, r.width - 18, oh - 4), label, elim ? new Color(0.4f, 0.4f, 0.46f) : (hover ? Gold : Ink), 1, true, 2);
                if (hover && Event.current.type == EventType.MouseUp && Event.current.button == 0 && r.Contains(PixelUI.Mouse)) { Event.current.Use(); SubmitSlot(slot); }
            }
            if (OptionsHidden) TextCenter(new Rect(p.x, p.yMax - 74, p.width, 10), "Buta! Pilihan muncul sebentar lagi...", new Color(0.55f, 0.1f, 0.1f), 1, false);
        }

        private void DrawResolutionPopup()
        {
            float h = 196;
            Rect p = PopupRect(h);
            float pop = GuiSprite.EaseOutBack(Mathf.Clamp01((Time.time - _popupStart - 0.05f) / 0.2f));
            Panel(p, "parchment");
            string icon = lastResolutionGrade == "PERFECT GUARD" ? "status/ward" : lastResolutionGrade == "GUARD" ? "icons/def" : lastResolutionGrade == "BREAK" ? "status/panic" : "icons/cmd_spell";
            Icon(icon, p.x + 12, p.y + 12, 2);
            Text(p.x + 40, p.y + 14, lastResolutionTitle, lastResolutionColor, 1, false);
            Text(p.x + 40, p.y + 26, isChallengeForSpell ? "Hasil mantra" : "Hasil Active Defend", ParchDim, 1, false);
            TextWrap(new Rect(p.x + 12, p.y + 42, p.width - 24, h - 70), lastResolutionDetail, ParchInk, 1, false, 11);
            string next = isChallengeForSpell ? "Giliran musuh" : (_volleyLeft > 0 ? "Panah berikutnya" : "Lanjut");
            if (Button(new Rect(p.xMax - 160, p.yMax - 26, 148, 18), next, null, true, true, "gold", "Spasi")) ContinueFromResolution();
        }

        private void DrawMercyPopup()
        {
            Rect p = PopupRect(176);
            Panel(p, "teal");
            Strip(p.x + 8, p.y + 10, "npc/shade", "npc_shade_idle", Time.time, 1, new Color(1, 1, 1, 0.8f + 0.15f * Mathf.Sin(Time.time * 2f)));
            Text(p.x + 80, p.y + 12, "BAYANG-BAYANG ORDO", new Color(0.5f, 0.95f, 0.85f));
            Text(p.x + 80, p.y + 24, "Kilas Codex - tanpa penalti", Dim);
            TextWrap(new Rect(p.x + 80, p.y + 38, p.width - 92, 40), "\"Jangan menyerah, Ksatria. Mari kita ulangi langkah-langkahnya.\" (+25% HP, +30% MP)", Ink);
            if (mercyCodexEntry != null)
            {
                Text(p.x + 12, p.y + 82, $"{mercyCodexEntry.tp}: {mercyCodexEntry.title}", Gold);
                float hh = TextWrap(new Rect(p.x + 12, p.y + 94, p.width - 24, 40), mercyCodexEntry.summary, Ink, 1, true, 3);
                Panel(new Rect(p.x + 10, p.y + 96 + hh, p.width - 20, 34), "dark");
                TextWrap(new Rect(p.x + 16, p.y + 100 + hh, p.width - 32, 28), mercyCodexEntry.pseudocode.Replace("\n", "  |  "), Cyan, 1, false, 2);
            }
            if (Button(new Rect(p.xMax - 128, p.yMax - 24, 116, 18), "Paham!", null, true, true, "gold", "Spasi")) CloseMercy();
        }

        private void DrawAnalyzePopup()
        {
            var m = activeMonster;
            Rect p = PopupRect(196);
            Panel(p);
            Strip(p.x + 8, p.y + 10, $"anim/{m.id}_idle", m.id + "_idle", Time.time, 1);
            Text(p.x + 76, p.y + 10, m.name, Gold);
            Text(p.x + 76, p.y + 22, $"Act {m.act} - {m.role}   HP {monsterHp}/{monsterMaxHp}   ATK {monsterAtk}   DEF {m.def}", Ink);
            bool hidden = difficulty == Difficulty.Hard;
            Text(p.x + 76, p.y + 36, "Topik soal: " + (hidden ? "??? (mode Sulit)" : string.Join(", ", m.tpPool)), Cyan);
            string st = GrimoireStatus.OnBreak(m.id, bossPhase);
            var info = st != null ? GrimoireStatus.Get(st) : null;
            if (info != null) { Icon($"status/{info.id}", p.x + 76, p.y + 48); Text(p.x + 91, p.y + 50, $"Jika Break: {info.name}", Red); }
            int vol = GrimoireStatus.Volley(m.id);
            if (vol > 1) Text(p.x + 76, p.y + 62, $"Serangan beruntun: {vol} soal per giliran", Gold);
            float y = p.y + 78;
            y += TextWrap(new Rect(p.x + 12, y, p.width - 24, 30), "Pola: " + m.mechanicDesc, Ink, 1, true, 3) + 4;
            var weak = GrimoireDatabase.Spells.Where(s => m.tpPool.Contains(s.tpCode)).Select(s => s.name).ToList();
            y += TextWrap(new Rect(p.x + 12, y, p.width - 24, 30), "{#8ae05a}Kelemahan (mantra x1.5):{/} " + (weak.Count > 0 ? string.Join(", ", weak) : "-"), Ink, 1, true, 2) + 4;
            TextWrap(new Rect(p.x + 12, y, p.width - 24, 40), "{#8a8ea8}\"" + m.lore + "\"{/}", Dim, 1, true, 3);
            if (Button(new Rect(p.xMax - 178, p.yMax - 24, 166, 18), "Tutup (giliran musuh)", null, true, true, "gold", "Spasi")) CloseAnalyze();
        }

        private void DrawWonPopup()
        {
            Rect p = PopupRect(150);
            float pop = GuiSprite.EaseOutBack(Mathf.Clamp01((Time.time - _popupStart) / 0.3f));
            Panel(p, "gold");
            TextCenter(new Rect(p.x, p.y + 10, p.width, 20), "MENANG!", Gold, 2);
            TextCenter(new Rect(p.x, p.y + 32, p.width, 10), $"{activeMonster.name} tersegel kembali ke Katalog Agung.", Ink);
            float y = p.y + 50;
            Icon("icons/exp", p.x + 90, y); Text(p.x + 104, y + 1, $"EXP +{winExp}{(winFlawless ? "  (Flawless +20%)" : "")}", Green); y += 14;
            Icon("icons/gold", p.x + 90, y); Text(p.x + 104, y + 1, $"Gold +{winGold}", Gold); y += 14;
            Icon("icons/fragment", p.x + 90, y); Text(p.x + 104, y + 1, $"Logic Fragment {(winFrag > 0 ? "+1 (Flawless)" : "+0")}  -  total {logicFragments}", Cyan); y += 14;
            if (winLevelUp) TextCenter(new Rect(p.x, y + 2, p.width, 12), $"LEVEL UP! Sekarang Lv.{playerLevel}", Gold);
            if (Button(new Rect(p.center.x - 58, p.yMax - 26, 116, 18), "Lanjut", null, true, true, "gold", "Spasi")) ContinueAfterVictory();
        }

        // =====================================================================
        // ENDING
        // =====================================================================
        private void DrawEnding()
        {
            Image(new Rect(0, 0, W, H), T("bg/bg_title"));
            Shade(new Rect(0, 0, W, H), 0.55f);
            TextCenter(new Rect(0, 40, W, 24), "KATALOG AGUNG TELAH TERURUT", Gold, 2);
            Strip(272, 76, "anim/hero_victory", "hero_victory", Time.time, 2);
            string txt = "Entropy tidak bisa dihancurkan - hanya bisa diurutkan. Dengan Linear & Binary Search, Bubble, Selection, Insertion, Merge dan Quick Sort, Aegis menyusun kembali Katalog Agung Algoria. Matahari kembali terbit pada urutannya.";
            var lines = PixelUI.Font != null ? PixelUI.Font.Wrap(txt, 460) : new List<string>();
            for (int i = 0; i < lines.Count; i++) TextCenter(new Rect(0, 184 + i * 12, W, 12), lines[i], Ink);
            int total = telemetryLog.Count, cor = telemetryLog.Count(r => r.isCorrect);
            TextCenter(new Rect(0, 240, W, 12), $"Soal dijawab: {total}  -  Akurasi: {(total > 0 ? cor * 100 / total : 0)}%  -  Logic Fragment: {logicFragments}", Cyan);
            if (Button(new Rect(180, 270, 130, 20), "Laporan Guru", "icons/cmd_analyze", true, false, "normal", "T")) ToggleModal(ModalOverlay.TeacherAnalytics);
            if (Button(new Rect(330, 270, 130, 20), "Ke Layar Judul", null, true, true, "gold"))
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
                case "panic": panicStacks = a; break;
            }
        }
    }
}
