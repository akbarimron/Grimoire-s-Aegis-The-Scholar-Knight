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

### Art & Animasi v2 (`tools/grimoire_art_v2.py`, `grimoire_monsters_v2.py`, `grimoire_extras_v2.py`)
Semua karakter digambar ulang per frame (bukan hanya digeser) dengan ramp warna hue-shift, satu sumber cahaya kiri-atas, dan selective outline.

| Aktor | Ukuran frame | Animasi |
| :--- | :---: | :--- |
| **Aegis, The Scholar Knight** | 48×48 | `idle` 4f, `walk` 6f, `attack` 6f (antisipasi → smear → follow-through), `defend` 3f, `parry` 4f (perisai heksagonal + counter), `cast` 6f (Grimoire terbuka), `hurt` 3f, `victory` 4f, `death` 6f |
| 9 monster biasa + Twin-Faced Librarian | 48×48 | `idle` 4f, `attack` 5f (pola serangan meniru algoritmanya, GDD §9), `hurt` 3f, `death` 6f (*Glitch dissolve*) |
| Blind Archivist, Abyss Golem, Sphinx, Entropy | 64×64 | sama seperti di atas |
| NPC: Archivist Lyra, Mordecai, Bayang-bayang Ordo | 64×64 | potret `idle` dengan kedip |
| VFX | 64×64 | `slash`, `hex_shield`, `shatter`, `arcane`, `spark`, `heal`, `poison` |
| Objek array bertema (GDD §17) | 16 px lebar | buku (Act I), lilin (Act II), kristal (Act III) — tinggi mengikuti nilai |

Sheet disimpan sebagai strip horizontal di `Resources/Gen/anim/`, `vfx/`, `npc/`, `array/`; durasi tiap frame ada di `anim/anim_index.json` dan dibaca otomatis oleh Unity (`GrimoireBattleFx.cs`).

### Active Defend sesuai GDD §7
- **Perfect Guard**: benar & sisa waktu ≥ 60% → 0 damage, counter-attack, +1 *Logic Fragment*, Panic −1 (“AEGIS ACTIVE!”).
- **Guard**: benar & sisa waktu < 60% → 0 damage (MISS).
- **Break**: salah/waktu habis → damage = min(3×ATK musuh − DEF, 50% HP maks), +1 Panic (maks 3: teks bergetar → tepi layar gelap → urutan tombol diacak).
- Monster membeku di tengah ayunan (*bullet-time*) saat soal muncul; timer baru berjalan setelah **masa baca 1,5 detik**; tepi layar berdenyut merah saat sisa waktu ≤ 30%; timer +1 detik saat HP < 20%.

### Tingkat Kesulitan
| Mode | Timer | Damage Break maks | Lainnya |
| :--- | :--- | :---: | :--- |
| **Mudah** | **Tanpa batas waktu** — jawab selama yang kamu mau | 25% HP | Jawaban benar selalu Perfect Guard, Hint 5 MP, monster −10% HP |
| **Sedang** | Normal (mode *Scholar* GDD) | 50% HP | Masa baca 1,5 detik |
| **Sulit** | −20%, masa baca 1 detik | 50% HP | Monster +15% HP & ATK, Analisis tidak menampilkan topik |

Mode dipilih saat *Petualangan Baru* dan bisa diganti kapan saja di **Pengaturan [O]** (juga: pengali timer 1.0–2.0×, DDA on/off, getar layar, volume).

