using System;
using System.Collections.Generic;
using GrimoireAegis.Core;
using UnityEngine;
using static GrimoireAegis.UI.PixelUI;

namespace GrimoireAegis.UI
{
    /// <summary>
    /// Short story lines and one-time tips, spoken by Archivist Lyra (or a boss).
    /// Rule: one idea per line, shown only at the moment it is needed.
    /// </summary>
    public partial class GrimoireGameController
    {
        private struct Line { public string who, portrait, text; }

        private readonly Queue<Line> _dialog = new Queue<Line>();
        private Line _line;
        private float _lineStart;
        private Action _dialogDone;
        private readonly HashSet<string> _seenTips = new HashSet<string>();

        public bool DialogOpen => _line.text != null;

        private const string Lyra = "Lyra";

        private void Say(Action done, params (string who, string text)[] lines)
        {
            foreach (var l in lines)
            {
                string portrait = l.who == Lyra ? "npc/lyra" : l.who == "Mordecai" ? "npc/mordecai" : l.who == "Bayang-bayang Ordo" ? "npc/shade" : null;
                _dialog.Enqueue(new Line { who = l.who, portrait = portrait, text = l.text });
            }
            var prev = _dialogDone;
            _dialogDone = prev == null ? done : () => { prev(); done?.Invoke(); };
            if (!DialogOpen) NextLine();
        }

        private void Tip(string id, Action done, params (string who, string text)[] lines)
        {
            if (_seenTips.Contains(id)) { done?.Invoke(); return; }
            _seenTips.Add(id);
            Say(done, lines);
        }

        private void NextLine()
        {
            if (_dialog.Count == 0)
            {
                _line = default;
                var d = _dialogDone;
                _dialogDone = null;
                d?.Invoke();
                return;
            }
            _line = _dialog.Dequeue();
            _lineStart = Time.time;
            GrimoireAudioManager.Instance?.PlaySfx("move", 0.5f);
        }

        private int VisibleChars => Mathf.Min(_line.text.Length, Mathf.FloorToInt((Time.time - _lineStart) * 90f));

        public void AdvanceDialog()
        {
            if (!DialogOpen) return;
            if (VisibleChars < _line.text.Length) { _lineStart = -999f; return; }   // reveal the rest first
            NextLine();
        }

        public void SkipDialog()
        {
            _dialog.Clear();
            NextLine();
        }

        private void DrawDialog()
        {
            if (!DialogOpen) return;
            float dy = PuzzleOpen ? 30 : (viewMode == GameViewMode.Battle ? 168 : 252);   // never cover the puzzle answers
            Rect p = new Rect(70, dy, 500, 70);
            Panel(p);
            float x = p.x + 10;
            if (_line.portrait != null)
            {
                Rect port = new Rect(p.x + 8, p.y + 7, 56, 56);
                NineSlice(port, T("ui2/slot_normal"), 4);
                var tex = T(_line.portrait);
                if (tex != null && IsRepaint)
                {
                    int n = Mathf.Max(1, tex.width / tex.height);
                    int f = AnimLibrary.FrameAt("npc_" + _line.portrait.Substring(4) + "_idle", Time.time, true);
                    GUI.DrawTextureWithTexCoords(new Rect(port.x + 4, port.y + 4, 48, 48), tex, new Rect((f + 8f / 64f) / n, 8f / 64f, (48f / 64f) / n, 48f / 64f), true);
                }
                x = port.xMax + 10;
            }
            else if (activeMonster != null)
            {
                Strip(p.x + 8, p.y + 7, $"anim/{activeMonster.id}_idle", activeMonster.id + "_idle", Time.time, 1);
                x = p.x + 64;
            }
            Text(x, p.y + 8, _line.who, Gold);
            if (_dialog.Count > 0)
            {
                if (Button(new Rect(p.xMax - 64, p.y + 5, 56, 14), "Lewati", null, true, false, align: 1)) { SkipDialog(); return; }
            }
            else
            {
                TextRight(p.xMax - 10, p.y + 8, "Klik / Spasi", Dim);
            }
            string shown = _line.text.Substring(0, VisibleChars);
            TextWrap(new Rect(x, p.y + 21, p.xMax - x - 12, 44), shown, Ink, 1, true, 4);
            if (VisibleChars >= _line.text.Length && Mathf.Sin(Time.time * 6f) > 0) Icon("ui2/arrow_down", p.xMax - 16, p.yMax - 11);
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0) { Event.current.Use(); AdvanceDialog(); }
        }

        // =====================================================================
        // STORY BEATS & TIPS
        // =====================================================================
        private void StoryIntro(Action done) => Say(done,
            (Lyra, "Ksatria! Syukurlah kamu sudah bangun."),
            (Lyra, "Raja Entropy menghancurkan Katalog Agung. Buku, angka, bahkan urutan matahari terbit di Algoria jadi kacau."),
            (Lyra, "Monster kutukan Glitch tidak bisa dilawan dengan pedang saja. Seranganmu harus ditangkis dengan logika."),
            (Lyra, "Aku Lyra, penjaga katalog terakhir. Ayo, kita mulai dari Perpustakaan Terkubur."));

