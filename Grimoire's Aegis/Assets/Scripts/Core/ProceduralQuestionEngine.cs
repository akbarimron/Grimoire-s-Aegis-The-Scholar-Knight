using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GrimoireAegis.Core
{
    [Serializable]
    public class QuestionPayload
    {
        public string tp;
        public string bloom;
        public string formatCode;
        public string formatName;
        public string algorithmName;
        public int[] arrayData = Array.Empty<int>();
        public int[] highlightIndices = Array.Empty<int>();
        public int lowIdx = -1;
        public int midIdx = -1;
        public int highIdx = -1;
        public int pivotIdx = -1;
        public int targetVal = -1;
        public string prompt;
        public string[] options = new string[4];
        public int correctIndex;
        public string[] misconceptionTags = new string[4];
        public string hintText;
        public string explanation;
        public float timeLimitSec;
    }

    [Serializable]
    public class TelemetryRecord
    {
        public string timestamp;
        public int act;
        public string monsterName;
        public string tp;
        public string bloom;
        public string formatCode;
        public string ddaTier;
        public bool isCorrect;
        public string defenceGrade;
        public float responseTimeSec;
        public float timeLimitSec;
        public string misconceptionTag;
    }

    public static class ProceduralQuestionEngine
    {
        private static System.Random _rng = new System.Random();

        public static float CalculateTimeLimit(string formatCode, string ddaTier, int playerSpd, float accessoryBonusSec)
        {
            float baseTime = 12.0f;
            switch (formatCode)
            {
                case "F1": baseTime = 11.0f; break;
                case "F2": baseTime = 13.0f; break;
                case "F3": baseTime = 10.0f; break;
                case "F4": baseTime = 15.0f; break;
                case "F5": baseTime = 14.0f; break;
                case "F6": baseTime = 16.0f; break;
                case "F7": baseTime = 12.0f; break;
            }

            float ddaMult = ddaTier == "Assisted" ? 1.25f : (ddaTier == "Mastery" ? 0.85f : 1.0f);
            float spdBonus = Mathf.Max(0, playerSpd - 10) * 0.12f;
            return Mathf.Clamp(baseTime * ddaMult + spdBonus + accessoryBonusSec, 7.0f, 30.0f);
        }

        public static QuestionPayload GenerateQuestion(string tp, string ddaTier, int playerSpd, float accessoryBonusSec)
        {
            int arrLen = ddaTier == "Assisted" ? 5 : (ddaTier == "Mastery" ? 7 : 6);
            QuestionPayload q;

            switch (tp)
            {
                case "TP-01": q = BuildTP01(arrLen); break;
                case "TP-02": q = BuildTP02(arrLen); break;
                case "TP-03": q = BuildTP03(arrLen); break;
                case "TP-04": q = BuildTP04(arrLen); break;
                case "TP-05": q = BuildTP05(); break;
                case "TP-06": q = BuildTP06(arrLen); break;
                case "TP-07": q = BuildTP07(arrLen); break;
                case "TP-08": q = BuildTP08(arrLen); break;
                case "TP-09": q = BuildTP09(); break;
                case "TP-10": q = BuildTP10(arrLen); break;
                case "TP-11": q = BuildTP11(); break;
                case "TP-12":
                default:
                    q = BuildTP12();
                    break;
            }

            q.timeLimitSec = CalculateTimeLimit(q.formatCode, ddaTier, playerSpd, accessoryBonusSec);
            return q;
        }

        private static int[] MakeUniqueUnsorted(int count, int minVal = 11, int maxVal = 95)
        {
            HashSet<int> set = new HashSet<int>();
            while (set.Count < count)
            {
                set.Add(_rng.Next(minVal, maxVal));
            }
            int[] arr = set.ToArray();
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                int tmp = arr[i];
                arr[i] = arr[j];
                arr[j] = tmp;
            }
            return arr;
        }

        private static int[] MakeUniqueSorted(int count, int minVal = 10, int maxVal = 92)
        {
            int[] arr = MakeUniqueUnsorted(count, minVal, maxVal);
            Array.Sort(arr);
            return arr;
        }

        private static void AssignOptions(QuestionPayload q, string correctOpt, (string opt, string tag)[] distractors)
        {
            var pool = new List<(string opt, string tag)> { (correctOpt, "CORRECT") };
            foreach (var d in distractors)
            {
                if (pool.Count >= 4) break;
                if (!pool.Any(p => p.opt == d.opt))
                {
                    pool.Add(d);
                }
            }
            while (pool.Count < 4)
            {
                pool.Add(($"Alternatif #{pool.Count}", "Random Guess / Unfamiliar Concept"));
            }

            // Fisher-Yates shuffle
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }

            for (int i = 0; i < 4; i++)
            {
                q.options[i] = pool[i].opt;
                q.misconceptionTags[i] = pool[i].tag;
                if (pool[i].tag == "CORRECT") q.correctIndex = i;
            }
        }

        private static string FormatArray(int[] arr)
        {
            return "[" + string.Join(", ", arr) + "]";
        }

        // TP-01: Konsep & Langkah Linear Search (F1: Pointer/Index Selection)
        private static QuestionPayload BuildTP01(int n)
        {
            int[] arr = MakeUniqueUnsorted(n);
            int curIdx = _rng.Next(0, n - 2);
            int targetIdx = _rng.Next(curIdx + 1, n);
            int target = arr[targetIdx];

            var q = new QuestionPayload
            {
                tp = "TP-01",
                bloom = "C2",
                formatCode = "F1",
                formatName = "F1: Pointer/Index Selection",
                algorithmName = "Linear Search",
                arrayData = arr,
                highlightIndices = new[] { curIdx },
                targetVal = target,
                prompt = $"Linear Search mencari Target = {target}. Saat ini pointer berada di indeks [{curIdx}] (nilai {arr[curIdx]} != {target}). Indeks manakah yang diperiksa TEPAT berikutnya?",
                hintText = "Linear Search memeriksa elemen secara berurutan dari kiri ke kanan (indeks bertambah +1 setiap langkah).",
                explanation = $"Karena indeks [{curIdx}] belum cocok, Linear Search bergeser 1 langkah ke kanan menuju indeks [{curIdx + 1}] (nilai {arr[curIdx + 1]})."
            };

            string correct = $"Indeks [{curIdx + 1}] (nilai {arr[curIdx + 1]})";
            var dist = new (string, string)[]
            {
                ($"Indeks [{targetIdx}] langsung (nilai {arr[targetIdx]})", "Miskonsepsi: Melompat langsung ke target tanpa iterasi sekuensial"),
                ($"Indeks [{(n - 1) / 2}] (titik tengah / Mid)", "Miskonsepsi: Mencampuradukkan Linear Search dengan Binary Search"),
                ($"Indeks [0] (ulangi dari awal)", "Miskonsepsi: Mereset pointer pencarian pada setiap kegagalan komparasi")
            };
            AssignOptions(q, correct, dist);
            return q;
        }

        // TP-02: Simulasi Linear Search Ada/Absen (F2: Next-Step / Comparison Count)
        private static QuestionPayload BuildTP02(int n)
        {
            int[] arr = MakeUniqueUnsorted(n);
            bool present = _rng.NextDouble() < 0.65;
            int targetIdx = present ? _rng.Next(1, n) : -1;
            int target = present ? arr[targetIdx] : 99;
            int steps = present ? (targetIdx + 1) : n;

            var q = new QuestionPayload
            {
                tp = "TP-02",
                bloom = "C3",
                formatCode = "F2",
                formatName = "F2: Step Count Prediction",
                algorithmName = "Linear Search",
                arrayData = arr,
                highlightIndices = present ? new[] { targetIdx } : Array.Empty<int>(),
                targetVal = target,
                prompt = present
                    ? $"Berapa total perbandingan (comparisons) yang dilakukan Linear Search dari indeks [0] hingga menemukan Target = {target}?"
                    : $"Target = {target} TIDAK ADA di dalam array berjumlah {n} elemen ini. Berapa perbandingan yang dilakukan Linear Search sebelum menyimpulkan -1 (Not Found)?",
                hintText = "Ingat: Indeks dimulai dari 0, sehingga elemen di indeks k membutuhkan (k + 1) kali pemeriksaan.",
                explanation = present
                    ? $"Target {target} berada di indeks [{targetIdx}]. Karena pemeriksaan dimulai dari indeks [0], total perbandingan adalah {targetIdx} + 1 = {steps} kali."
                    : $"Saat elemen absen, Linear Search wajib memeriksa seluruh {n} elemen dari indeks [0] sampai [{n - 1}] ({n} perbandingan)."
            };

            string correct = present ? $"{steps} perbandingan (berhenti di indeks [{targetIdx}])" : $"{n} perbandingan (seluruh elemen diperiksa)";
            var dist = new (string, string)[]
            {
                (present ? $"{targetIdx} perbandingan (off-by-one)" : $"{n - 1} perbandingan", "Miskonsepsi Off-by-One: Menganggap jumlah langkah sama dengan nomor indeks 0-based"),
                ($"1 perbandingan langsung", "Miskonsepsi: Mengira komputer mengetahui posisi elemen secara instan O(1)"),
                ($"{Mathf.CeilToInt(Mathf.Log(n, 2))} perbandingan (log2 N)", "Miskonsepsi: Menggunakan rumus langkah Binary Search untuk Linear Search")
            };
            AssignOptions(q, correct, dist);
            return q;
        }

        // TP-03: Prasyarat Binary Search (F5: Algorithm Evaluation)
        private static QuestionPayload BuildTP03(int n)
        {
            int[] arr = MakeUniqueUnsorted(n);
            int target = arr[_rng.Next(0, n)];
            int mid = (n - 1) / 2;

            var q = new QuestionPayload
            {
                tp = "TP-03",
                bloom = "C2",
                formatCode = "F5",
                formatName = "F5: Prerequisite Verification",
                algorithmName = "Binary Search Check",
                arrayData = arr,
                lowIdx = 0,
                midIdx = mid,
                highIdx = n - 1,
                targetVal = target,
                prompt = $"Perhatikan array di atas untuk mencari Target = {target}. Mengapa Binary Search TIDAK BOLEH langsung dijalankan pada array ini?",
                hintText = "Periksa apakah elemen-elemen array dari indeks [0] hingga akhir sudah berurutan naik/turun.",
                explanation = "Prasyarat mutlak Binary Search adalah data WAJIB terurut (sorted). Jika tidak terurut, membuang separuh ruang kiri/kanan berdasarkan A[mid] akan salah mengeliminasi posisi target."
            };

            string correct = "Karena elemen array belum terurut (unsorted), sehingga eliminasi separuh ruang tidak valid";
            var dist = new (string, string)[]
            {
                ("Karena jumlah elemen array bukan kelipatan pangkat dua", "Miskonsepsi: Mengira Binary Search hanya bekerja pada panjang array genap/pangkat 2"),
                ($"Karena nilai Mid = {arr[mid]} adalah bilangan ganjil/genap", "Miskonsepsi: Mengira nilai elemen tengah membatasi jalannya Binary Search"),
                ("Binary Search tetap bisa langsung dipakai tanpa syarat apa pun", "Miskonsepsi Fatal: Mengabaikan prasyarat keterurutan data pada Binary Search")
            };
            AssignOptions(q, correct, dist);
            return q;
        }

        // TP-04: Eksekusi Midpoint & Eliminasi Binary Search (F2: Next-Step Prediction)
        private static QuestionPayload BuildTP04(int n)
        {
            int[] arr = MakeUniqueSorted(n);
            int low = 0;
            int high = n - 1;
            int mid = (low + high) / 2;
            bool goRight = _rng.NextDouble() < 0.5;
            int targetIdx = goRight ? _rng.Next(mid + 1, n) : _rng.Next(0, mid);
            int target = arr[targetIdx];

            var q = new QuestionPayload
            {
                tp = "TP-04",
                bloom = "C3",
                formatCode = "F2",
                formatName = "F2: Next-Step Prediction",
                algorithmName = "Binary Search",
                arrayData = arr,
                lowIdx = low,
                midIdx = mid,
                highIdx = high,
                highlightIndices = new[] { mid },
                targetVal = target,
                prompt = $"Binary Search pada array terurut: Low=[{low}], High=[{high}], Mid=[{mid}] (nilai {arr[mid]}). Untuk mencari Target = {target}, bagaimana pembaruan batas pencarian berikutnya?",
                hintText = $"Bandingkan Target ({target}) dengan A[Mid] ({arr[mid]}). Jika Target > A[Mid], cari di kanan (Low = Mid + 1). Jika Target < A[Mid], cari di kiri (High = Mid - 1).",
                explanation = goRight
                    ? $"Karena Target ({target}) > A[Mid] ({arr[mid]}), separuh kiri beserta Mid dieliminasi. Batas baru: Low = Mid + 1 = {mid + 1}, High tetap {high}."
                    : $"Karena Target ({target}) < A[Mid] ({arr[mid]}), separuh kanan beserta Mid dieliminasi. Batas baru: Low tetap {low}, High = Mid - 1 = {mid - 1}."
            };

            string correct = goRight
                ? $"Geser Low ke [{mid + 1}], High tetap [{high}] (cari di separuh kanan)"
                : $"Low tetap [{low}], geser High ke [{mid - 1}] (cari di separuh kiri)";

            var dist = new (string, string)[]
            {
                (goRight
                    ? $"Low tetap [{low}], geser High ke [{mid - 1}] (cari di separuh kiri)"
                    : $"Geser Low ke [{mid + 1}], High tetap [{high}] (cari di separuh kanan)",
                    "Miskonsepsi Arah Eliminasi: Terbalik membuang sub-array tempat target berada"),
                ($"Geser Low ke [{low + 1}] satu langkah seperti Linear Search", "Miskonsepsi: Menggeser pointer satu per satu alih-alih membagi dua ruang pencarian"),
                ($"Tetapkan Mid baru = [{high}] tanpa mengubah Low/High", "Miskonsepsi Rumus Midpoint: Salah memperbarui batas Low/High")
            };
            AssignOptions(q, correct, dist);
            return q;
        }

        // TP-05: Komparasi Linear vs Binary Search (F7: Complexity Comparison)
        private static QuestionPayload BuildTP05()
        {
            int[] powers = { 64, 128, 256, 512, 1024 };
            int powIdx = _rng.Next(powers.Length);
            int N = powers[powIdx];
            int logN = (int)Mathf.Round(Mathf.Log(N, 2));

            var q = new QuestionPayload
            {
                tp = "TP-05",
                bloom = "C4",
                formatCode = "F7",
                formatName = "F7: Complexity Comparison",
                algorithmName = "Linear vs Binary Search",
                arrayData = new[] { 12, 24, 36, 48, 60, 72 },
                prompt = $"Sebuah katalog perpustakaan memiliki N = {N} buku yang SUDAH TERURUT. Pada skenario terburuk (Worst Case), berapa perbandingan maksimal Linear Search dibanding Binary Search?",
                hintText = $"Linear Search Worst Case = N langkah. Binary Search Worst Case ≈ log2(N) + 1 langkah (2^{logN} = {N}).",
                explanation = $"Pada N = {N} data terurut, Linear Search O(n) memerlukan hingga {N} perbandingan, sedangkan Binary Search O(log2 n) hanya memerlukan sekitar {logN} hingga {logN + 1} perbandingan."
            };

            string correct = $"Linear Search: {N} langkah vs Binary Search: ~{logN + 1} langkah (O(n) vs O(log n))";
            var dist = new (string, string)[]
            {
                ($"Linear Search: ~{logN + 1} langkah vs Binary Search: {N} langkah", "Miskonsepsi Terbalik: Menukar efisiensi Linear Search dengan Binary Search"),
                ($"Keduanya sama-sama membutuhkan {N / 2} langkah pada kasus terburuk", "Miskonsepsi: Mengira Binary Search hanya membagi 2 satu kali (N/2)"),
                ($"Linear Search: {N} langkah vs Binary Search: {N * N} langkah", "Miskonsepsi Big-O: Mengira Binary Search memiliki kompleksitas kuadratik O(n^2)")
            };
            AssignOptions(q, correct, dist);
            return q;
        }

        // TP-06: Bubble Sort Swap & Pass (F3: Swap/No-Swap Decision)
        private static QuestionPayload BuildTP06(int n)
        {
            int[] arr = MakeUniqueUnsorted(n);
            int j = _rng.Next(0, n - 1);
            bool shouldSwap = arr[j] > arr[j + 1];

            var q = new QuestionPayload
            {
                tp = "TP-06",
                bloom = "C3",
                formatCode = "F3",
                formatName = "F3: Swap / No-Swap Decision",
                algorithmName = "Bubble Sort (Ascending)",
                arrayData = arr,
                highlightIndices = new[] { j, j + 1 },
                prompt = $"Bubble Sort (Ascending) membandingkan pasangan bersebelahan indeks [{j}] ({arr[j]}) dan [{j + 1}] ({arr[j + 1]}). Apa tindakan yang tepat?",
                hintText = "Pada pengurutan menaik (Ascending), Bubble Sort menukar (SWAP) jika elemen kiri > elemen kanan (A[j] > A[j+1]).",
                explanation = shouldSwap
                    ? $"Karena A[{j}] ({arr[j]}) > A[{j + 1}] ({arr[j + 1]}), kedua elemen WAJIB ditukar (SWAP) agar nilai lebih besar menggelembung ke kanan."
                    : $"Karena A[{j}] ({arr[j]}) <= A[{j + 1}] ({arr[j + 1]}), urutan pasangan ini sudah benar sehingga TIDAK perlu ditukar (NO SWAP)."
            };

            string correct = shouldSwap
                ? $"SWAP {arr[j]} dan {arr[j + 1]} karena {arr[j]} > {arr[j + 1]}"
                : $"NO SWAP (biarkan tetap) karena {arr[j]} < {arr[j + 1]}";

            var dist = new (string, string)[]
            {
                (shouldSwap
                    ? $"NO SWAP karena {arr[j]} > {arr[j + 1]} sudah urut"
                    : $"SWAP {arr[j]} dan {arr[j + 1]} agar urut menurun",
                    "Miskonsepsi Arah Urutan: Terbalik menerapkan kondisi pertukaran Ascending vs Descending"),
                ($"Tukar A[{j}] ({arr[j]}) langsung dengan elemen paling ujung kanan A[{n - 1}] ({arr[n - 1]})", "Miskonsepsi Bubble Sort: Menukar elemen yang tidak bersebelahan"),
                ($"Hapus elemen {arr[j]} dari array", "Miskonsepsi: Mengira proses sorting menghapus elemen array")
            };
            AssignOptions(q, correct, dist);
            return q;
        }

        // TP-07: Selection Sort Minimum & Swap (F1: Minimum Index Selection)
        private static QuestionPayload BuildTP07(int n)
        {
            int[] arr = MakeUniqueUnsorted(n);
            int passStart = _rng.Next(0, 2);
            if (passStart == 1)
            {
                // Make index 0 the global minimum already
                int minAll = 0;
                for (int k = 1; k < n; k++) if (arr[k] < arr[minAll]) minAll = k;
                int t = arr[0]; arr[0] = arr[minAll]; arr[minAll] = t;
            }

            int minIdx = passStart;
            for (int k = passStart + 1; k < n; k++)
            {
                if (arr[k] < arr[minIdx]) minIdx = k;
            }

            var q = new QuestionPayload
            {
                tp = "TP-07",
                bloom = "C3",
                formatCode = "F1",
                formatName = "F1: Minimum & Swap Selection",
                algorithmName = "Selection Sort",
                arrayData = arr,
                lowIdx = passStart,
                highlightIndices = new[] { passStart, minIdx },
                prompt = $"Selection Sort memulai iterasi pada indeks i = [{passStart}] (sub-array belum terurut [{passStart}..{n - 1}]). Pertukaran (swap) manakah yang dieksekusi di akhir pass ini?",
                hintText = $"Cari nilai terkecil (minimum) di antara indeks [{passStart}] sampai [{n - 1}], lalu tukarkan posisinya dengan A[{passStart}].",
                explanation = $"Nilai minimum pada rentang [{passStart}..{n - 1}] adalah {arr[minIdx]} di indeks [{minIdx}]. Selection Sort menukarnya dengan elemen di posisi awal pass A[{passStart}] ({arr[passStart]})."
            };

            string correct = minIdx == passStart
                ? $"Min adalah {arr[minIdx]} di [{minIdx}]; tetap di posisi [{passStart}] (swap dengan dirinya sendiri)"
                : $"Tukar A[{passStart}] ({arr[passStart]}) dengan nilai minimum A[{minIdx}] ({arr[minIdx]})";

            int maxIdx = passStart;
            for (int k = passStart + 1; k < n; k++) if (arr[k] > arr[maxIdx]) maxIdx = k;

            var dist = new (string, string)[]
            {
                ($"Tukar A[{passStart}] ({arr[passStart]}) dengan nilai terbesar A[{maxIdx}] ({arr[maxIdx]})", "Miskonsepsi Selection Sort: Memilih elemen maksimum alih-alih minimum pada Ascending Sort"),
                ($"Tukar berpasangan A[{passStart}] dengan A[{passStart + 1}] ({arr[passStart + 1]}) saja", "Miskonsepsi: Mencampuradukkan Selection Sort dengan Bubble Sort"),
                ($"Geser semua elemen tanpa mencari nilai minimum", "Miskonsepsi: Mencampuradukkan Selection Sort dengan Insertion Sort")
            };
            AssignOptions(q, correct, dist);
            return q;
        }

        // TP-08: Insertion Sort Key Insertion (F4: Pass Outcome)
        private static QuestionPayload BuildTP08(int n)
        {
            int[] arr = MakeUniqueUnsorted(n);
            // Sort first 3 elements so prefix [0..2] is sorted!
            Array.Sort(arr, 0, 3);
            // Make sure arr[3] is smaller than arr[2] so an actual insertion shift occurs
            if (arr[3] > arr[2])
            {
                int tmp = arr[3];
                arr[3] = arr[0] - 3;
                arr[0] = tmp;
                Array.Sort(arr, 0, 3);
            }

            int keyIdx = 3;
            int keyVal = arr[keyIdx];
            int[] afterPass = (int[])arr.Clone();
            int j = keyIdx - 1;
            while (j >= 0 && afterPass[j] > keyVal)
            {
                afterPass[j + 1] = afterPass[j];
                j--;
            }
            afterPass[j + 1] = keyVal;

            var q = new QuestionPayload
            {
                tp = "TP-08",
                bloom = "C3",
                formatCode = "F4",
                formatName = "F4: Pass Outcome",
                algorithmName = "Insertion Sort",
                arrayData = arr,
                highlightIndices = new[] { keyIdx },
                prompt = $"Insertion Sort telah mengurutkan sub-array kiri [0..2]. Sekarang giliran menyisipkan Key = A[3] ({keyVal}) ke posisi yang tepat di sebelah kiri. Bagaimana wujud array setelah Key disisipkan?",
                hintText = $"Geser semua elemen di [0..2] yang lebih besar dari {keyVal} satu langkah ke kanan, lalu tempatkan {keyVal} di celah yang terbentuk.",
                explanation = $"Elemen di sub-array kiri [0..2] yang lebih besar dari {keyVal} digeser ke kanan, sehingga {keyVal} menempati indeks [{j + 1}] dan menghasilkan {FormatArray(afterPass)}."
            };

            int[] wrongSwapOnly = (int[])arr.Clone();
            wrongSwapOnly[keyIdx] = wrongSwapOnly[0];
            wrongSwapOnly[0] = keyVal;

            int[] wrongAdjSwap = (int[])arr.Clone();
            wrongAdjSwap[keyIdx] = wrongAdjSwap[keyIdx - 1];
            wrongAdjSwap[keyIdx - 1] = keyVal;

            string correct = FormatArray(afterPass);
            var dist = new (string, string)[]
            {
                (FormatArray(wrongSwapOnly), "Miskonsepsi Insertion Sort: Menukar langsung Key dengan indeks [0] tanpa menggeser (shift) elemen di antaranya"),
                (FormatArray(wrongAdjSwap), "Miskonsepsi: Hanya menukar 1 langkah ke kiri tanpa melanjutkan hingga posisi terurut"),
                (FormatArray(arr), "Miskonsepsi: Tidak menggeser elemen yang lebih besar dari Key")
            };
            AssignOptions(q, correct, dist);
            return q;
        }

        // TP-09: Merge Sort Divide & Conquer (F4: Merge Outcome)
        private static QuestionPayload BuildTP09()
        {
            int[] baseArr = MakeUniqueSorted(6);
            // Split into 2 sorted halves of 3 elements interleaved
            int[] left = new[] { baseArr[0], baseArr[2], baseArr[4] };
            int[] right = new[] { baseArr[1], baseArr[3], baseArr[5] };
            int[] combinedDisplay = new[] { left[0], left[1], left[2], right[0], right[1], right[2] };

            var q = new QuestionPayload
            {
                tp = "TP-09",
                bloom = "C4",
                formatCode = "F4",
                formatName = "F4: Merge Step Outcome",
                algorithmName = "Merge Sort",
                arrayData = combinedDisplay,
                lowIdx = 0,
                midIdx = 2,
                highIdx = 5,
                highlightIndices = new[] { 0, 3 },
                prompt = $"Tahap Conquer Merge Sort menggabungkan sub-array Kiri L = {FormatArray(left)} dan Kanan R = {FormatArray(right)}. Apa urutan 3 elemen PERTAMA yang masuk ke array hasil Merge?",
                hintText = "Bandingkan kepala L dan kepala R secara berulang; ambil nilai yang lebih kecil dan majukan pointernya.",
                explanation = $"Merge membandingkan L[0]({left[0]}) vs R[0]({right[0]}) -> ambil {baseArr[0]}, lalu menghasilkan 3 elemen pertama: [{baseArr[0]}, {baseArr[1]}, {baseArr[2]}]."
            };

            string correct = $"[{baseArr[0]}, {baseArr[1]}, {baseArr[2]}] (perbandingan pointer kepala L dan R)";
            var dist = new (string, string)[]
            {
                ($"[{left[0]}, {left[1]}, {left[2]}] (salin seluruh L dulu baru R)", "Miskonsepsi Merge Sort: Menggabungkan (concatenate) tanpa membandingkan elemen L dan R"),
                ($"[{right[0]}, {right[1]}, {right[2]}] (salin seluruh R dulu)", "Miskonsepsi Merge Sort: Menyalin sub-array kanan tanpa perbandingan two-pointer"),
                ($"[{baseArr[5]}, {baseArr[4]}, {baseArr[3]}] (urutan terbalik)", "Miskonsepsi: Mengambil elemen terbesar terlebih dahulu pada pengurutan Ascending")
            };
            AssignOptions(q, correct, dist);
            return q;
        }

        // TP-10: Quick Sort Partitioning (F4: Partition Outcome)
        private static QuestionPayload BuildTP10(int n)
        {
            int[] sorted = MakeUniqueSorted(n);
            int pivotVal = sorted[n / 2];
            int[] arr = (int[])sorted.Clone();
            // Shuffle but put pivotVal at last index (high = n-1)
            var others = sorted.Where(v => v != pivotVal).OrderBy(_ => _rng.Next()).ToList();
            for (int i = 0; i < others.Count; i++) arr[i] = others[i];
            arr[n - 1] = pivotVal;

            var leftPart = others.Where(v => v < pivotVal).ToList();
            var rightPart = others.Where(v => v > pivotVal).ToList();

            var q = new QuestionPayload
            {
                tp = "TP-10",
                bloom = "C4",
                formatCode = "F4",
                formatName = "F4: Pivot Partition Outcome",
                algorithmName = "Quick Sort",
                arrayData = arr,
                pivotIdx = n - 1,
                highlightIndices = new[] { n - 1 },
                prompt = $"Quick Sort memilih elemen terakhir A[{n - 1}] = {pivotVal} sebagai PIVOT. Setelah proses Partition selesai, manakah pembagian sub-array Kiri (< Pivot) dan Kanan (> Pivot) yang benar?",
                hintText = $"Semua elemen yang kurang dari {pivotVal} harus berada di sebelah kiri Pivot, dan semua elemen yang lebih besar dari {pivotVal} di sebelah kanan Pivot.",
                explanation = $"Dengan Pivot = {pivotVal}, elemen < {pivotVal} adalah [{string.Join(", ", leftPart)}] di kiri, dan elemen > {pivotVal} adalah [{string.Join(", ", rightPart)}] di kanan."
            };

            string correct = $"Kiri (<{pivotVal}): [{string.Join(", ", leftPart)}] | Pivot: {pivotVal} | Kanan (>{pivotVal}): [{string.Join(", ", rightPart)}]";
            var dist = new (string, string)[]
            {
                ($"Kiri: [{string.Join(", ", rightPart)}] | Pivot: {pivotVal} | Kanan: [{string.Join(", ", leftPart)}]", "Miskonsepsi Partisi: Menempatkan elemen > Pivot di kiri dan < Pivot di kanan"),
                ($"Membagi array tepat di indeks tengah tanpa memeriksa nilai Pivot {pivotVal}", "Miskonsepsi: Mencampuradukkan partisi Quick Sort dengan pembelahan tengah Merge Sort"),
                ($"Pivot {pivotVal} tetap di ujung kanan tanpa memindahkan elemen yang lebih besar", "Miskonsepsi: Tidak menempatkan Pivot pada posisi finalnya setelah partisi")
            };
            AssignOptions(q, correct, dist);
            return q;
        }

        // TP-11: Evaluasi Kontekstual Pemilihan Algoritma (F5: Algorithm Match C5)
        private static QuestionPayload BuildTP11()
        {
            int scenario = _rng.Next(0, 3);
            var q = new QuestionPayload
            {
                tp = "TP-11",
                bloom = "C5",
                formatCode = "F5",
                formatName = "F5: Contextual Algorithm Match",
                algorithmName = "Algorithm Evaluation (C5)",
                arrayData = new[] { 10, 20, 30, 45, 40, 60 }
            };

            if (scenario == 0)
            {
                q.prompt = "Skenario Arsip: Terdapat 5.000 log transaksi yang 99% SUDAH TERURUT (hanya 3 data terakhir yang sedikit bergeser). Algoritma pengurutan manakah yang paling efisien mendekati O(n)?";
                q.hintText = "Cari algoritma yang hanya menggeser elemen bila menemukan ketidakteraturan dan langsung lanjut dalam O(1) bila elemen sudah >= elemen sebelumnya.";
                q.explanation = "Insertion Sort memiliki kompleksitas Best Case O(n) pada data yang sudah atau hampir terurut (nearly sorted), jauh mengungguli Selection Sort O(n^2).";
                AssignOptions(q,
                    "Insertion Sort — Best Case O(n) pada data yang hampir terurut",
                    new (string, string)[]
                    {
                        ("Selection Sort — karena selalu memindai minimum dalam O(n)", "Miskonsepsi: Selection Sort tetap O(n^2) meskipun data sudah terurut"),
                        ("Binary Search — karena langsung mengurutkan data dalam O(log n)", "Miskonsepsi Kategori: Binary Search adalah algoritma pencarian, bukan pengurutan"),
                        ("Bubble Sort tanpa flag — karena jumlah perbandingannya paling sedikit", "Miskonsepsi: Bubble Sort standar tetap melakukan O(n^2) perbandingan")
                    });
            }
            else if (scenario == 1)
            {
                q.prompt = "Skenario Katakomba: Memori kristal kuno memiliki batas usia tulis (write/swap sangat mahal), tetapi operasi baca/perbandingan sangat murah. Algoritma O(n^2) manakah yang meminimalkan jumlah SWAP maksimal O(n) kali?";
                q.hintText = "Ingat algoritma yang hanya melakukan tepat 1 kali swap di akhir setiap pass setelah menemukan indeks minimum.";
                q.explanation = "Selection Sort hanya melakukan maksimal (n - 1) kali pertukaran (O(n) swaps), sedangkan Bubble dan Insertion Sort dapat melakukan hingga O(n^2) penulisan memori.";
                AssignOptions(q,
                    "Selection Sort — hanya melakukan maksimal (n - 1) kali swap (O(n) tulis memori)",
                    new (string, string)[]
                    {
                        ("Bubble Sort — karena hanya menukar elemen bersebelahan", "Miskonsepsi: Bubble Sort justru melakukan sangat banyak swap O(n^2)"),
                        ("Insertion Sort — karena menggeser elemen tidak dihitung sebagai penulisan memori", "Miskonsepsi: Setiap pergeseran (shift) tetap menulis ke memori O(n^2)"),
                        ("Linear Search — karena mengurutkan tanpa swap", "Miskonsepsi Kategori: Linear Search tidak mengurutkan data")
                    });
            }
            else
            {
                q.prompt = "Skenario Menara: Katalog kerajaan berisi N = 100.000 gulungan acak dan menuntut jaminan waktu Worst-Case O(n log n) serta sifat STABIL (urutan data bernilai sama terjaga). Algoritma manakah yang paling tepat?";
                q.hintText = "Pilih algoritma Divide & Conquer yang selalu membagi dua sama rata dan menggabungkan secara stabil.";
                q.explanation = "Merge Sort menjamin kompleksitas waktu O(n log n) pada Best, Average, maupun Worst Case serta bersifat stabil (stable sort).";
                AssignOptions(q,
                    "Merge Sort — menjamin Worst-Case O(n log n) dan bersifat stabil",
                    new (string, string)[]
                    {
                        ("Bubble Sort — karena paling stabil untuk 100.000 data acak", "Miskonsepsi Skala: Bubble Sort O(n^2) membutuhkan ~10 miliar operasi untuk N=100.000"),
                        ("Selection Sort — karena tidak butuh memori tambahan dan O(n log n)", "Miskonsepsi Kompleksitas: Selection Sort berkinerja O(n^2) dan tidak stabil"),
                        ("Linear Search — karena bekerja pada data acak dalam O(n log n)", "Miskonsepsi: Linear Search adalah algoritma pencarian O(n)")
                    });
            }

            return q;
        }

        // TP-12: Parsons Problem & Big-O Complexity (F6 / F7)
        private static QuestionPayload BuildTP12()
        {
            bool useParsons = _rng.NextDouble() < 0.55;
            if (useParsons)
            {
                var q = new QuestionPayload
                {
                    tp = "TP-12",
                    bloom = "C4",
                    formatCode = "F6",
                    formatName = "F6: Parsons Code Completion",
                    algorithmName = "Binary Search Pseudocode",
                    arrayData = new[] { 14, 28, 42, 56, 70, 84 },
                    lowIdx = 0,
                    midIdx = 2,
                    highIdx = 5,
                    prompt = "Lengkapi potongan kode (Parsons Problem) Binary Search berikut saat A[mid] < target:\n  mid = (low + high) / 2\n  if A[mid] == target: return mid\n  elif A[mid] < target: ____________",
                    hintText = "Jika nilai tengah A[mid] lebih kecil dari target, berarti target berada di sebelah kanan mid.",
                    explanation = "Ketika A[mid] < target, seluruh elemen di kiri beserta indeks mid terlalu kecil, sehingga batas bawah diperbarui menjadi low = mid + 1."
                };

                AssignOptions(q,
                    "low = mid + 1  (eliminasi separuh kiri termasuk mid)",
                    new (string, string)[]
                    {
                        ("high = mid - 1  (eliminasi separuh kanan)", "Miskonsepsi Logika Percabangan: Memperbarui high saat A[mid] < target"),
                        ("low = low + 1  (geser batas bawah satu langkah)", "Miskonsepsi: Mengubah Binary Search menjadi iterasi linier O(n)"),
                        ("return mid - 1  (langsung hentikan fungsi)", "Miskonsepsi Alur Kontrol: Menghentikan loop pencarian sebelum target ditemukan")
                    });
                return q;
            }
            else
            {
                var q = new QuestionPayload
                {
                    tp = "TP-12",
                    bloom = "C4",
                    formatCode = "F7",
                    formatName = "F7: Big-O Complexity Ranking",
                    algorithmName = "Big-O Complexity Analysis",
                    arrayData = new[] { 1, 2, 4, 8, 16, 32 },
                    prompt = "Urutkan kelas kompleksitas waktu Big-O berikut dari yang PALING CEPAT (efisien) hingga PALING LAMBAT seiring bertambahnya ukuran data N yang sangat besar!",
                    hintText = "Ingat pertumbuhan fungsi: Konstan < Logaritmik < Linier < Linearitmik < Kuadratik.",
                    explanation = "Urutan efisiensi asimtotik dari tercepat ke terlambat adalah O(1) < O(log n) < O(n) < O(n log n) < O(n^2)."
                };

                AssignOptions(q,
                    "O(1) < O(log n) < O(n) < O(n log n) < O(n^2)",
                    new (string, string)[]
                    {
                        ("O(1) < O(n) < O(log n) < O(n^2) < O(n log n)", "Miskonsepsi Big-O: Mengira O(log n) lebih lambat daripada O(n)"),
                        ("O(n^2) < O(n log n) < O(n) < O(log n) < O(1)", "Miskonsepsi Urutan: Mengurutkan dari paling lambat ke paling cepat"),
                        ("O(log n) < O(1) < O(n log n) < O(n) < O(n^2)", "Miskonsepsi: Mengira O(log n) lebih cepat dari waktu konstan O(1)")
                    });
                return q;
            }
        }

        public static string ExportTelemetryToCsv(List<TelemetryRecord> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Act,Monster,TP,Bloom,Format,DDA_Tier,Is_Correct,Defence_Grade,Response_Time_Sec,Time_Limit_Sec,Diagnostic_Misconception");
            foreach (var r in records)
            {
                string safeMis = (r.misconceptionTag ?? "").Replace(",", ";");
                string safeMon = (r.monsterName ?? "").Replace(",", " ");
                sb.AppendLine($"{r.timestamp},{r.act},{safeMon},{r.tp},{r.bloom},{r.formatCode},{r.ddaTier},{r.isCorrect},{r.defenceGrade},{r.responseTimeSec:F2},{r.timeLimitSec:F2},{safeMis}");
            }
            return sb.ToString();
        }
    }
}
