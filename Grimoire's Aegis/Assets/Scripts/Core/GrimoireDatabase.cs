using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrimoireAegis.Core
{
    [Serializable]
    public class MonsterDef
    {
        public string id;
        public string name;
        public int act;
        public string role; // "Regular", "Mid-Boss", "Boss", "Final Boss"
        public int hp;
        public int atk;
        public int def;
        public int exp;
        public int gold;
        public string[] tpPool;
        public string statusEffect;
        public string mechanicDesc;
        public string lore;
    }

    [Serializable]
    public class SpellDef
    {
        public string id;
        public string name;
        public int mpCost;
        public int focusCost;
        public float multiplier;
        public string tpCode;
        public string desc;
    }

    [Serializable]
    public class ItemDef
    {
        public string id;
        public string name;
        public string kind; // "weapon", "accessory", "consumable"
        public string iconKey;
        public int price;
        public int atkBonus;
        public int defBonus;
        public int spdBonus;
        public float timerBonus;
        public int healHp;
        public int healMp;
        public string desc;
    }

    [Serializable]
    public class CodexEntry
    {
        public string tp;
        public string bloom;
        public string title;
        public string complexity;
        public string summary;
        public string pseudocode;
    }

    [Serializable]
    public class ActStageDef
    {
        public int act;
        public string title;
        public string subtitle;
        public string intro;
        public int gridW;
        public int gridH;
        public Color rimColor;
        public string[] regularEnemies;
        public string[] eliteEnemies;
        public string bossId;
        public string musicKey;
    }

    public static class GrimoireDatabase
    {
        public static readonly Dictionary<int, ActStageDef> Stages = new Dictionary<int, ActStageDef>
        {
            {
                1, new ActStageDef
                {
                    act = 1,
                    title = "ACT I: THE DUSTY ARCHIVES",
                    subtitle = "Linear & Binary Search (TP-01..TP-05)",
                    intro = "Sayap barat Perpustakaan Agung Alexandria-Nova dipenuhi rak buku raksasa yang kehilangan nomor indeksnya.\nGunakan Linear Search pada data acak dan Binary Search pada rak yang telah terurut!",
                    gridW = 6, gridH = 4,
                    rimColor = new Color(0.91f, 0.77f, 0.16f),
                    regularEnemies = new[] { "corrupted_slime", "lost_wisp", "bookworm_horror" },
                    eliteEnemies = new[] { "twin_faced_librarian" },
                    bossId = "the_blind_archivist",
                    musicKey = "music_s1"
                }
            },
            {
                2, new ActStageDef
                {
                    act = 2,
                    title = "ACT II: THE CATACOMBS OF CHAOS",
                    subtitle = "Basic Sorting — Bubble, Selection, Insertion (TP-06..TP-08)",
                    intro = "Ruang bawah tanah tempat prasasti batu kuno runtuh dan bercampur aduk.\nSusun kembali pilar angka menggunakan Bubble Sort, Selection Sort, dan Insertion Sort!",
                    gridW = 7, gridH = 4,
                    rimColor = new Color(0.38f, 0.78f, 0.72f),
                    regularEnemies = new[] { "skeleton_archer", "venomous_arachnid", "grave_digger" },
                    eliteEnemies = new[] { "grave_digger" },
                    bossId = "the_abyss_golem",
                    musicKey = "music_s2"
                }
            },
            {
                3, new ActStageDef
                {
                    act = 3,
                    title = "ACT III: THE TOWER OF ENTROPY",
                    subtitle = "Merge Sort, Quick Sort & Big-O Complexity (TP-09..TP-12)",
                    intro = "Puncak menara tempat Entropy, The Chaos King bersemayam.\nKuasai algoritma Divide & Conquer (Merge Sort & Quick Sort) serta evaluasi kompleksitas Big-O!",
                    gridW = 7, gridH = 5,
                    rimColor = new Color(0.61f, 0.36f, 0.83f),
                    regularEnemies = new[] { "hydra_of_division", "pivot_wraith" },
                    eliteEnemies = new[] { "the_sphinx_of_entropy" },
                    bossId = "entropy_chaos_king",
                    musicKey = "music_s3"
                }
            }
        };

        public static readonly string[] MonsterOrder = new[]
        {
            "corrupted_slime",
            "lost_wisp",
            "bookworm_horror",
            "twin_faced_librarian",
            "the_blind_archivist",
            "skeleton_archer",
            "venomous_arachnid",
            "grave_digger",
            "the_abyss_golem",
            "hydra_of_division",
            "pivot_wraith",
            "the_sphinx_of_entropy",
            "entropy_chaos_king"
        };

        public static readonly Dictionary<string, MonsterDef> Monsters = new Dictionary<string, MonsterDef>
        {
            {
                "corrupted_slime", new MonsterDef {
                    id = "corrupted_slime", name = "Corrupted Slime", act = 1, role = "Regular",
                    hp = 45, atk = 10, def = 3, exp = 35, gold = 25,
                    tpPool = new[] { "TP-01", "TP-02" }, statusEffect = "sticky",
                    mechanicDesc = "Sticky Index: Meminta penelusuran sekuensial dari indeks 0.",
                    lore = "Gumpalan tinta hijau terkorupsi yang menelan halaman indeks satu per satu."
                }
            },
            {
                "lost_wisp", new MonsterDef {
                    id = "lost_wisp", name = "Lost Wisp", act = 1, role = "Regular",
                    hp = 60, atk = 13, def = 4, exp = 45, gold = 32,
                    tpPool = new[] { "TP-02", "TP-03" }, statusEffect = "flicker",
                    mechanicDesc = "Flicker Array: Menguji pencarian elemen absen & syarat Binary Search.",
                    lore = "Roh lentera arsip yang tersesat di antara rak buku yang belum diurutkan."
                }
            },
            {
                "bookworm_horror", new MonsterDef {
                    id = "bookworm_horror", name = "Bookworm Horror", act = 1, role = "Regular",
                    hp = 72, atk = 14, def = 5, exp = 55, gold = 38,
                    tpPool = new[] { "TP-01", "TP-03", "TP-04" }, statusEffect = "torn_page",
                    mechanicDesc = "Torn Page: Mengaburkan 1 opsi jawaban jika pertahanan gagal.",
                    lore = "Ulat buku raksasa yang menggerogoti nomor halaman katalog."
                }
            },
            {
                "twin_faced_librarian", new MonsterDef {
                    id = "twin_faced_librarian", name = "Twin-Faced Librarian", act = 1, role = "Mid-Boss",
                    hp = 150, atk = 18, def = 8, exp = 120, gold = 85,
                    tpPool = new[] { "TP-03", "TP-04", "TP-05" }, statusEffect = "silence",
                    mechanicDesc = "Silence: Mengunci 1 mantra Grimoire selama 2 giliran jika Guard Broken.",
                    lore = "Kurator bertopeng ganda yang menguji efisiensi Linear O(n) vs Binary O(log n)."
                }
            },
            {
                "the_blind_archivist", new MonsterDef {
                    id = "the_blind_archivist", name = "The Blind Archivist", act = 1, role = "Boss",
                    hp = 260, atk = 22, def = 10, exp = 250, gold = 180,
                    tpPool = new[] { "TP-01", "TP-02", "TP-03", "TP-04", "TP-05" }, statusEffect = "fog",
                    mechanicDesc = "Shroud of Fog: Fase 2 menyelimuti elemen array di luar titik tengah (Mid) dengan kabut.",
                    lore = "Penjaga utama Arsip Berdebu yang kehilangan penglihatannya saat The Great Index hancur."
                }
            },
            {
                "skeleton_archer", new MonsterDef {
                    id = "skeleton_archer", name = "Skeleton Archer", act = 2, role = "Regular",
                    hp = 95, atk = 20, def = 8, exp = 80, gold = 55,
                    tpPool = new[] { "TP-06", "TP-07" }, statusEffect = "none",
                    mechanicDesc = "Rapid Volley: Menuntut eksekusi swap Bubble/Selection Sort dengan cepat.",
                    lore = "Pemanah kerangka katakomba yang menembakkan anak panah bernomor acak."
                }
            },
            {
                "venomous_arachnid", new MonsterDef {
                    id = "venomous_arachnid", name = "Venomous Arachnid", act = 2, role = "Regular",
                    hp = 110, atk = 22, def = 9, exp = 95, gold = 65,
                    tpPool = new[] { "TP-06", "TP-08" }, statusEffect = "poison",
                    mechanicDesc = "Venom Bite: Memberikan status Poison (-5% Max HP/giliran selama 3 giliran).",
                    lore = "Laba-laba beracun yang menjerat buku-buku arsip dengan jaring tak beraturan."
                }
            },
            {
                "grave_digger", new MonsterDef {
                    id = "grave_digger", name = "Grave Digger", act = 2, role = "Regular",
                    hp = 135, atk = 24, def = 12, exp = 115, gold = 75,
                    tpPool = new[] { "TP-07", "TP-08" }, statusEffect = "buried",
                    mechanicDesc = "Buried Index: Mengubur 1 nilai elemen array di bawah tanah katakomba.",
                    lore = "Penggali makam kuno yang menimbun prasasti terurut di bawah tanah."
                }
            },
            {
                "the_abyss_golem", new MonsterDef {
                    id = "the_abyss_golem", name = "The Abyss Golem", act = 2, role = "Boss",
                    hp = 420, atk = 30, def = 18, exp = 400, gold = 280,
                    tpPool = new[] { "TP-06", "TP-07", "TP-08" }, statusEffect = "crushed",
                    mechanicDesc = "Heavy Stone Armor & Crushed: Serangan fisik biasa dikurangi 70%; wajib mantra Sorting!",
                    lore = "Golem monolit raksasa yang terbentuk dari runtuhan pilar-pilar katakomba."
                }
            },
            {
                "hydra_of_division", new MonsterDef {
                    id = "hydra_of_division", name = "Hydra of Division", act = 3, role = "Regular",
                    hp = 190, atk = 28, def = 14, exp = 180, gold = 110,
                    tpPool = new[] { "TP-09", "TP-12" }, statusEffect = "hydra_regen",
                    mechanicDesc = "Divide & Regrow: Meregenerasi +15% HP jika pemain gagal menyatukan sub-array Merge Sort.",
                    lore = "Naga berkepala tiga yang membelah diri seperti pohon rekursi Merge Sort."
                }
            },
            {
                "pivot_wraith", new MonsterDef {
                    id = "pivot_wraith", name = "Pivot Wraith", act = 3, role = "Regular",
                    hp = 210, atk = 32, def = 15, exp = 200, gold = 125,
                    tpPool = new[] { "TP-10", "TP-12" }, statusEffect = "false_pivot",
                    mechanicDesc = "False Pivot: Memberikan serangan kritikal jika pemain salah mempartisi elemen di sekitar Pivot.",
                    lore = "Roh penari pedang yang menyeimbangkan ruang di atas titik tumpu Quick Sort."
                }
            },
            {
                "the_sphinx_of_entropy", new MonsterDef {
                    id = "the_sphinx_of_entropy", name = "The Sphinx of Entropy", act = 3, role = "Mid-Boss",
                    hp = 340, atk = 35, def = 16, exp = 320, gold = 220,
                    tpPool = new[] { "TP-11", "TP-12" }, statusEffect = "death_seal",
                    mechanicDesc = "Three Death Seals: 3 segel teka-teki evaluasi algoritma & Big-O; 3 kegagalan memicu serangan fatal.",
                    lore = "Penjaga gerbang takhta yang menguji kebijaksanaan pemilihan algoritma tertinggi."
                }
            },
            {
                "entropy_chaos_king", new MonsterDef {
                    id = "entropy_chaos_king", name = "Entropy, The Chaos King", act = 3, role = "Final Boss",
                    hp = 680, atk = 42, def = 22, exp = 1000, gold = 500,
                    tpPool = new[] { "TP-01", "TP-04", "TP-06", "TP-07", "TP-08", "TP-09", "TP-10", "TP-11", "TP-12" },
                    statusEffect = "chaos_catalog",
                    mechanicDesc = "3-Phase Transformation & Grand Catalog: Menggabungkan Searching, Basic Sorting, dan Divide & Conquer!",
                    lore = "Raja Kekacauan kosmik yang menghancurkan The Great Index untuk menghapus keteraturan pengetahuan."
                }
            }
        };

        public static readonly List<SpellDef> Spells = new List<SpellDef>
        {
            new SpellDef { id = "linear_slash", name = "Linear Slash", mpCost = 10, focusCost = 0, multiplier = 1.25f, tpCode = "TP-01", desc = "Tebasan sekuensial dari indeks 0 (1.25x ATK)." },
            new SpellDef { id = "binary_pierce", name = "Binary Pierce", mpCost = 18, focusCost = 0, multiplier = 2.0f, tpCode = "TP-04", desc = "Tusukan titik tengah Mid; menembus 50% DEF musuh (2.0x ATK)." },
            new SpellDef { id = "bubble_shield", name = "Bubble Shield", mpCost = 15, focusCost = 0, multiplier = 1.35f, tpCode = "TP-06", desc = "Serangan gelembung + memulihkan 20 HP & menghapus Poison/Torn Page." },
            new SpellDef { id = "selection_strike", name = "Selection Strike", mpCost = 20, focusCost = 0, multiplier = 1.85f, tpCode = "TP-07", desc = "Menarget titik kelemahan minimum musuh (1.85x ATK)." },
            new SpellDef { id = "insertion_lance", name = "Insertion Lance", mpCost = 22, focusCost = 0, multiplier = 2.15f, tpCode = "TP-08", desc = "Tombak penyisipan presisi tinggi (+25% Crit, 2.15x ATK)." },
            new SpellDef { id = "merge_tempest", name = "Merge Tempest", mpCost = 30, focusCost = 0, multiplier = 2.65f, tpCode = "TP-09", desc = "Badai Divide & Conquer O(n log n) yang menghancurkan armor batu (2.65x ATK)." },
            new SpellDef { id = "quick_judgment", name = "Quick Judgment", mpCost = 35, focusCost = 0, multiplier = 3.2f, tpCode = "TP-10", desc = "Partisi kilat berbasis Pivot dengan daya hancur masif (3.2x ATK)." },
            new SpellDef { id = "order_restored", name = "ORDER RESTORED [LIMIT]", mpCost = 0, focusCost = 100, multiplier = 3.6f, tpCode = "TP-12", desc = "Limit Break (100 Focus): 3.6x Pure Damage (abaikan 100% DEF) + Heal 30% HP!" }
        };

        public static readonly Dictionary<string, ItemDef> Items = new Dictionary<string, ItemDef>
        {
            // 4 Weapons (Index Blade Lv.1 - Lv.4)
            { "blade_lv1", new ItemDef { id = "blade_lv1", name = "Index Blade Lv.1 (Rusty Pointer)", kind = "weapon", iconKey = "blade1", price = 0, atkBonus = 0, desc = "Pedang penunjuk dasar ksatria arsip." } },
            { "blade_lv2", new ItemDef { id = "blade_lv2", name = "Index Blade Lv.2 (Sorted Edge)", kind = "weapon", iconKey = "blade2", price = 150, atkBonus = 8, desc = "ATK +8. Bilah terkalibrasi untuk pengurutan cepat." } },
            { "blade_lv3", new ItemDef { id = "blade_lv3", name = "Index Blade Lv.3 (Binary Saber)", kind = "weapon", iconKey = "blade3", price = 350, atkBonus = 18, desc = "ATK +18. Membelah ruang pencarian menjadi dua." } },
            { "blade_lv4", new ItemDef { id = "blade_lv4", name = "Index Blade Lv.4 (Grand Archivist)", kind = "weapon", iconKey = "blade4", price = 700, atkBonus = 32, desc = "ATK +32. Pedang suci pemulih The Great Index." } },

            // 7 Accessories
            { "acc_monocle", new ItemDef { id = "acc_monocle", name = "Scholar's Monocle", kind = "accessory", iconKey = "monocle", price = 120, desc = "Mengeliminasi 1 opsi pengecoh tambahan saat menggunakan Hint [H]." } },
            { "acc_hourglass", new ItemDef { id = "acc_hourglass", name = "Hourglass of Patience", kind = "accessory", iconKey = "hourglass", price = 180, timerBonus = 3.0f, desc = "+3.0 detik waktu tambahan pada setiap Active Defend." } },
            { "acc_ring_comp", new ItemDef { id = "acc_ring_comp", name = "Ring of Complexity", kind = "accessory", iconKey = "aegis_charm", price = 200, desc = "Mengurangi biaya MP seluruh mantra Grimoire sebesar 20%." } },
            { "acc_aegis_charm", new ItemDef { id = "acc_aegis_charm", name = "Aegis Charm", kind = "accessory", iconKey = "aegis_charm", price = 250, desc = "Sekali per pertarungan mengubah Guard Broken menjadi Solid Block (75% mitigasi)." } },
            { "acc_boots", new ItemDef { id = "acc_boots", name = "Boots of Swiftness", kind = "accessory", iconKey = "hourglass", price = 140, spdBonus = 5, timerBonus = 0.5f, desc = "SPD +5 (+0.5 detik timer & +1 mata dadu eksplorasi)." } },
            { "acc_cloak", new ItemDef { id = "acc_cloak", name = "Cloak of the Archivist", kind = "accessory", iconKey = "monocle", price = 220, defBonus = 10, desc = "DEF +10 dan kebal terhadap efek Torn Page." } },
            { "acc_pendant", new ItemDef { id = "acc_pendant", name = "Pendant of Clarity", kind = "accessory", iconKey = "aegis_charm", price = 190, defBonus = 4, desc = "DEF +4 dan kebal terhadap efek Silence & Poison." } },

            // 6 Consumables
            { "pot_hp", new ItemDef { id = "pot_hp", name = "Crimson Ink Vial (HP)", kind = "consumable", iconKey = "potion_hp", price = 30, healHp = 65, desc = "Memulihkan +65 HP dan menghapus status Poison." } },
            { "pot_mp", new ItemDef { id = "pot_mp", name = "Azure Mana Draught (MP)", kind = "consumable", iconKey = "potion_mp", price = 35, healMp = 45, desc = "Memulihkan +45 MP dan menghapus status Silence." } },
            { "pot_elixir", new ItemDef { id = "pot_elixir", name = "Grand Archivist Elixir", kind = "consumable", iconKey = "potion_hp", price = 85, healHp = 150, healMp = 80, desc = "Memulihkan +150 HP, +80 MP, dan membersihkan semua debuff." } },
            { "scroll_clarity", new ItemDef { id = "scroll_clarity", name = "Scroll of Big-O Insight", kind = "consumable", iconKey = "monocle", price = 45, desc = "Memberikan +50 Focus untuk mempercepat Limit Break." } },
            { "golden_die", new ItemDef { id = "golden_die", name = "Loaded Golden Die", kind = "consumable", iconKey = "hourglass", price = 50, desc = "Lemparan dadu berikutnya dijamin bernilai 6 langkah." } },
            { "smoke_scroll", new ItemDef { id = "smoke_scroll", name = "Teleport Bookmark", kind = "consumable", iconKey = "aegis_charm", price = 40, desc = "Mundur dengan aman dari pertarungan non-Boss." } }
        };

        public static readonly List<CodexEntry> Codex = new List<CodexEntry>
        {
            new CodexEntry { tp = "TP-01", bloom = "C2 (Memahami)", title = "Konsep & Langkah Linear Search", complexity = "Best O(1) | Avg O(n) | Worst O(n)", summary = "Memeriksa elemen array satu per satu dari indeks 0 hingga ditemukan atau mencapai akhir array. Tidak memerlukan data terurut.", pseudocode = "for i = 0 to n-1:\n  if A[i] == target: return i\nreturn -1" },
            new CodexEntry { tp = "TP-02", bloom = "C3 (Menerapkan)", title = "Simulasi Linear Search (Ada / Absen)", complexity = "Worst Case: n perbandingan", summary = "Jika elemen target berada di indeks k, diperlukan (k+1) perbandingan. Jika tidak ada di dalam array, seluruh n elemen diperiksa.", pseudocode = "steps = 0\nfor i = 0 to n-1:\n  steps += 1\n  if A[i] == target: break" },
            new CodexEntry { tp = "TP-03", bloom = "C2 (Memahami)", title = "Prasyarat Mutlak Binary Search", complexity = "Syarat: Array WAJIB Terurut", summary = "Binary Search hanya valid jika data telah terurut (ascending/descending). Pada data acak, eliminasi separuh ruang akan membuang lokasi target yang sebenarnya.", pseudocode = "require is_sorted(A) == true\nlow = 0; high = n - 1" },
            new CodexEntry { tp = "TP-04", bloom = "C3 (Menerapkan)", title = "Eksekusi Midpoint & Eliminasi Binary Search", complexity = "Best O(1) | Worst O(log2 n)", summary = "Menghitung mid = floor((low + high) / 2). Jika A[mid] < target, geser low = mid + 1. Jika A[mid] > target, geser high = mid - 1.", pseudocode = "mid = (low + high) / 2\nif A[mid] == target: return mid\nelif A[mid] < target: low = mid + 1\nelse: high = mid - 1" },
            new CodexEntry { tp = "TP-05", bloom = "C4 (Menganalisis)", title = "Komparasi Linear vs Binary Search", complexity = "O(n) vs O(log2 n)", summary = "Untuk N = 1.024 data terurut, Linear Search butuh hingga 1.024 langkah, sedangkan Binary Search hanya butuh maksimal 10 langkah (log2 1024 = 10).", pseudocode = "Linear Max Steps = N\nBinary Max Steps = floor(log2(N)) + 1" },
            new CodexEntry { tp = "TP-06", bloom = "C3 (Menerapkan)", title = "Mekanisme Swap Bubble Sort", complexity = "Best O(n) | Avg/Worst O(n^2)", summary = "Membandingkan pasangan bersebelahan A[j] dan A[j+1]. Jika A[j] > A[j+1], tukar (swap). Setiap akhir Pass ke-k menempatkan nilai terbesar ke-(k) di ujung kanan.", pseudocode = "for j = 0 to n-1-i:\n  if A[j] > A[j+1]:\n    swap(A[j], A[j+1])" },
            new CodexEntry { tp = "TP-07", bloom = "C3 (Menerapkan)", title = "Pencarian Minimum & Swap Selection Sort", complexity = "Best/Avg/Worst O(n^2) | Swap O(n)", summary = "Mencari indeks elemen terkecil (min_idx) pada sub-array belum terurut [i..n-1], lalu menukarnya tepat satu kali dengan A[i] di akhir pass.", pseudocode = "min_idx = i\nfor j = i+1 to n-1:\n  if A[j] < A[min_idx]: min_idx = j\nswap(A[i], A[min_idx])" },
            new CodexEntry { tp = "TP-08", bloom = "C3 (Menerapkan)", title = "Penyisipan Elemen Kunci Insertion Sort", complexity = "Best O(n) [Hampir Terurut] | Worst O(n^2)", summary = "Mengambil elemen key = A[i], lalu menggeser semua elemen di sub-array kiri yang lebih besar dari key ke kanan satu posisi sebelum menyisipkan key.", pseudocode = "key = A[i]; j = i - 1\nwhile j >= 0 and A[j] > key:\n  A[j+1] = A[j]; j -= 1\nA[j+1] = key" },
            new CodexEntry { tp = "TP-09", bloom = "C4 (Menganalisis)", title = "Divide & Conquer Merge Sort", complexity = "Best/Avg/Worst O(n log n) | Space O(n)", summary = "Membagi array menjadi dua bagian secara rekursif hingga berukuran 1, lalu menggabungkan (merge) dua sub-array terurut dengan membandingkan elemen terdepan keduanya.", pseudocode = "merge(L, R):\n  while i < len(L) and j < len(R):\n    append min(L[i], R[j])" },
            new CodexEntry { tp = "TP-10", bloom = "C4 (Menganalisis)", title = "Partisi Pivot Quick Sort", complexity = "Best/Avg O(n log n) | Worst O(n^2)", summary = "Memilih elemen Pivot, lalu mempartisi array sehingga seluruh elemen <= Pivot berada di sebelah kiri dan seluruh elemen > Pivot berada di sebelah kanan.", pseudocode = "pivot = A[high]\npartition elements <= pivot to left\npartition elements > pivot to right" },
            new CodexEntry { tp = "TP-11", bloom = "C5 (Mengevaluasi)", title = "Pemilihan Algoritma Berdasarkan Konteks", complexity = "Analisis Kontekstual C5", summary = "Data hampir terurut -> Insertion Sort O(n). Minimasi jumlah penulisan memori (swap) -> Selection Sort. Dataset besar butuh jaminan stabil O(n log n) -> Merge Sort.", pseudocode = "if nearly_sorted: use InsertionSort\nelif memory_writes_costly: use SelectionSort\nelse: use MergeSort / QuickSort" },
            new CodexEntry { tp = "TP-12", bloom = "C4 (Menganalisis)", title = "Evaluasi Kompleksitas Big-O & Parsons Problem", complexity = "O(1) < O(log n) < O(n) < O(n log n) < O(n^2)", summary = "Menganalisis pertumbuhan waktu eksekusi terhadap ukuran input N dan menyusun potongan kode algoritma pencarian/pengurutan yang benar.", pseudocode = "O(log n): Binary Search\nO(n): Linear Search / Best Insertion\nO(n log n): Merge / Quick Sort\nO(n^2): Bubble / Selection / Insertion" }
        };
    }
}