        private void TipBoard() => Tip("board", null,
            (Lyra, "Tekan Spasi untuk melempar dadu. Aegis akan berjalan sendiri, dan di persimpangan kamu yang memilih arah."),
            (Lyra, "Gerbang boss terkunci rapat. Cari 3 Kunci Indeks: dari Penjaga Kunci, Rak Terkunci, dan Peti Tersembunyi."));

        private void TipGateLocked() => Say(null,
            (Lyra, $"Gerbangnya masih terkunci. Kunci Indeks: {keysCollected}/3. Coba jalan lain."));

        private void TipGateOpen() => Say(null, (Lyra, "Ketiga kunci bersinar... gerbangnya terbuka! Boss ada di depan."));

        private void TipKey() => Tip("key" + keysCollected, null,
            (Lyra, keysCollected >= 3 ? "Tiga kunci! Sekarang pergi ke gerbang boss." : $"Bagus, satu Kunci Indeks lagi! Sekarang {keysCollected}/3."));

        private void TipPuzzle() => Tip("puzzle", null,
            (Lyra, "Rak ini terkunci teka-teki. Tidak ada batas waktu dan tidak ada damage, jadi pikirkan baik-baik."));

        private void TipFirstBattle() => Tip("battle", null,
            (Lyra, "Giliranmu! Pilih Serang untuk menebas monster itu."));

        private void TipFirstDefend(Action done) => Tip("defend", done,
            (Lyra, "Awas, dia mau menyerang! Pilih jawaban yang benar untuk menangkis."),
            (Lyra, difficulty == Difficulty.Easy ? "Tidak ada batas waktu, jadi santai saja." : "Jawab dengan cepat, dan Aegis akan langsung membalas."));

        private void TipSpells() => Tip("spells", null,
            (Lyra, "Mantra lebih kuat dari tebasan, tapi kamu harus menjawab soal topiknya dulu. Mantra memakai MP."));

        private void TipWrong() => Tip("wrong", null,
            (Lyra, "Tidak apa-apa salah. Baca pembahasannya, karena soal berikutnya angkanya akan berbeda."));

        private void BossIntro(string id, Action done)
        {
            switch (id)
            {
                case "the_blind_archivist":
                    Say(done, ("The Blind Archivist", "Aku tidak butuh mata untuk menemukanmu, Ksatria... selama raknya terurut."),
                              (Lyra, "Dia memakai Binary Search! Ingat: cari di tengah, buang separuhnya."));
                    break;
                case "the_abyss_golem":
                    Say(done, ("The Abyss Golem", "BATU... TIDAK... TERURUT... HANCURKAN!"),
                              (Lyra, "Kulit batunya menahan pedang. Pakai mantra Sorting!"));
                    break;
                case "entropy_chaos_king":
                    Say(done, ("Entropy", "Kau tidak bisa menghancurkanku. Kekacauan itu abadi."),
                              (Lyra, "Entropy tidak bisa dihancurkan... tapi bisa diurutkan! Pakai semua yang sudah kamu pelajari."));
                    break;
                default:
                    done?.Invoke();
                    break;
            }
        }

        private void ActClear(Action done)
        {
            string skill = currentAct == 1 ? "menemukan (Searching)" : "menata (Sorting)";
            Say(done, (Lyra, $"Kamu berhasil! Kemampuan {skill} kembali ke Katalog Agung."),
                      (Lyra, currentAct == 1 ? "Jalan turun ke Benteng Tulang sudah terbuka. Di sana semuanya harus diurutkan." :
                                               "Di atas sana, Menara Pecah-Belah. Entropy menunggu di puncaknya."));
        }

        private static readonly string[][] Notes =
        {
            new[] { "Indeks array selalu mulai dari 0. Elemen pertama ada di A[0].",
                    "Linear Search memeriksa satu per satu dari kiri. Kalau tidak ketemu, hasilnya -1.",
                    "Binary Search hanya boleh dipakai pada data yang sudah terurut." },
            new[] { "Bubble Sort menukar dua elemen bersebelahan sampai yang terbesar 'menggelembung' ke ujung.",
                    "Selection Sort mencari yang terkecil, lalu menaruhnya di depan.",
                    "Insertion Sort menyisipkan satu kartu ke tempat yang pas, seperti merapikan kartu di tangan." },
            new[] { "Merge Sort membelah array jadi dua terus-menerus, lalu menggabungkannya sambil mengurutkan.",
                    "Quick Sort memilih pivot: yang lebih kecil ke kiri, yang lebih besar ke kanan.",
                    "O(log n) jauh lebih cepat dari O(n) kalau datanya banyak." },
        };

        private void LyraNote()
        {
            var list = Notes[Mathf.Clamp(currentAct, 1, 3) - 1];
            string note = list[UnityEngine.Random.Range(0, list.Length)];
            GrimoireAudioManager.Instance?.PlaySfx("magic");
            Say(null, (Lyra, "Catatan Lyra: " + note));
        }
    }
}
