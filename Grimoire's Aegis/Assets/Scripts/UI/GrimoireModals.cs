using System.Collections.Generic;
using System.IO;
using System.Linq;
using GrimoireAegis.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using static GrimoireAegis.UI.PixelUI;

namespace GrimoireAegis.UI
{
    /// <summary>Overlay windows: Guide book, Codex & Bestiary, shop/bag, teacher report, settings, pause.</summary>
    public partial class GrimoireGameController
    {
        private static readonly Rect ModalRect = new Rect(16, 14, 608, 332);
        private int _bestiarySel, _codexSel;
        private float _bestiaryAnimStart;

        private void DrawModal()
        {
            Shade(new Rect(0, 0, W, H), 0.72f);
            switch (activeModal)
            {
                case ModalOverlay.Pause: DrawPause(); return;
                case ModalOverlay.Guide: DrawGuide(); break;
                case ModalOverlay.CodexBestiary: DrawCodexBestiary(); break;
                case ModalOverlay.ShopInventory: DrawShop(); break;
                case ModalOverlay.TeacherAnalytics: DrawTeacher(); break;
                case ModalOverlay.Settings: DrawSettings(); break;
            }
            if (Button(new Rect(ModalRect.xMax - 72, ModalRect.y + 6, 64, 16), "Tutup", null, true, false, "danger", "Esc"))
                activeModal = ModalOverlay.None;
        }

        private void ModalFrame(string title, string kind = "panel")
        {
            Panel(ModalRect, kind);
            Text(ModalRect.x + 10, ModalRect.y + 10, title, Gold);
            Fill(new Rect(ModalRect.x + 8, ModalRect.y + 24, ModalRect.width - 16, 1), new Color(1, 1, 1, 0.08f));
        }

        private void HandleModalKeys()
        {
            int n = 0;
            switch (activeModal)
            {
                case ModalOverlay.Guide: n = GuideChapters.Length; break;
                case ModalOverlay.CodexBestiary: n = _codexTab == 0 ? GrimoireDatabase.Codex.Count : GrimoireDatabase.MonsterOrder.Length; break;
                case ModalOverlay.Pause: n = PauseItems.Length; break;
            }
            if (n > 0)
            {
                if (Up) { _listSel = (_listSel + n - 1) % n; _modalScroll = 0; _bestiaryAnimStart = Time.time; }
                if (Down) { _listSel = (_listSel + 1) % n; _modalScroll = 0; _bestiaryAnimStart = Time.time; }
            }
            if (activeModal == ModalOverlay.CodexBestiary && (Left || Right)) { _codexTab = 1 - _codexTab; _listSel = 0; }
            if (activeModal == ModalOverlay.ShopInventory && (Left || Right)) { _shopTab = 1 - _shopTab; _modalScroll = 0; }
            if (activeModal == ModalOverlay.Guide) _guideChapter = _listSel;
            if (activeModal == ModalOverlay.Pause && Confirm) PauseAction(_listSel);
        }

        // =====================================================================
        // PAUSE
        // =====================================================================
        private static readonly string[] PauseItems = { "Lanjutkan", "Tas & Perlengkapan", "Codex & Bestiary", "Buku Panduan", "Pengaturan", "Laporan Guru", "Kembali ke Judul" };

        private void PauseAction(int i)
        {
            switch (i)
            {
                case 0: activeModal = ModalOverlay.None; break;
                case 1: activeModal = ModalOverlay.ShopInventory; _shopTab = 1; _modalScroll = 0; break;
                case 2: activeModal = ModalOverlay.CodexBestiary; _listSel = 0; break;
                case 3: activeModal = ModalOverlay.Guide; _listSel = 0; break;
                case 4: activeModal = ModalOverlay.Settings; break;
                case 5: activeModal = ModalOverlay.TeacherAnalytics; break;
                case 6:
                    activeModal = ModalOverlay.None;
                    _scheduled.Clear();
                    viewMode = GameViewMode.Title; _titleMode = 0;
                    GrimoireAudioManager.Instance?.PlayMusic("music_title");
                    break;
            }
        }

        private void DrawPause()
        {
            Rect p = new Rect(220, 110, 200, 30 + PauseItems.Length * 20);
            Panel(p);
            TextCenter(new Rect(p.x, p.y + 6, p.width, 12), "JEDA", Gold);
            for (int i = 0; i < PauseItems.Length; i++)
            {
                Rect r = new Rect(p.x + 10, p.y + 24 + i * 20, p.width - 20, 18);
                if (Hover(r)) _listSel = i;
                if (Button(r, PauseItems[i], null, true, _listSel == i, i == 0 ? "gold" : "normal", align: 1)) PauseAction(i);
            }
            if (viewMode == GameViewMode.Battle && battlePhase == BattlePhase.AlgorithmicChallenge && !NoTimer)
                TextCenter(new Rect(p.x, p.yMax + 4, p.width, 10), "Timer dijeda selama menu terbuka.", Dim);
        }

        // =====================================================================
        // GUIDE BOOK (Buku Panduan)
        // =====================================================================
        private static readonly string[] GuideChapters =
        {
            "Selamat Datang", "Kontrol", "Alur Permainan", "Peta Petualangan", "Pertarungan", "Menangkis Serangan",
            "Mode Kesulitan", "Status Effect", "Monster", "Mantra Grimoire", "Item & Perlengkapan", "Format Soal", "Untuk Guru"
        };