### Alur permainan (revisi)
1. **Judul** → pilih kesulitan → Lyra membuka cerita dalam beberapa kalimat singkat → kartu Act.
2. **Peta petualangan** (`Core/AdventureMap.cs`): satu peta besar yang dirancang manual per Act, bergulir mengikuti Aegis. Lempar dadu, Aegis berjalan sendiri, dan di persimpangan pemain memilih arah. Monster berdiri di jalan dan menghentikan langkah.
3. **Gerbang boss butuh 3 Kunci Indeks**: dari *Penjaga Kunci* (elite), *Rak Terkunci* (teka-teki tanpa batas waktu, GDD §4), dan *Peti Tersembunyi*. Tidak ada lagi jalan pintas ke boss.
4. **Pertarungan**: 4 aksi besar (Serang, Mantra, Ramuan, Kabur); mantra terbuka bertahap per Act; klik nama monster untuk info. Tidak ada catatan pertarungan panjang, hanya satu baris pesan.
5. **Menjawab soal**: pilih jawaban → perkamen menutup → **monster menyerang dulu** (ditangkis atau kena) → baru muncul kartu singkat: BENAR / KURANG TEPAT, jawaban yang benar, dan satu kalimat pembahasan.
6. Lyra muncul hanya pada momen penting (langkah pertama, pertarungan pertama, soal pertama, kunci, gerbang, boss, Act selesai).

### Monster biasa (kroco) baru
Zombi Tinta & Kelelawar Kertas (Act I), Prajurit Tulang & Ghoul Kripta (Act II), Imp Glitch & Kumbang Kristal (Act III), masing-masing dengan animasi idle, serang, skill, terluka, mati, dan muncul (`tools/grimoire_minions_v2.py`). Total kini 19 monster.

### GUI bergaya *Lost in Multiverse RPG*
Semua layar digambar di kanvas piksel **640×360** yang diperbesar dengan skala bulat (tajam di layar apa pun), memakai font piksel 5×7 dan panel 9-slice (`tools/grimoire_ui_v2.py`, `UI/PixelUI.cs`):
- **Judul** → pilih kesulitan / Act → **kartu Act** → **papan labirin** (dadu beranimasi, token berjalan, tooltip petak) → **pertarungan** (panel Kondisi kiri-atas, papan nama + HP monster, HUD 3 kolom: potret & bar, daftar aksi berikon, catatan pertarungan).
- Soal tampil di **perkamen** dengan timer, objek array bertema, dan 4 pilihan; hasil, Kilas Codex (Bayang-bayang Ordo), Analisis musuh, dan layar hadiah memakai pop-up yang sama.
- **Buku Panduan [G]** 13 bab (kontrol, papan, pertarungan, Active Defend, mode, semua status effect, 13 monster beranimasi, mantra, item, format soal, panduan guru), **Codex & Bestiary [C]** dengan pemutar animasi tiap monster, **Toko Mordecai / Tas [I]**, **Laporan Guru [T]**, **Pengaturan [O]**, dan menu **Jeda [Esc]**.

### Status Effect
Racun, Bisu, Halaman Robek, Buta, Terkubur, Remuk, Terbelah, Segel Kematian, Panik (debuff); Insertion Ward, Adrenalin, Limit Siap (buff); Terurut, Ditandai, Zirah Batu, Regenerasi, Murka, Kabut, Pivot Palsu (pada monster). Tiap efek punya ikon, sisa giliran, tooltip, dan entri di Buku Panduan. Skeleton Archer menembak 3 panah (3 soal) dan Bookworm menggigit 2× sesuai GDD §9.

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
| **Panah / WASD** | Pilih menu; di papan: melangkah ke petak tetangga |
| **Enter / Spasi** | Konfirmasi; di papan: Spasi lempar dadu, Enter berhenti di petak |
| **`1` – `5`** | Aksi cepat pertarungan (Serang, Grimoire, Item, Analisis, Kabur) |
| **`1` – `4`** | Memilih jawaban soal · **`H`** Scholar's Hint |
| **`G`** | Buku Panduan |
| **`C`** | Codex & Bestiary |
| **`I`** | Tas & Toko (di luar pertarungan) |
| **`T`** | Laporan Guru & ekspor CSV |
| **`O`** | Pengaturan (kesulitan, timer, DDA, efek layar, audio) |
| **`Esc`** | Tutup jendela / Jeda |
