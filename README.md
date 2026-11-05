# Grimoire's Aegis: The Scholar Knight (*The Order of Lost Indexes*)

> **2D Turn-Based RPG / Educational Puzzle Adventure / Labyrinth Board & Dungeon Crawler**  
> **Engines:** **Unity 6 (`6000.3.7f1`, URP 2D — `Grimoire's Aegis/`)** & **Godot Engine 4.7+**  
> **Art & Gameplay Reference:** Pixel-Art Region-Shaded Pipeline & Labyrinth Board Architecture (`Lost in Multiverse RPG`)  
> **Mata Kuliah:** Pengembangan Alat Permainan Edukatif — **Kelompok 3**

---

## Tim Pengembang (Kelompok 3)

| Nama Mahasiswa | NIM | Peran |
| :--- | :---: | :--- |
| **Ariel Hidayatul Faqih** | `2401265` | Game Designer & Systems Balancing |
| **Muhammad Akbar Imron** | `2404565` | Lead Programmer & Engine Architect |
| **Muhammad Raditya Santosa** | `2409461` | Gameplay & UI/UX Programmer |
| **Khairatul Husna Tartila** | `2400494` | Instructional Designer & Curriculum Specialist |
| **Septin Sehawati** | `2400923` | QA Lead, Art Direction & Educational Analytics |

---

## Ringkasan Permainan

Di kerajaan **Technosophia**, perpustakaan agung **The Grand Library of Alexandria-Nova** diserang oleh entitas kekacauan bernama **Entropy, The Chaos King**, yang menghancurkan *The Great Index* dan mengubah arsip pengetahuan menjadi monster liar. Pemain berperan sebagai **Aegis, The Scholar Knight**, ksatria cendekiawan yang menggunakan perisai suci **Aegis Shield** dan buku mantra **Grimoire** untuk memulihkan keteraturan data melalui algoritma **Pencarian (*Searching*)** dan **Pengurutan (*Sorting*)**.

### Dokumentasi Utama di Repositori Ini
- [**GDD.md**](./GDD.md) — *Game Design Document* lengkap (18 Bagian) sesuai dokumen resmi Kelompok 3.
- [**IMPLEMENTATION_PLAN.md**](./IMPLEMENTATION_PLAN.md) — *Master Technical & Pedagogical Implementation Plan* (Arsitektur Godot 4.7, FSM, Rumus Matematika, Matriks 12 TP, 13 Monster, DDA, & Roadmap).

---

## Fitur Utama yang Diimplementasikan

1. **Mekanika Inti *Active Defend* (Aegis Shield):**
   - **Perfect Parry** (Jawab Benar & Sisa Waktu $\ge 50\%$): **100% Mitigasi Damage (0 Damage)** + *Counter-Attack* $1.5\times \text{ATK}$ + $+25\text{ Focus}$ + $+15\%\text{ EXP}$.
   - **Solid Block** (Jawab Benar & Sisa Waktu $< 50\%$): **75% Mitigasi Damage** + $+10\text{ Focus}$.
   - **Guard Broken** (Jawab Salah / Waktu Habis): **0% Mitigasi (100% Damage Masuk)** + terkena status negatif monster + umpan balik diagnosis miskonsepsi.
2. **8 Mantra Algoritma Grimoire + Limit Break:**
   - *Linear Slash* (`TP-01`), *Binary Pierce* (`TP-03/04`), *Bubble Shield* (`TP-06`), *Selection Strike* (`TP-07`), *Insertion Lance* (`TP-08`), *Merge Tempest* (`TP-09`), *Quick Judgment* (`TP-10`), dan Limit Break **Order Restored** (`100 Focus`, $3.5\times$ *Pure Damage* + *Heal*).
3. **Mesin Soal Prosedural (`TP-01` s.d. `TP-12` & Format `F1` s.d. `F7`):**
   - Setiap soal dibangkitkan secara prosedural dengan *Random Seed* unik dan pengecoh (*distractor*) berbasis miskonsepsi nyata siswa.
4. **13 Monster Bestiary Lengkap (3 Act Dungeon Crawler):**
   - **Act I (*The Dusty Archives — Searching*):** *Corrupted Slime*, *Lost Wisp*, *Bookworm Horror*, *Twin-Faced Librarian* (Mid-Boss), **The Blind Archivist** (Act I Boss).
   - **Act II (*The Catacombs of Chaos — Basic Sorting*):** *Skeleton Archer*, *Venomous Arachnid*, *Grave Digger*, **The Abyss Golem** (Act II Boss).
   - **Act III (*The Tower of Entropy — Advanced Sorting & Complexity*):** *Hydra of Division*, *Pivot Wraith*, *The Sphinx of Entropy* (Mid-Boss), **Entropy, The Chaos King** (Final Boss).
5. **Dynamic Difficulty Adjustment (DDA) & Mercy System (*Grimoire Guidance*):**
   - Penyesuaian tingkat kesulitan otomatis (*Assisted*, *Standard*, *Mastery*) berdasarkan 5 soal terakhir, serta panduan langkah demi langkah bebas penalti jika salah $\ge 3\times$ berturut-turut pada TP yang sama.
6. **Mode Guru (*Teacher Analytics & CSV Telemetry Export*):**
   - Pelacakan akurasi per-TP, waktu jawab rata-rata, skor *Pre-Test* / *Post-Test*, kalkulasi **N-Gain**, dan ekspor laporan `.csv` (`user://teacher_report_grimoires_aegis.csv`).

---

## Cara Menjalankan Proyek (Unity 6 — `Grimoire's Aegis/` & Godot 4.7+)

### A. Menjalankan di Unity 6 (`6000.3.7f1` — Direkomendasikan)
1. Buka **Unity Hub**, tambahkan proyek dari folder **`Grimoire's Aegis/`**, lalu buka dengan **Unity 6 (`6000.3.7f1`)**.
2. Buka scene **`Assets/Scenes/SampleScene.unity`** (telah dilengkapi `GrimoireGameManager` & `GrimoirePixelStagePreview`), lalu tekan tombol **Play (▶)**.
3. Untuk meregenerasi seluruh **106 aset Pixel Art & Audio Chiptune WAV** (`Assets/Resources/Gen/` & `assets/gen/`) dengan pipeline bergaya *Lost in Multiverse RPG*:
   ```powershell
   python tools/build_grimoire_assets.py
   ```

### B. Menjalankan di Godot 4.7+
1. Buka **Godot Engine 4.7+** dan pilih **Import** pada file `project.godot` di direktori root ini, lalu tekan **F5 (Run Project)**.
2. Untuk memverifikasi melalui terminal (*headless*):
   ```powershell
   Godot_v4.7.2-stable_win64_console.exe --headless --quit --path .
   ```
3. Untuk meregenerasi seluruh aset sprite piksel 16-bit (`32x32` statis + `128x32` 4-frame idle sheet):
   ```powershell
   python generate_pixel_assets.py
   ```

---

## Pintasan Kontrol (Keyboard & Mouse)

| Tombol | Fungsi |
| :---: | :--- |
| **`1` – `5`** | Memilih aksi komando giliran atau memilih opsi jawaban **A / B / C / D** |
| **`Tab`** | Membuka / menutup **Codex Grimoire** (*In-Game Algorithm Encyclopedia*) |
| **`N`** | Lompat ke pertarungan monster berikutnya (*Gauntlet Testing*) |
| **`T`** | Membuka **Teacher Analytics Report** & ekspor **CSV Telemetry** |
| **`R`** | Mengatur ulang pertarungan / membangkitkan karakter |