        /// <summary>A guide entry: optional icon (or animated sprite) + title + body.</summary>
        private struct GuideItem
        {
            public string icon, anim, title, body;
            public GuideItem(string title, string body, string icon = null, string anim = null) { this.title = title; this.body = body; this.icon = icon; this.anim = anim; }
        }

        private List<GuideItem> GuideContent(int ch)
        {
            var L = new List<GuideItem>();
            switch (ch)
            {
                case 0:
                    L.Add(new GuideItem("Grimoire's Aegis", "Raja Entropy menghancurkan Katalog Agung dan mengacak urutan dunia Algoria. Kamu adalah Aegis, ksatria cendekia yang menangkis kekacauan dengan logika.", null, "hero_idle"));
                    L.Add(new GuideItem("Tujuan", "Lewati 3 Act. Di tiap Act, kumpulkan 3 Kunci Indeks, buka gerbang, lalu kalahkan boss."));
                    L.Add(new GuideItem("Act I - " + ActName(1), ActTopic(1), "tiles/icon_enemy"));
                    L.Add(new GuideItem("Act II - " + ActName(2), ActTopic(2), "tiles/icon_elite"));
                    L.Add(new GuideItem("Act III - " + ActName(3), ActTopic(3), "tiles/icon_boss"));
                    L.Add(new GuideItem("Lyra", "Penjaga katalog terakhir. Ia muncul saat kamu butuh petunjuk.", null, "npc_lyra_idle"));
                    break;
                case 1:
                    L.Add(new GuideItem("Peta", "{#ffd86a}Spasi{/} lempar dadu. Di persimpangan pilih arah dengan {#ffd86a}panah{/} atau klik."));
                    L.Add(new GuideItem("Pertarungan", "{#ffd86a}1-4{/} pilih aksi. Saat soal muncul, {#ffd86a}1-4{/} pilih jawaban dan {#ffd86a}H{/} untuk petunjuk."));
                    L.Add(new GuideItem("Menu", "{#ffd86a}Esc{/} membuka menu: tas, Codex, panduan, pengaturan, laporan guru. {#ffd86a}G{/} langsung ke panduan."));
                    break;
                case 2:
                    L.Add(new GuideItem("Di peta", "Lempar dadu, jelajahi jalan, kalahkan monster yang menghadang, dan kumpulkan 3 Kunci Indeks."));
                    L.Add(new GuideItem("Saat bertarung", "Giliranmu: serang atau rapal mantra. Giliran monster: jawab soal untuk menangkis."));
                    L.Add(new GuideItem("Soal selalu baru", "Angka di setiap soal diacak ulang, jadi kamu perlu paham caranya, bukan menghafal jawabannya."));
                    break;
                case 3:
                    L.Add(new GuideItem("Kunci Indeks", "Gerbang boss butuh 3 kunci: dari Penjaga Kunci, Rak Terkunci, dan Peti Tersembunyi.", "icons/key"));
                    L.Add(new GuideItem("Gerbang Boss", "Selama kunci belum lengkap, jalan ke boss tertutup.", "world/gate_0"));
                    foreach (NodeType t in new[] { NodeType.Enemy, NodeType.Elite, NodeType.Puzzle, NodeType.KeyChest, NodeType.Treasure, NodeType.Rest, NodeType.Shrine, NodeType.Merchant, NodeType.Event, NodeType.Trap })
                    {
                        string icon = t == NodeType.Enemy ? "tiles/icon_enemy" : t == NodeType.Elite ? "tiles/icon_elite" : t == NodeType.Puzzle ? "tiles/icon_puzzle" :
                                      t == NodeType.KeyChest || t == NodeType.Treasure ? "tiles/icon_treasure" : t == NodeType.Rest ? "tiles/icon_rest" :
                                      t == NodeType.Shrine ? "tiles/icon_shrine" : t == NodeType.Merchant ? "tiles/icon_merchant" : t == NodeType.Event ? "tiles/icon_event" : "tiles/icon_trap";
                        L.Add(new GuideItem(AdventureMaps.NodeName(t), AdventureMaps.NodeDesc(t), icon));
                    }
                    L.Add(new GuideItem("Monster menghadang", "Monster di peta menghentikan langkahmu. Kalau kabur, Aegis mundur selangkah."));
                    break;
                case 4:
                    L.Add(new GuideItem("Serang", "Tebasan pedang. Mengisi Focus.", "icons/cmd_attack"));
                    L.Add(new GuideItem("Mantra", "Jawab soal topik mantra untuk merapalnya. Memakai MP. Mantra baru terbuka di setiap Act.", "icons/cmd_spell"));
                    L.Add(new GuideItem("Ramuan", "Pulihkan HP atau MP. Memakai satu giliran.", "icons/cmd_item"));
                    L.Add(new GuideItem("Kabur", "Mundur dari monster biasa. Tidak bisa dari penjaga kunci dan boss.", "icons/cmd_flee"));
                    L.Add(new GuideItem("Info monster", "Klik nama monster untuk melihat topik soal dan mantra yang ampuh.", "icons/cmd_analyze"));
                    L.Add(new GuideItem("Focus", "Saat bar Focus penuh, mantra ORDER RESTORED muncul: serangan terkuat dan menghapus semua efek buruk.", "icons/focus"));
                    break;
                case 5:
                    L.Add(new GuideItem("Cara kerjanya", "Monster bersiap menyerang, lalu soal muncul. Pilih jawabanmu, dan lihat apa yang terjadi: perisai menahan serangan, atau seranganmu tertembus. Setelah itu baru terlihat jawaban yang benar."));
                    L.Add(new GuideItem("Benar dan cepat", "Serangan ditangkis dan Aegis langsung membalas. +1 Logic Fragment.", "status/ward"));
                    L.Add(new GuideItem("Benar tapi lambat", "Perisai menahan serangan, tanpa damage.", "icons/def"));
                    L.Add(new GuideItem("Salah", "Kamu terkena damage, kadang juga efek buruk. Baca pembahasannya, soal berikutnya angkanya berbeda.", "status/panic"));
                    L.Add(new GuideItem("Petunjuk", $"Buang 1 jawaban salah dengan MP.", "icons/cmd_hint"));
                    L.Add(new GuideItem("Bayang-bayang Ordo", "Kalau salah 3 kali di topik yang sama atau HP habis, ia menolong dan menjelaskan ulang materinya.", null, "npc_shade_idle"));
                    break;
                case 6:
                    L.Add(new GuideItem("MUDAH", GrimoireStatus.DifficultyDesc(Difficulty.Easy), "icons/heart_full"));
                    L.Add(new GuideItem("SEDANG", GrimoireStatus.DifficultyDesc(Difficulty.Medium), "icons/timer"));
                    L.Add(new GuideItem("SULIT", GrimoireStatus.DifficultyDesc(Difficulty.Hard), "icons/skull"));
                    L.Add(new GuideItem("Ganti mode", "Buka Pengaturan [O] kapan saja. Di sana juga ada pengali timer (1.0x - 2.0x), DDA, dan efek layar."));
                    L.Add(new GuideItem("DDA", "Dynamic Difficulty: jika sering benar, array soal lebih panjang; jika sering salah, lebih pendek dan timer lebih lama. Guru bisa mematikannya."));
                    break;
                case 7:
                    L.Add(new GuideItem("Cara membaca", "Efek aktif muncul di panel KONDISI (kiri atas) beserta sisa giliran. Arahkan kursor untuk detail. Bingkai {#f05a50}merah{/} = debuff, {#7ff0d0}hijau{/} = buff."));
                    foreach (var s in GrimoireStatus.All.Values)
                        L.Add(new GuideItem(s.name, $"{s.effect}\n{{#8a8ea8}}Sumber: {s.source}. Obat: {s.cure}{{/}}", "status/" + s.id));
                    break;
                case 8:
                    foreach (string id in GrimoireDatabase.MonsterOrder)
                    {
                        var m = GrimoireDatabase.Monsters[id];
                        string st = GrimoireStatus.OnBreak(id, 1);
                        string fx = st != null ? GrimoireStatus.Get(st).name : (id == "hydra_of_division" ? "Regenerasi" : "-");
                        int vol = GrimoireStatus.Volley(id);
                        L.Add(new GuideItem($"{m.name}  {{#8a8ea8}}Act {m.act} - {m.role}{{/}}", $"TP: {string.Join(", ", m.tpPool)}. Jika Break: {fx}.{(vol > 1 ? $" Menyerang {vol}x beruntun." : "")}\n{m.mechanicDesc}", null, id + "_idle"));
                    }
                    break;
                case 9:
                    foreach (var sp in GrimoireDatabase.Spells)
                        L.Add(new GuideItem($"{sp.name}  {{#8a8ea8}}{(sp.focusCost > 0 ? sp.focusCost + " Focus" : sp.mpCost + " MP")} - {sp.tpCode}{{/}}", sp.desc, "icons/cmd_spell"));
                    break;
                case 10:
                    foreach (var it in GrimoireDatabase.Items.Values)
                        L.Add(new GuideItem($"{it.name}  {{#ffd86a}}{it.price} G{{/}}", it.desc, "items/" + it.iconKey));
                    L.Add(new GuideItem("Logic Fragment", "Hanya didapat dari Perfect Guard dan pertarungan Flawless (tanpa salah). Bukti penguasaan materi.", "icons/fragment"));
                    break;
                case 11:
                    L.Add(new GuideItem("F1 Pilihan Ganda", "Pilih satu jawaban A-D. Konsep, Big-O, syarat algoritma."));
                    L.Add(new GuideItem("F2 Benar / Salah", "Pernyataan singkat tentang algoritma."));
                    L.Add(new GuideItem("F3 Trace Array", "Pilih kondisi array yang benar setelah pass ke-k (Bubble, Selection, Insertion)."));
                    L.Add(new GuideItem("F4 Swap Puzzle", "Tukar elemen mengikuti aturan algoritma."));
                    L.Add(new GuideItem("F5 Pointer Hunt", "Tentukan indeks mid di setiap langkah Binary Search: mid = (low + high) // 2."));
                    L.Add(new GuideItem("F6 Hitung", "Berapa perbandingan / swap / indeks hasil."));
                    L.Add(new GuideItem("F7 Susun Langkah", "Susun potongan pseudocode ke urutan yang benar."));
                    L.Add(new GuideItem("Aturan soal", "Array 4-8 elemen bernilai 1-99, indeks selalu mulai dari 0 dan ditampilkan di bawah elemen. Arah urutan selalu disebutkan. Objek array bertema: buku (Act I), lilin (Act II), kristal (Act III); tingginya sesuai nilai. Elemen yang dibandingkan berkedip kuning."));
                    break;
                case 12:
                    L.Add(new GuideItem("Laporan Guru [T]", "Akurasi per TP, rata-rata waktu jawab, miskonsepsi yang paling sering dipilih, dan ekspor CSV untuk analisis lebih lanjut.", "icons/cmd_analyze"));
                    L.Add(new GuideItem("Mode kelas", "Untuk penilaian yang adil: pilih satu tingkat kesulitan untuk semua siswa dan matikan DDA di Pengaturan."));
                    L.Add(new GuideItem("12 Tujuan Pembelajaran", "TP-01 array & indeks ... TP-12 memilih algoritma berdasarkan Big-O. Daftar lengkap ada di Codex [C]."));
                    break;
            }
            return L;
        }

