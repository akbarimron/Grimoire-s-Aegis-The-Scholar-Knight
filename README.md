# Grimoire's Aegis: The Scholar Knight

> *"Di dunia yang diacak kekacauan, hanya logika yang bisa menemukan jalan pulang."*

**Grimoire’s Aegis: The Scholar Knight** (*The Order of Lost Indexes*) adalah RPG turn-based 2D *dark fantasy* edutainment berstandar kurikulum SMK RPL/PPLG & Mahasiswa IT pemula. Di game ini, setiap serangan musuh hanya bisa ditangkis dengan menelusuri (*tracing*), memilih, dan menyusun algoritma **Searching & Sorting** di bawah tekanan waktu (*Active Defend System*).

---

## 🎮 Fitur Utama & Core Gameplay

1. **Active Defend System**:
   - **Bullet-time & Pop-up Perkamen**: Setiap musuh menyerang, perkamen kuno muncul menampilkan array dan soal algoritma.
   - **Tiga Tier Pertahanan**:
     - **Tier 1: Perfect Guard** (Jawaban benar & sisa waktu $\ge 60\%$): $0$ Damage + *Counter-Attack* sebesar $50\%$ ATK + $1$ *Logic Fragment*.
     - **Tier 2: Guard** (Jawaban benar, sisa waktu $< 60\%$): $0$ Damage (*Miss*).
     - **Tier 3: Break / Lethal Strike** (Salah / Waktu habis): Terkena damage formula $\min(3 \times \text{ATK}_{\text{musuh}} - \text{DEF}_{\text{pemain}}, 0.5 \times \text{HP}_{\text{maks}})$ + $1$ *Panic Stack*.
2. **Procedural Question Generator**:
   - Soal di-*generate* prosedural secara matematis sehingga pemain tidak bisa sekadar menghafal pola angka.
   - Pilihan distraktor dirancang berdasarkan **miskonsepsi umum siswa** (seperti indeks berbasis 1, pencarian duplikat terakhir, kelalaian penanganan nilai tidak ditemukan `-1`, salah pembulatan *mid* Binary Search, dsb.).
3. **Materi Pembelajaran (Taksonomi Bloom C2–C5)**:
   - **Prolog**: Fondasi Array & Indeks 0 (`Corrupted Slime` - TP-01).
   - **Act I**: Linear Search & Binary Search (`Lost Wisp`, `Bookworm Horror`, `Twin-Faced Librarian`, `The Blind Archivist` - TP-02 s/d TP-05).
   - **Act II**: Sorting Dasar Bubble, Selection, & Insertion Sort (`Skeleton Archer`, `Venomous Arachnid`, `Grave Digger`, `The Abyss Golem` - TP-06 s/d TP-09).
   - **Act III & Final**: Merge Sort, Quick Sort, Big-O Notation, & Katalog Agung (`Hydra of Division`, `Pivot Wraith`, `The Sphinx of Entropy`, `Raja Kekacauan Entropy` - TP-10 s/d TP-12).

---

## 🎨 Seni Piksel 16-Bit (Pixel Art Guidelines)

- **Resolusi Dasar**: $480 \times 270$ (16:9), di-*scale* integer / viewport menuju 1080p.
- **Filter Tekstur**: Nearest (`textures/canvas_textures/default_texture_filter=0`).
- **Palet Warna**: *Dark fantasy* dingin & *gritty* dengan *hue-shifting* (bayangan bergeser dingin ke biru/slate, *highlight* hangat ke kuning/emas).
- **Hard Pixel Edges**: 100% *cluster control* tanpa *anti-aliasing* kabur ke latar belakang.

---

## 🛠️ Persiapan & Menjalankan Game (Godot 4)

1. **Engine**: Godot Engine v4.x (Direkomendasikan Godot 4.3+ / 4.7+).
2. **Buka Proyek**:
   ```bash
   godot -e --path "D:/Project/The-Scholar-Knight_EDU-GAME"
   ```
3. **Godot AI MCP Server**:
   Proyek ini dilengkapi dengan addon `addons/godot_ai` yang terintegrasi langsung dengan Godot AI MCP server untuk otomasi, *scene introspection*, eksekusi kode *in-game*, dan inspeksi visual secara langsung melalui agent AI.
