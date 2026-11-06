using System.Collections.Generic;

namespace GrimoireAegis.Core
{
    public enum Difficulty { Easy, Medium, Hard }

    public class StatusInfo
    {
        public string id;
        public string name;
        public bool buff;
        public bool stacks;        // counts stacks instead of turns (Panic, Death Seal)
        public string effect;      // one line shown in tooltips
        public string source;      // who causes it
        public string cure;        // how to remove it
    }

    /// <summary>All status effects (GDD §9, §15) with the text used by tooltips and the Guide.</summary>
    public static class GrimoireStatus
    {
        public static readonly Dictionary<string, StatusInfo> All = new Dictionary<string, StatusInfo>();

        private static void Add(string id, string name, bool buff, string effect, string source, string cure, bool stacks = false)
            => All[id] = new StatusInfo { id = id, name = name, buff = buff, effect = effect, source = source, cure = cure, stacks = stacks };

        static GrimoireStatus()
        {
            // ---- debuffs on the knight ----
            Add("poison", "Racun", false, "-5% HP maks di akhir setiap giliran musuh.", "Venomous Arachnid (Break)", "Crimson Ink Vial, Bubble Shield, Elixir, atau habis dalam 3 giliran.");
            Add("silence", "Bisu", false, "Mantra Grimoire biasa tidak bisa dipakai.", "Twin-Faced Librarian, The Blind Archivist (Break)", "Azure Mana Draught, Order Restored, atau tunggu.");
            Add("torn_page", "Halaman Robek", false, "Satu pilihan jawaban tertutup sobekan.", "Bookworm Horror (Break)", "Cloak of the Archivist (kebal), Bubble Shield.");
            Add("blind", "Buta", false, "Pilihan jawaban baru muncul 1 detik setelah soal tampil.", "Lost Wisp, Entropy (Break)", "Habis setelah 1 soal.");
            Add("buried", "Terkubur", false, "Menu Item terkunci.", "Grave Digger (Break)", "Habis setelah 1 giliran.");
            Add("crushed", "Remuk", false, "DEF turun 20%.", "The Abyss Golem (Break)", "Habis dalam 2 giliran.");
            Add("split", "Terbelah", false, "Serangan berikutnya hanya memberi 50% damage.", "Pivot Wraith (Break)", "Hilang setelah kamu menyerang sekali.");
            Add("death_seal", "Segel Kematian", false, "Tiap Break menyalakan 1 segel. 3 segel = HP tersisa 1 (bukan mati).", "The Sphinx of Entropy", "Segel direset setelah menyala penuh atau saat menang.", true);
            Add("panic", "Panik", false, "1: teks soal bergetar. 2: tepi layar gelap. 3: urutan tombol jawaban diacak.", "Setiap Break", "Perfect Guard -1 tumpukan; menang = hilang semua.", true);
            // ---- buffs on the knight ----
            Add("ward", "Insertion Ward", true, "Damage serangan berikutnya -30%.", "Mantra Insertion Lance (berhasil)", "Terpakai saat menerima serangan.");
            Add("adrenaline", "Adrenalin", true, "HP di bawah 20%: timer soal +1 detik.", "Otomatis (Mercy System)", "Hilang saat HP pulih di atas 20%.");
            Add("limit", "Limit Siap", true, "Focus 100%: mantra ORDER RESTORED bisa dirapal.", "Serangan & Perfect Guard mengisi Focus", "Terpakai saat merapal Limit Break.");
            // ---- states on monsters ----
            Add("ordered", "Terurut", false, "DEF musuh -25% dan soal berikutnya +2 detik.", "Mantra Order Restored", "Habis dalam 2 giliran.");
            Add("marked", "Ditandai", false, "Serangan berikutnya ke musuh ini kritis (x1.5).", "Mantra Selection Strike", "Terpakai saat diserang.");
            Add("armored", "Zirah Batu", true, "Serangan fisik biasa hanya memberi 30% damage. Pakai mantra Sorting!", "The Abyss Golem (pasif)", "Tidak bisa dihapus.");
            Add("regen", "Regenerasi", true, "Memulihkan 10% HP di awal giliran berikutnya.", "Hydra of Division (saat kamu Break / mantra gagal)", "Habis setelah 1 giliran.");
            Add("enraged", "Murka", true, "Fase boss baru: ATK +4 per fase, animasi & pola berubah.", "Boss di bawah 50% HP (Entropy: 66% & 33%)", "Permanen.");
            Add("fog", "Kabut", true, "Elemen ujung array tertutup kabut (kecuali MID).", "The Blind Archivist fase 2", "Permanen selama fase 2.");
            Add("false_pivot", "Pivot Palsu", true, "Break terhadap musuh ini memberi damage kritis x1.35.", "Pivot Wraith (pasif)", "Tidak bisa dihapus.");
        }

        public static StatusInfo Get(string id) => All.TryGetValue(id, out var s) ? s : null;

        /// <summary>Which status a monster inflicts on Break (GDD §9).</summary>
        public static string OnBreak(string monsterId, int bossPhase)
        {
            switch (monsterId)
            {
                case "lost_wisp": return "blind";
                case "bookworm_horror": return "torn_page";
                case "twin_faced_librarian": return "silence";
                case "the_blind_archivist": return "silence";
                case "venomous_arachnid": return "poison";
                case "grave_digger": return "buried";
                case "the_abyss_golem": return "crushed";
                case "pivot_wraith": return "split";
                case "the_sphinx_of_entropy": return "death_seal";
                case "entropy_chaos_king": return bossPhase == 1 ? "blind" : (bossPhase == 2 ? "poison" : "torn_page");
                default: return null;
            }
        }

        public static int Duration(string id)
        {
            switch (id)
            {
                case "poison": return 3;
                case "silence": return 1;
                case "torn_page": return 2;
                case "blind": return 1;
                case "buried": return 1;
                case "crushed": return 2;
                case "split": return 1;
                case "ordered": return 2;
                case "regen": return 1;
                default: return 1;
            }
        }

        /// <summary>Number of consecutive questions in one enemy attack (GDD §9).</summary>
        public static int Volley(string monsterId) =>
            monsterId == "skeleton_archer" ? 3 : (monsterId == "bookworm_horror" ? 2 : 1);

        public static string DifficultyName(Difficulty d) => d == Difficulty.Easy ? "MUDAH" : (d == Difficulty.Medium ? "SEDANG" : "SULIT");

        public static string DifficultyDesc(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Easy:
                    return "Tanpa batas waktu saat menjawab soal - pikirkan selama yang kamu mau. Jawaban benar selalu menjadi Perfect Guard. Damage Break maks 25% HP, Hint lebih murah.";
                case Difficulty.Hard:
                    return "Timer -20%, masa baca 1 detik, musuh +15% HP & ATK, Analisis tidak menampilkan topik soal. Untuk siswa yang sudah mahir.";
                default:
                    return "Timer normal (GDD mode Scholar). Benar & cepat (sisa waktu >= 60%) = Perfect Guard. Damage Break maks 50% HP.";
            }
        }
    }
}