        private void DrawGuide()
        {
            ModalFrame("BUKU PANDUAN");
            // chapters (left)
            Rect list = new Rect(ModalRect.x + 8, ModalRect.y + 30, 128, ModalRect.height - 38);
            Panel(list, "dark");
            for (int i = 0; i < GuideChapters.Length; i++)
            {
                Rect r = new Rect(list.x + 10, list.y + 6 + i * 16, list.width - 16, 15);
                if (Row(r, GuideChapters[i], _guideChapter == i)) { _guideChapter = _listSel = i; _modalScroll = 0; }
            }
            // content (right, scrollable)
            Rect view = new Rect(ModalRect.x + 142, ModalRect.y + 30, ModalRect.width - 150, ModalRect.height - 38);
            Panel(view, "dark");
            var items = GuideContent(_guideChapter);
            Rect inner = new Rect(view.x + 6, view.y + 4, view.width - 12, view.height - 8);
            if (ScrollWheel(view, out float d)) _modalScroll += d * 12f;
            if (Pressed(Key.PageDown)) _modalScroll += 80; if (Pressed(Key.PageUp)) _modalScroll -= 80;

            BeginClip(inner);
            float y = 4 - _modalScroll, w = inner.width - 8;
            foreach (var it in items)
            {
                float x = 2;
                float iconH = 0;
                if (it.anim != null)
                {
                    var key = it.anim.StartsWith("npc_") ? "npc/" + it.anim.Substring(4).Replace("_idle", "") : "anim/" + it.anim;
                    var tex = T(key);
                    float sz = tex != null ? tex.height : 48;
                    if (sz > 48) sz = 48;
                    Frame(new Rect(x, y, sz, sz), tex, AnimLibrary.FrameAt(it.anim, Time.time, true));
                    x += sz + 6; iconH = sz;
                }
                else if (it.icon != null)
                {
                    var tex = T(it.icon);
                    if (tex != null)
                    {
                        float iw = tex.width, ih = tex.height;
                        if (tex.width > tex.height * 2) { iw = tex.height; }   // strips: first frame
                        if (iw > ih) Frame(new Rect(x, y + 1, ih, ih), tex, 0); else Image(new Rect(x, y + 1, iw, ih), tex);
                        x += Mathf.Max(iw > ih ? ih : iw, 12) + 6; iconH = ih;
                    }
                }
                Text(x, y, it.title, Gold);
                float bh = TextWrap(new Rect(x, y + 12, w - x, 400), it.body, Ink);
                y += Mathf.Max(iconH + 4, 12 + bh + 6);
                Fill(new Rect(2, y - 3, w, 1), new Color(1, 1, 1, 0.05f));
            }
            float contentH = y + _modalScroll;
            EndClip();
            _modalScroll = Mathf.Clamp(_modalScroll, 0, Mathf.Max(0, contentH - inner.height + 4));
            if (contentH > inner.height)
            {
                float t = _modalScroll / Mathf.Max(1, contentH - inner.height);
                Fill(new Rect(view.xMax - 4, view.y + 4 + t * (view.height - 28), 2, 20), new Color(1f, 0.85f, 0.4f, 0.6f));
            }
        }

        // =====================================================================
        // CODEX & BESTIARY
        // =====================================================================
        private static readonly string[] BestiaryAnims = { "idle", "attack", "cast", "hurt", "death", "spawn", "enrage", "idle2" };
        private static readonly string[] BestiaryAnimNames = { "Diam", "Serang", "Skill", "Terluka", "Mati", "Muncul", "Murka", "Fase 2" };

        private void DrawCodexBestiary()
        {
            ModalFrame(_codexTab == 0 ? "CODEX GRIMOIRE  -  12 Tujuan Pembelajaran" : "BESTIARY  -  19 Monster");
            if (Button(new Rect(ModalRect.x + 300, ModalRect.y + 6, 100, 16), "Codex", "icons/cmd_spell", true, _codexTab == 0, align: 1)) { _codexTab = 0; _listSel = 0; }
            if (Button(new Rect(ModalRect.x + 404, ModalRect.y + 6, 100, 16), "Bestiary", "icons/skull", true, _codexTab == 1, align: 1)) { _codexTab = 1; _listSel = 0; }

            Rect list = new Rect(ModalRect.x + 8, ModalRect.y + 30, 150, ModalRect.height - 38);
            Rect det = new Rect(ModalRect.x + 164, ModalRect.y + 30, ModalRect.width - 172, ModalRect.height - 38);
            Panel(list, "dark"); Panel(det, "dark");

            if (_codexTab == 0)
            {
                var cx = GrimoireDatabase.Codex;
                _listSel = Mathf.Clamp(_listSel, 0, cx.Count - 1);
                for (int i = 0; i < cx.Count; i++)
                {
                    Rect r = new Rect(list.x + 10, list.y + 6 + i * 16, list.width - 16, 15);
                    if (Row(r, $"{cx[i].tp}", _listSel == i, null, true, codexOpened.Contains(cx[i].tp) ? "*" : null, Gold)) _listSel = i;
                }
                var c = cx[_listSel];
                codexOpened.Add(c.tp);
                Strip(det.xMax - 58, det.y + 4, "npc/lyra", "npc_lyra_idle", Time.time, 1, new Color(1, 1, 1, 0.9f));
                float y = det.y + 8;
                Text(det.x + 8, y, c.tp, Cyan); Text(det.x + 50, y, c.bloom, Dim); y += 14;
                y += TextWrap(new Rect(det.x + 8, y, det.width - 80, 30), c.title, Gold, 1, true, 2) + 4;
                Text(det.x + 8, y, c.complexity, Purple); y += 16;
                y += TextWrap(new Rect(det.x + 8, y, det.width - 16, 80), c.summary, Ink) + 8;
                Text(det.x + 8, y, "Pseudocode:", Dim); y += 12;
                float ph = c.pseudocode.Split('\n').Length * 12 + 10;
                Panel(new Rect(det.x + 6, y, det.width - 12, ph), "panel");
                Text(det.x + 14, y + 5, c.pseudocode, new Color(0.7f, 0.95f, 0.7f));
                y += ph + 6;
                var spells = GrimoireDatabase.Spells.Where(s => s.tpCode == c.tp).Select(s => s.name).ToList();
                var mons = GrimoireDatabase.MonsterOrder.Where(m => GrimoireDatabase.Monsters[m].tpPool.Contains(c.tp)).Select(m => GrimoireDatabase.Monsters[m].name).ToList();
                if (y < det.yMax - 24) TextWrap(new Rect(det.x + 8, y, det.width - 16, 40), $"{{#8a8ea8}}Diuji oleh:{{/}} {string.Join(", ", mons)}{(spells.Count > 0 ? $"\n{{#8a8ea8}}Mantra:{{/}} {string.Join(", ", spells)}" : "")}", Ink, 1, true, 3);
            }
            else
            {
                var order = GrimoireDatabase.MonsterOrder;
                _listSel = Mathf.Clamp(_listSel, 0, order.Length - 1);
                for (int i = 0; i < order.Length; i++)
                {
                    var m = GrimoireDatabase.Monsters[order[i]];
                    Rect r = new Rect(list.x + 10, list.y + 6 + i * 16, list.width - 16, 15);
                    if (Row(r, m.name, _listSel == i, null, true, monstersSeen.Contains(m.id) ? null : "?", Dim)) { _listSel = i; _bestiaryAnimStart = Time.time; }
                }
                var mon = GrimoireDatabase.Monsters[order[_listSel]];
                // animation previewer: every generated animation can be played here
                string anim = BestiaryAnims[Mathf.Clamp(_bestiaryAnim, 0, BestiaryAnims.Length - 1)];
                if (AnimLibrary.Durations(mon.id + "_" + anim) == null) anim = "idle";
                var tex = T($"anim/{mon.id}_{anim}");
                float sz = (tex != null ? tex.height : 48) * 2;
                Rect stage = new Rect(det.x + 8, det.y + 8, 136, 136);
                Panel(stage, "panel");
                float len = AnimLibrary.Length(mon.id + "_" + anim);
                float t = (Time.time - _bestiaryAnimStart) % (len + 0.6f);
                Frame(new Rect(stage.center.x - sz * 0.5f, stage.yMax - 4 - sz, sz, sz), tex, AnimLibrary.FrameAt(mon.id + "_" + anim, t, AnimLibrary.Loops(mon.id + "_" + anim)));
                for (int k = 0; k < BestiaryAnims.Length; k++)
                {
                    bool has = AnimLibrary.Durations(mon.id + "_" + BestiaryAnims[k]) != null;
                    Rect br = new Rect(det.x + 8 + (k % 3) * 46, det.y + 148 + (k / 3) * 17, 44, 15);
                    if (Button(br, BestiaryAnimNames[k], null, has, _bestiaryAnim == k, align: 1)) { _bestiaryAnim = k; _bestiaryAnimStart = Time.time; }
                }

                float x = det.x + 152, y = det.y + 8, w = det.width - 160;
                Text(x, y, mon.name, isBossRole(mon.role) ? new Color(1f, 0.55f, 0.45f) : Gold); y += 12;
                Text(x, y, $"Act {mon.act} - {mon.role}", Dim); y += 14;
                Icon("icons/hp", x, y); Text(x + 12, y + 1, $"{mon.hp}", Ink);
                Icon("icons/atk", x + 44, y); Text(x + 56, y + 1, $"{mon.atk}", Ink);
                Icon("icons/def", x + 84, y); Text(x + 96, y + 1, $"{mon.def}", Ink);
                Icon("icons/exp", x + 124, y); Text(x + 136, y + 1, $"{mon.exp}", Ink); y += 16;
                y += TextWrap(new Rect(x, y, w, 24), "TP: " + string.Join(", ", mon.tpPool), Cyan, 1, true, 2) + 2;
                string st = GrimoireStatus.OnBreak(mon.id, 1);
                if (st != null) { Icon($"status/{st}", x, y - 1); Text(x + 15, y + 1, "Jika Break: " + GrimoireStatus.Get(st).name, Red); y += 14; }
                int vol = GrimoireStatus.Volley(mon.id);
                if (vol > 1) { Text(x, y, $"Menyerang {vol}x beruntun", Gold); y += 12; }
                y += TextWrap(new Rect(x, y, w, 60), mon.mechanicDesc, Ink) + 6;
                y += TextWrap(new Rect(x, y, w, 70), "{#8a8ea8}\"" + mon.lore + "\"{/}", Dim) + 6;
                bool analyzed = monstersAnalyzed.Contains(mon.id);
                var weak = GrimoireDatabase.Spells.Where(s => mon.tpPool.Contains(s.tpCode)).Select(s => s.name).ToList();
                if (y < det.yMax - 40) y += TextWrap(new Rect(x, y, w, 30), analyzed ? "{#8ae05a}Kelemahan:{/} " + string.Join(", ", weak) : "{#8a8ea8}Kelemahan: gunakan Analisis dalam pertarungan.{/}", Ink, 1, true, 2);
                if (Button(new Rect(det.xMax - 98, det.yMax - 22, 90, 16), "Latih (Spar)", "icons/cmd_attack", viewMode != GameViewMode.Battle, false, "danger"))
                {
                    activeModal = ModalOverlay.None;
                    if (!runStarted) { difficulty = (Difficulty)_diffSel; runStarted = true; }
                    StartBattleEncounter(mon.id, isBossRole(mon.role));
                }
            }
        }

        private static bool isBossRole(string role) => role != "Regular";

        // =====================================================================
        // SHOP (Mordecai) & BAG
        // =====================================================================
        private void DrawShop()
        {
            ModalFrame(_shopTab == 0 ? "TOKO MORDECAI SI PANDAI BESI" : "TAS & PERLENGKAPAN");
            if (Button(new Rect(ModalRect.x + 300, ModalRect.y + 6, 100, 16), "Toko", "icons/gold", true, _shopTab == 0, align: 1)) { _shopTab = 0; _modalScroll = 0; }
            if (Button(new Rect(ModalRect.x + 404, ModalRect.y + 6, 100, 16), "Tas", "icons/cmd_item", true, _shopTab == 1, align: 1)) { _shopTab = 1; _modalScroll = 0; }

            Rect side = new Rect(ModalRect.x + 8, ModalRect.y + 30, 150, ModalRect.height - 38);
            Panel(side, "dark");
            Strip(side.x + 43, side.y + 6, "npc/mordecai", "npc_mordecai_idle", Time.time, 1);
            TextWrap(new Rect(side.x + 8, side.y + 76, side.width - 16, 60),
                _shopTab == 0 ? "\"Logika tajam, bilah pun tajam. Mau beli apa, Ksatria?\"" : "\"Pilih perlengkapan yang cocok dengan kelemahanmu.\"", Ink);
            float y = side.y + 120;
            Icon("icons/gold", side.x + 8, y); Text(side.x + 22, y + 1, $"{gold} Gold", Gold); y += 14;
            Icon("icons/fragment", side.x + 8, y); Text(side.x + 22, y + 1, $"{logicFragments} Logic Fragment", Cyan); y += 18;
            Text(side.x + 8, y, "Dipakai:", Dim); y += 12;
            Icon("items/" + GrimoireDatabase.Items[equippedWeapon].iconKey, side.x + 8, y);
            TextWrap(new Rect(side.x + 28, y + 2, side.width - 34, 24), GrimoireDatabase.Items[equippedWeapon].name, Ink, 1, true, 2); y += 26;
            if (!string.IsNullOrEmpty(equippedAccessory))
            {
                Icon("items/" + GrimoireDatabase.Items[equippedAccessory].iconKey, side.x + 8, y);
                TextWrap(new Rect(side.x + 28, y + 2, side.width - 34, 24), GrimoireDatabase.Items[equippedAccessory].name, Ink, 1, true, 2); y += 26;
            }
            Text(side.x + 8, y, $"ATK {TotalAtk}  DEF {TotalDef}  SPD {TotalSpd}", Ink);

            Rect view = new Rect(ModalRect.x + 164, ModalRect.y + 30, ModalRect.width - 172, ModalRect.height - 38);
            Panel(view, "dark");
            IEnumerable<ItemDef> items = GrimoireDatabase.Items.Values;
            if (_shopTab == 1) items = items.Where(i => (i.kind == "consumable" && consumables.TryGetValue(i.id, out int q) && q > 0) || ownedEquipment.Contains(i.id));
            var list = items.ToList();
            if (ScrollWheel(view, out float d)) _modalScroll += d * 12f;
            Rect inner = new Rect(view.x + 4, view.y + 4, view.width - 8, view.height - 8);
            BeginClip(inner);
            float ry = 2 - _modalScroll;
            foreach (var it in list)
            {
                Rect row = new Rect(2, ry, inner.width - 6, 34);
                bool inView = row.yMax > 0 && row.y < inner.height;
                if (inView)
                {
                    NineSlice(row, T(Hover(row) ? "ui2/slot_hover" : "ui2/slot_normal"), 4);
                    Icon("items/" + it.iconKey, row.x + 6, row.y + 9);
                    Text(row.x + 28, row.y + 5, it.name, Ink);
                    TextWrap(new Rect(row.x + 28, row.y + 17, row.width - 130, 16), it.desc, Dim, 1, true, 1);
                    Rect b = new Rect(row.xMax - 88, row.y + 9, 82, 16);
                    if (it.kind == "consumable")
                    {
                        int q = consumables.TryGetValue(it.id, out int qq) ? qq : 0;
                        if (_shopTab == 0)
                        {
                            if (Button(b, $"{it.price} G", "icons/gold", gold >= it.price, false, "normal", $"x{q}")) BuyItem(it.id);
                        }
                        else if (Button(b, $"Pakai", null, q > 0 && (it.healHp > 0 || it.healMp > 0 || it.id == "scroll_clarity" || it.id == "golden_die") && viewMode != GameViewMode.Battle, false, "normal", $"x{q}"))
                            UseConsumableItem(it.id);
                    }
                    else
                    {
                        bool owned = ownedEquipment.Contains(it.id);
                        bool eq = equippedWeapon == it.id || equippedAccessory == it.id;
                        if (eq) Button(b, "Dipakai", null, true, true, "gold", align: 1);
                        else if (owned) { if (Button(b, "Pakai", null, true, false, align: 1)) EquipItem(it.id); }
                        else if (Button(b, $"{it.price} G", "icons/gold", gold >= it.price, false)) BuyItem(it.id);
                    }
                }
                ry += 37;
            }
            float contentH = ry + _modalScroll;
            EndClip();
            _modalScroll = Mathf.Clamp(_modalScroll, 0, Mathf.Max(0, contentH - inner.height));
            if (list.Count == 0) TextCenter(view, "Tas kosong.", Dim);
        }

        // =====================================================================
        // TEACHER REPORT
        // =====================================================================
        private void DrawTeacher()
        {
            ModalFrame("LAPORAN GURU  -  Analitik Pembelajaran");
            int total = telemetryLog.Count, cor = telemetryLog.Count(r => r.isCorrect);
            float avgT = total > 0 ? telemetryLog.Average(r => r.responseTimeSec) : 0f;
            Rect top = new Rect(ModalRect.x + 8, ModalRect.y + 30, ModalRect.width - 16, 40);
            Panel(top, "dark");
            Text(top.x + 8, top.y + 7, $"Total soal: {total}", Ink);
            Text(top.x + 110, top.y + 7, $"Akurasi: {(total > 0 ? cor * 100 / total : 0)}%", total > 0 && cor * 100 / total >= 70 ? Green : Gold);
            Text(top.x + 210, top.y + 7, $"Rata-rata waktu: {avgT:F1} dtk", Ink);
            Text(top.x + 360, top.y + 7, $"Mode: {GrimoireStatus.DifficultyName(difficulty)}  DDA: {(ddaEnabled ? ddaTier : "off")}", Ink);
            int pg = telemetryLog.Count(r => r.defenceGrade == "PERFECT GUARD");
            Text(top.x + 8, top.y + 21, $"Perfect Guard: {pg}   Codex dibuka: {codexOpened.Count}/12   Monster dikenal: {monstersSeen.Count}/{GrimoireDatabase.MonsterOrder.Length}   Logic Fragment: {logicFragments}", Dim);
            if (Button(new Rect(top.xMax - 104, top.y + 18, 98, 16), "Ekspor CSV", "icons/cmd_item", total > 0, false, "gold"))
            {
                string csv = ProceduralQuestionEngine.ExportTelemetryToCsv(telemetryLog);
                string path = Path.Combine(Application.persistentDataPath, "GrimoireAegis_Student_Telemetry.csv");
                File.WriteAllText(path, csv);
                lastExportedCsvPath = path;
                GrimoireAudioManager.Instance?.PlaySfx("levelup");
            }

            // teacher shortcut: start a lesson straight at the Act being taught
            if (viewMode != GameViewMode.Battle)
            {
                Text(ModalRect.x + 300, ModalRect.yMax - 20, "Mulai langsung:", Dim);
                for (int act = 1; act <= 3; act++)
                {
                    int a2 = act;
                    if (Button(new Rect(ModalRect.x + 380 + (act - 1) * 72, ModalRect.yMax - 24, 66, 16), $"Act {ActRoman(act)}", null, true, false, align: 1))
                    {
                        if (!runStarted) { runStarted = true; _seenTips.Add("board"); }
                        StartActExpedition(a2);
                    }
                }
            }

            Rect tbl = new Rect(ModalRect.x + 8, ModalRect.y + 74, ModalRect.width - 16, ModalRect.height - 110);
            Panel(tbl, "dark");
            float y = tbl.y + 6;
            Text(tbl.x + 8, y, "TP", Dim); Text(tbl.x + 52, y, "Topik", Dim); Text(tbl.x + 250, y, "Akurasi", Dim); Text(tbl.x + 370, y, "Soal", Dim); Text(tbl.x + 404, y, "Waktu", Dim); Text(tbl.x + 444, y, "Miskonsepsi tersering", Dim);
            y += 13;
            foreach (var c in GrimoireDatabase.Codex)
            {
                var recs = telemetryLog.Where(r => r.tp == c.tp).ToList();
                int n = recs.Count, k = recs.Count(r => r.isCorrect);
                float acc = n > 0 ? (float)k / n : 0f;
                string mis = recs.Where(r => !r.isCorrect).GroupBy(r => r.misconceptionTag).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} ({g.Count()}x)").FirstOrDefault() ?? "-";
                Text(tbl.x + 8, y, c.tp, Cyan);
                TextWrap(new Rect(tbl.x + 52, y, 194, 12), c.title, Ink, 1, true, 1);
                Bar(new Rect(tbl.x + 250, y + 1, 80, 7), acc, acc >= 0.7f ? new Color(0.45f, 0.85f, 0.35f) : (acc >= 0.5f ? new Color(0.95f, 0.75f, 0.2f) : new Color(0.85f, 0.25f, 0.25f)));
                Text(tbl.x + 334, y, n > 0 ? $"{Mathf.RoundToInt(acc * 100)}%" : "-", Ink);
                Text(tbl.x + 370, y, $"{n}", Ink);
                Text(tbl.x + 404, y, n > 0 ? $"{recs.Average(r => r.responseTimeSec):F1}" : "-", Ink);
                TextWrap(new Rect(tbl.x + 444, y, tbl.width - 450, 12), mis, n > k ? Red : Dim, 1, true, 1);
                y += 15;
            }
            if (!string.IsNullOrEmpty(lastExportedCsvPath)) TextWrap(new Rect(tbl.x + 8, tbl.yMax - 14, tbl.width - 16, 12), "CSV: " + lastExportedCsvPath, Green, 1, true, 1);
        }

        // =====================================================================
        // SETTINGS
        // =====================================================================
        private void DrawSettings()
        {
            ModalFrame("PENGATURAN");
            float x = ModalRect.x + 20, y = ModalRect.y + 36;
            Text(x, y, "Tingkat Kesulitan", Gold); y += 14;
            for (int i = 0; i < 3; i++)
            {
                var d = (Difficulty)i;
                if (Button(new Rect(x + i * 110, y, 104, 18), GrimoireStatus.DifficultyName(d), i == 0 ? "icons/heart_full" : i == 1 ? "icons/timer" : "icons/skull", true, difficulty == d, difficulty == d ? "gold" : "normal"))
                    difficulty = d;
            }
            y += 22;
            y += TextWrap(new Rect(x, y, 540, 40), GrimoireStatus.DifficultyDesc(difficulty), Ink) + 10;

            Text(x, y, "Pengali Timer (aksesibilitas)", Gold); y += 14;
            float[] mults = { 1f, 1.25f, 1.5f, 2f };
            for (int i = 0; i < mults.Length; i++)
                if (Button(new Rect(x + i * 70, y, 64, 18), $"{mults[i]:0.##}x", null, difficulty != Difficulty.Easy, Mathf.Approximately(timerMultiplier, mults[i]), align: 1)) timerMultiplier = mults[i];
            if (difficulty == Difficulty.Easy) Text(x + 290, y + 5, "(mode Mudah tidak memakai timer)", Dim);
            y += 28;

            Text(x, y, "Pembelajaran & Tampilan", Gold); y += 14;
            if (Button(new Rect(x, y, 200, 18), $"DDA (kesulitan dinamis): {(ddaEnabled ? "ON" : "OFF")}", null, true, ddaEnabled)) ddaEnabled = !ddaEnabled;
            if (Button(new Rect(x + 210, y, 220, 18), $"Getar layar & kilatan: {(accessibilityNoShake ? "OFF" : "ON")}", null, true, !accessibilityNoShake)) accessibilityNoShake = !accessibilityNoShake;
            y += 28;

            var au = GrimoireAudioManager.Instance;
            if (au != null)
            {
                Text(x, y, "Audio", Gold); y += 14;
                Text(x, y + 5, $"Musik {Mathf.RoundToInt(au.MusicVolume * 100)}%", Ink);
                if (Button(new Rect(x + 90, y, 20, 18), "-", null, true, false, align: 1)) au.SetVolumes(au.SfxVolume, Mathf.Clamp01(au.MusicVolume - 0.1f), au.IsMuted);
                if (Button(new Rect(x + 114, y, 20, 18), "+", null, true, false, align: 1)) au.SetVolumes(au.SfxVolume, Mathf.Clamp01(au.MusicVolume + 0.1f), au.IsMuted);
                Text(x + 150, y + 5, $"Efek {Mathf.RoundToInt(au.SfxVolume * 100)}%", Ink);
                if (Button(new Rect(x + 230, y, 20, 18), "-", null, true, false, align: 1)) au.SetVolumes(Mathf.Clamp01(au.SfxVolume - 0.1f), au.MusicVolume, au.IsMuted);
                if (Button(new Rect(x + 254, y, 20, 18), "+", null, true, false, align: 1)) au.SetVolumes(Mathf.Clamp01(au.SfxVolume + 0.1f), au.MusicVolume, au.IsMuted);
                if (Button(new Rect(x + 290, y, 80, 18), au.IsMuted ? "Bunyikan" : "Bisukan", null, true, au.IsMuted, align: 1)) au.SetVolumes(au.SfxVolume, au.MusicVolume, !au.IsMuted);
                y += 28;
            }
            if (viewMode != GameViewMode.Title && Button(new Rect(x, ModalRect.yMax - 30, 140, 18), "Kembali ke Judul", null, true, false, "danger"))
            {
                activeModal = ModalOverlay.None; _scheduled.Clear();
                viewMode = GameViewMode.Title; _titleMode = 0;
                GrimoireAudioManager.Instance?.PlayMusic("music_title");
            }
        }
    }
}
