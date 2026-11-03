Game Design Document
Kelompok 3
Ariel Hidayatul Faqih 2401265
Muhammad Akbar Imron 2400000
Muhammad Raditya Santosa 2409461
Khairatul Husna Tartila 2400494
Septin Sehawati 2400923
1. Identitas Inti Game
Grimoire’s Aegis adalah RPG turn-based dark fantasy di mana setiap serangan musuh hanya bisa
ditangkis dengan menjawab soal searching atau sorting di bawah tekanan waktu.
Atribut Detail
Judul Grimoire’s Aegis: The Scholar Knight
Subjudul (opsional) The Order of Lost Indexes
Tagline “Di dunia yang diacak kekacauan, hanya logika yang bisa menemukan
jalan pulang.”
Genre 2D Dark Fantasy RPG, Turn-Based Combat, Edutainment
Platform PC (Windows) sebagai target utama; Android sebagai target kedua (lab
sekolah & HP siswa)
Engine (rekomendasi) Godot 4 (ringan, gratis, ekspor PC + Android) atau Unity 2D
Kontrol Keyboard, gamepad (D-Pad), dan layar sentuh
Target pemain Siswa SMK RPL / TKJ / PPLG (15–18 tahun) dan mahasiswa IT
pemula
Prasyarat pemain Paham variabel, array/list, perulangan, dan percabangan dasar
Durasi sesi 15–25 menit per lantai dungeon, agar muat dalam satu jam pelajaran
bersama pembukaan dan refleksi
Total konten ± 6–8 jam (Prolog + 3 Act + Final)
Rating konten Kekerasan fantasi ringan tanpa darah berlebihan; aman untuk usia 13+
Tujuan edukasi utama: siswa mampu menelusuri (trace), memilih, dan menyusun langkah
algoritma searching dan sorting, serta membandingkan efisiensinya. Tekanan waktu melatih refleks
computational thinking, sedangkan Codex dan pembahasan menjamin pemahaman, bukan sekadar
tebakan.

2. Tujuan Pembelajaran & Peta Kompetensi
Ada 12 tujuan pembelajaran (TP) yang tersebar dari Prolog sampai Final, disusun bertahap dari
mengingat hingga mengevaluasi (Taksonomi Bloom). Setiap soal di game diberi tag TP agar
laporan guru bisa menunjukkan topik mana yang lemah.
Kode Tujuan pembelajaran Level Bloom Muncul di
TP-01 Menjelaskan array, indeks (mulai dari 0), dan C2 Memahami Prolog
panjang array
TP-02 Menelusuri Linear Search dan menghitung C3 Menerapkan Act I
jumlah perbandingannya
TP-03 Menentukan hasil pencarian untuk kasus C3 Menerapkan Act I
khusus: data ganda, data tidak ada (return -1)
TP-04 Menjelaskan syarat Binary Search (data harus C2 Memahami Act I
terurut)
TP-05 Menelusuri Binary Search: menghitung low, C3 Menerapkan Act I
high, dan mid di setiap langkah
TP-06 Menelusuri Bubble Sort per pass, termasuk C3 Menerapkan Act II
versi optimasi (berhenti bila tidak ada
pertukaran)
TP-07 Menelusuri Selection Sort per pass C3 Menerapkan Act II
TP-08 Menelusuri Insertion Sort per langkah C3 Menerapkan Act II
penyisipan
TP-09 Membedakan ciri ketiga sorting dasar C4 Act II
(jumlah swap, sifat stabil, kasus terbaik) Menganalisis
TP-10 Menelusuri proses divide dan merge pada C3 Menerapkan Act III
Merge Sort
TP-11 Menelusuri partisi Quick Sort dan posisi C3 Menerapkan Act III
akhir pivot
TP-12 Memilih algoritma yang paling tepat untuk C5 Act III & Final
suatu kasus berdasarkan kompleksitas Mengevaluasi
(Big-O)
Catatan kurikulum: pemetaan TP ini disusun mengikuti materi algoritma searching & sorting yang
umum diajarkan pada mata pelajaran pemrograman dasar di SMK. Guru dapat menyesuaikan kode
TP dengan Capaian Pembelajaran dan modul ajar sekolah masing-masing.
3. Latar Belakang & Lore
Algoria runtuh karena urutan dunianya diacak; Scholar Knight harus mencari pecahan yang hilang
dan mengurutkan kembali hukum alam.
Dunia Algoria. Kerajaan abad pertengahan yang suram. Keajaiban di sini tidak berasal dari sihir
mistis, melainkan dari “Logika Absolut”. Segala hal di Algoria (bintang, musim, bahkan ingatan

manusia) tersimpan dalam Katalog Agung, sebuah perpustakaan raksasa yang menyimpan semua
hal secara berurutan.
Konflik utama. Raja Iblis Entropy menghancurkan Katalog Agung dan menyebarkan kutukan The
Glitch. Kutukan ini tidak hanya mengubah makhluk menjadi monster, tetapi juga mengacak
urutan dunia: matahari terbit tak beraturan, nama-nama penduduk hilang, dan lorong kastil
tersusun kacau. Monster yang terkena Glitch “menyerang dengan kekacauan”, yang hanya bisa
ditangkis dengan keteraturan logika.
Karakter utama. Seorang Scholar Knight pemula dari Ordo Sintaksis. Ia membawa pedang baja
tumpul dan Grimoire, buku kuno peninggalan penyihir agung yang mencatat semua algoritma.
Halaman-halaman Grimoire tercerai-berai; setiap halaman yang ditemukan membuka satu algoritma
baru (lihat Codex di bagian 16).
Tokoh pendukung.
• Archivist Lyra: penjaga terakhir Katalog Agung di Desa Perlindungan. Berperan sebagai
mentor dan pemberi tutorial.
• Mordecai si Pandai Besi: menjual dan meningkatkan senjata serta armor.
• Bayang-bayang Ordo: arwah ksatria yang gagal. Muncul di save point dan memberi
petunjuk materi.
Benang merah cerita. Act I memulihkan kemampuan menemukan (searching). Act II dan III
memulihkan kemampuan menata (sorting). Di Final, pemain menyadari Entropy tidak bisa
dihancurkan, hanya bisa diurutkan; kemenangan dicapai dengan menyusun kembali Katalog Agung.
4. Struktur Dunia & Progresi Area
Dunia dibagi menjadi 5 bagian yang mengikuti urutan belajar: array → searching → sorting dasar
→ sorting divide & conquer → evaluasi gabungan. Setiap Act dibuka setelah boss Act sebelumnya
dikalahkan.
Bagian Area Materi Lantai Boss / Mini-boss Hadiah utama
Prolog Gerbang Ordo Array & indeks 1 Corrupted Slime Grimoire, halaman
yang Runtuh (tutorial) “Array”
Act I Perpustakaan Linear Search, 3 The Blind Halaman
Terkubur (The Binary Search Archivist “Searching”,
Sunken Lentera Pointer
Archive)
Act II Benteng Tulang Bubble, 3 The Abyss Halaman “Sorting
Berserakan Selection, Golem Dasar”
(Ossuary Keep) Insertion Sort
Act III Menara Merge Sort, 3 The Sphinx of Halaman “Divide &
Pecah-Belah Quick Sort, Entropy Conquer”
(Tower of Big-O
Division)

Bagian Area Materi Lantai Boss / Mini-boss Hadiah utama
Final Singgasana Semua materi + 1 Entropy, Raja Ending + mode New
Entropy pemilihan Kekacauan (3 Game+
algoritma fase)
Desain lingkungan yang ikut mengajar:
• Perpustakaan Terkubur: rak buku bernomor indeks. Puzzle eksplorasi meminta pemain
membuka pintu dengan mencari buku tertentu; rak yang terurut membolehkan “lompat ke
tengah” (binary search), rak yang acak memaksa memeriksa satu per satu.
• Benteng Tulang: tumpukan peti mati dan lilin dengan tinggi berbeda. Puzzle meminta
pemain menyusun lilin dari pendek ke tinggi dengan aturan tukar tertentu (hanya boleh
menukar dua yang bersebelahan = bubble sort).
• Menara Pecah-Belah: jembatan yang terbelah dua terus-menerus (divide) dan harus
disatukan kembali dengan urutan benar (merge).
Puzzle lingkungan tidak dibatasi waktu, sehingga berfungsi sebagai latihan santai sebelum diuji
dalam pertarungan.
5. Core Gameplay Loop
Game berjalan dalam dua loop: loop besar per lantai dungeon (persiapan → eksplorasi →
pertarungan → hadiah) dan loop kecil per giliran di dalam pertarungan, tempat soal
searching/sorting muncul.
Core gameplay loop: loop besar per lantai dan loop kecil per giliran
Perfect Guard memberi Logic Fragments, yang membuka perlengkapan terbaik di Desa; karena itu,
pemain yang menguasai materi akan maju paling cepat.

6. Mekanik Fase Pemain (Player Turn)
Di giliran pemain tidak ada batas waktu; pemain memilih satu dari 5 aksi. Spell Grimoire diberi
nama  algoritma  dan  efeknya  meniru  cara  kerja  algoritma  tersebut,  sehingga  konsep  ikut
tertanam lewat gameplay.
| Aksi  | Biaya  | Efek  |     |
| ----- | ------ | ----- | --- |
Attack  10 Stamina  Serangan pedang fisik, damage = ATK − DEF musuh (minimum 1)
| Grimoire  | Memory  | Sihir yang mengincar kelemahan musuh  |     |
| --------- | ------- | ------------------------------------- | --- |
| (Spell)   | (lihat  | tabel                                 |     |
spell)
Inventory  1 giliran  Pakai Potion (HP), Ether (Memory), atau item khusus
Analyze  1 giliran  Menampilkan topik soal musuh, pola serangan, dan kelemahannya.
Info tersimpan di Bestiary
Flee  1 giliran  Kabur. Peluang dasar 40%, +10% per tingkat Boots of Haste. Tidak
bisa di pertarungan boss
Spell Grimoire (dibuka bertahap):
| Spell  | Dibuka  | Memory  | Efek (meniru algoritma)  |
| ------ | ------- | ------- | ------------------------ |
Linear  Act I  8  Menyerang semua musuh satu per satu dari kiri ke kanan;
Strike  berhenti ketika mengenai target yang sedang ditandai
Binary  Act I  14  Damage  besar  ke  satu  musuh.  Hanya bisa dipakai bila
Lance  barisan  musuh  sudah  “terurut”  (debuff  Ordered  aktif),
mengajarkan syarat binary search
Bubble Shift  Act II  10  Menukar posisi dua musuh bersebelahan; musuh dengan
HP terendah “terdorong” ke depan agar mudah dihabisi
Selection  Act II  12  Menandai  musuh  dengan  HP  terkecil,  lalu  memberi
| Mark  |     |     | damage kritis kepadanya  |
| ----- | --- | --- | ------------------------ |
Insertion  Act II  12  Menyisipkan  perisai  ke  giliran  berikutnya:  mengurangi
| Ward  |     |     | damage serangan berikutnya 30%  |
| ----- | --- | --- | ------------------------------- |
Merge  Act III  20  Memecah serangan menjadi 2, lalu 4 tebasan kecil yang
| Barrage  |     |     | digabung menjadi satu combo  |
| -------- | --- | --- | ---------------------------- |
Quick Pivot  Act III  18  Pilih satu musuh sebagai pivot: musuh lebih lemah dari
pivot terkena damage, yang lebih kuat terkena debuff
Order  Final  30  Memberi debuff Ordered ke semua musuh (DEF turun
| Restored  |     |     | 25%, soal berikutnya mendapat +2 detik)  |
| --------- | --- | --- | ---------------------------------------- |
7. Active Defend System (Fase Musuh)
Setiap serangan musuh memicu satu pop-up soal; hasilnya ditentukan oleh benar/salah dan sisa
waktu, dengan 3 tier yang ambangnya jelas.
Urutan kejadian:

1.  Trigger: animasi serangan dimulai (contoh: Golem mengangkat batu). Waktu melambat
(bullet-time) selama 0,6 detik.
2.  Pop-up: perkamen besar muncul di tengah layar berisi soal. Array ditampilkan sebagai
deretan objek bertema (lilin, buku, tulang) lengkap dengan nomor indeks.
3.  Masa baca: timer baru berjalan setelah 1,5 detik masa baca, agar soal tidak terasa curang.
4.  Timer: bar waktu berjalan mundur. Saat sisa 30%, detak jantung dan detak jarum jam makin
cepat, tepi layar memerah.
5.  Jawaban:  pemain  memilih  atau  memanipulasi  jawaban  (format  di  bagian  8),  lalu
konfirmasi.
6.  Resolusi: animasi hasil diputar, lalu pembahasan singkat 1 baris muncul selama 2 detik (bisa
dilewati).
Tier hasil:
|          |         |         |         | Umpan         | balik   |
| -------- | ------- | ------- | ------- | ------------- | ------- |
| Tier     | Syarat  | Efek    |         | audio-visual  |         |
| Perfect  | Benar   | dan  0  | damage  | +  Teks       | “AEGIS  |
Guard  dijawab  saat  Counter-Attack sebesar  ACTIVE!”,  perisai
|     | sisa  waktu  | ≥  50%          | ATK  pemain + 1  | heksagonal   | biru       |
| --- | ------------ | --------------- | ---------------- | ------------ | ---------- |
|     | 60%          | Logic Fragment  |                  | memantulkan  | serangan,  |
denting logam nyaring
| Guard  | Benar,       | sisa  0 damage (MISS)  |     | Perisai                  | menahan  |
| ------ | ------------ | ---------------------- | --- | ------------------------ | -------- |
|        | waktu < 60%  |                        |     | serangan, denting logam  |          |
Break  Salah  atau  Damage  penuh  (rumus  Kaca  pecah,  screen
(Lethal  waktu habis  di bawah) + 1 tumpukan  shake, layar memerah
| Strike)  |     | Panic  |     |     |     |
| -------- | --- | ------ | --- | --- | --- |
Rumus damage Break:
Damage = min( 3 × ATK musuh − DEF pemain , 0,5 × HP maks )
Batas  50%  HP  maksimal  berlaku  untuk  semua serangan, termasuk boss. Dengan begitu satu
kesalahan selalu terasa berat, tetapi tidak pernah langsung membunuh dari HP penuh.
Rumus timer:
T = T dasar + 0,5 × n elemen + T item + T mode
•  T dasar: dari tabel kesulitan (bagian 12).
•  n elemen: jumlah elemen array atau baris kode yang tampil, agar soal yang lebih panjang
mendapat waktu baca lebih.
•  T item: bonus dari Hourglass Ring (+3 detik) dan efek lain.
•  T mode: penyesuaian mode kesulitan (Story +50%, Scholar 0, Nightmare −20%).
8. Format Soal & Mekanik Interaktif
Game  memakai  7  format  soal;  4 di antaranya meminta pemain memanipulasi array secara
langsung, sehingga yang diuji adalah kemampuan menjalankan algoritma, bukan menebak pilihan.

Kode Format Cara main Materi paling cocok
F1 Pilihan Pilih satu jawaban dengan Konsep, Big-O, syarat algoritma
Ganda D-Pad/keyboard (tombol 1–4) atau sentuh
(A–D)
F2 Benar / Dua tombol besar; dipakai untuk Pernyataan singkat tentang
Salah serangan beruntun yang cepat algoritma
F3 Trace Pilih kondisi array yang benar setelah Bubble, Selection, Insertion Sort
Array pass ke-k dari 3–4 pilihan
F4 Swap Klik dua elemen untuk menukarnya, Bubble Sort, Selection Sort,
Puzzle mengikuti aturan algoritma yang diminta. partisi Quick Sort
Tukar yang tidak sesuai aturan langsung
dihitung salah
F5 Pointer Array terurut ditampilkan; pemain Binary Search
Hunt memindahkan penanda ke indeks mid
yang benar di setiap langkah sampai
target ditemukan
F6 Hitung Masukkan angka (berapa perbandingan / Linear Search, Binary Search,
Perbandin berapa swap / indeks hasil) lewat tombol Bubble Sort
gan angka
F7 Susun Menyusun potongan pseudocode atau Semua algoritma, khususnya
Langkah langkah algoritma yang diacak ke urutan boss
(Parsons yang benar
Problem)
Aturan desain soal:
• Array berisi 4–8 elemen, nilai 1–99, agar bisa ditelusuri di kepala dalam hitungan detik.
• Indeks selalu dimulai dari 0 dan selalu ditampilkan di bawah elemen.
• Pseudocode memakai satu gaya yang konsisten (mirip Python), dijelaskan di Codex.
• Untuk Binary Search, rumus mid ditetapkan mid = (low + high) // 2 (pembulatan ke bawah)
dan ditulis di layar.
• Untuk sorting, arah urutan (naik/turun) selalu disebutkan di soal.
• Format interaktif (F4, F5) memakai mode “langkah demi langkah”: setiap langkah benar
menambah bar Aegis; satu langkah salah memecahkan perisai.
9. Bestiary & Pemetaan Kurikulum
Ada 13 musuh: 9 monster biasa, 3 boss Act, dan 1 boss final. Setiap monster mewakili satu
algoritma, dan pola serangannya meniru cara kerja algoritma tersebut. Tier timer merujuk ke
tabel di bagian 12.

Prolog & Act I: Searching
| Monste | Materi    | Form      | Pola serangan & mekanik  |     |     |                  |     |
| ------ | --------- | --------- | ------------------------ | --- | --- | ---------------- | --- |
| r      | Lv  (TP)  | at  Tier  | khusus                   |     |     | Efek bila gagal  |     |
Corrupt 1  Array  &  F1, F2  T1  Melompat  untuk  menelan.  Damage normal
| ed     | indeks   |     | Soal sangat singkat, contoh:  |     |     |     |     |
| ------ | -------- | --- | ----------------------------- | --- | --- | --- | --- |
| Slime  | (TP-01)  |     | “Nilai pada indeks ke-2?”     |     |     |     |     |
Lost  2  Linear  F1, F6  T1  Lentera menyinari rak satu  Blind:  pilihan
| Wisp      | Search   |     | per  satu              | dari  kiri,  | persis  | jawaban            | baru  |
| --------- | -------- | --- | ---------------------- | ------------ | ------- | ------------------ | ----- |
| (Hantu    | dasar    |     | seperti linear search  |              |         | terlihat  setelah  | 1     |
| Lentera)  | (TP-02)  |     |                        |              |         | detik  pada        | soal  |
berikutnya
Bookwo 3  Linear  F1, F6  T2  Menggigit 2 kali beruntun =  Damage normal per
| rm        | Search        |     | 2 soal berturut-turut  |     |     | gigitan  |     |
| --------- | ------------- | --- | ---------------------- | --- | --- | -------- | --- |
| Horror    | kasus         |     |                        |     |     |          |     |
| (Cacing   | khusus:       |     |                        |     |     |          |     |
| Pustaka)  | data  ganda,  |     |                        |     |     |          |     |
|           | data  tidak   |     |                        |     |     |          |     |
|           | ada,  kasus   |     |                        |     |     |          |     |
terburuk
(TP-03)
Twin-Fa 4  Binary  F5, F6  T3  Merobek buku menjadi dua  Torn  Page:  1
| ced  | Search  |     | bagian  | di  setiap  | langkah,  | halaman  | spell  |
| ---- | ------- | --- | ------- | ----------- | --------- | -------- | ------ |
Libraria (TP-04,  persis  seperti  pembagian  terkunci  selama  2
| n   | TP-05)  |     | ruang pencarian  |     |     | giliran  |     |
| --- | ------- | --- | ---------------- | --- | --- | -------- | --- |
(Pustaka
wan
Bermuk
a Dua)
The  5  Linear  vs  F1,  T4  Fase  1:  menebak  lokasi  Silence:  tidak  bisa
Blind  Binary  F5, F6  pemain  di  rak  terurut  memakai  spell
Archivi Search  (Pointer Hunt). Fase 2: soal  selama 1 giliran
| st (Boss  | (TP-02  s/d  |     | jebakan; kadang array tidak  |          |          |     |     |
| --------- | ------------ | --- | ---------------------------- | -------- | -------- | --- | --- |
| Act I)    | TP-05)       |     | terurut                      | dan      | jawaban  |     |     |
|           |              |     | benarnya                     | “Binary  | Search   |     |     |
tidak dapat digunakan”
Act II: Sorting Dasar
|     | L   |     | Pola  | serangan  &  | mekanik  |     |     |
| --- | --- | --- | ----- | ------------ | -------- | --- | --- |
Monster  v  Materi (TP)  Format  Tier  khusus  Efek bila gagal
Skeleton  6  Bubble  Sort  F3, F2  T2  Menembakkan  3  panah  = 3  Setiap panah yang
| Archer  | (TP-06)  |     | soal beruntun dengan jeda 2  |        |       | mengenai  |     |
| ------- | -------- | --- | ---------------------------- | ------ | ----- | --------- | --- |
|         |          |     | detik.                       | Panah  | ke-k  |           |     |

|     | L   |     | Pola  serangan  | &   | mekanik  |     |     |
| --- | --- | --- | --------------- | --- | -------- | --- | --- |
Monster  v  Materi (TP)  Format  Tier  khusus  Efek bila gagal
|     |     |     | menanyakan  | kondisi      | array     | memberi       | 1/3  |
| --- | --- | --- | ----------- | ------------ | --------- | ------------- | ---- |
|     |     |     | setelah     | pass  ke-k.  | Gagal di  | damage Break  |      |
panah ke-2 berarti panah ke-2
dan ke-3 mengenai pemain
Venomou 7  Selection  F3, F4  T3  Laba-laba  selalu  memangsa  Poison:  −5% HP
s  Sort (TP-07)  korban  terkecil di jaringnya  maks  tiap  giliran
| Arachnid  |     |     | lalu  memindahkannya             |     | ke  | selama 3 giliran  |     |
| --------- | --- | --- | -------------------------------- | --- | --- | ----------------- | --- |
| (Laba-lab |     |     | depan, persis seperti selection  |     |     |                   |     |
| a         |     |     | sort                             |     |     |                   |     |
Raksasa)
Grave  8  Insertion  F3, F4  T3  Menyisipkan  peti  mati  ke  Buried:  menu
Digger  Sort (TP-08)  liang yang tepat di antara peti  Inventory
| (Penggali  |     |     | yang sudah tersusun  |     |     | terkunci 1 giliran  |     |
| ---------- | --- | --- | -------------------- | --- | --- | ------------------- | --- |
Kubur)
The  9  Perbandinga F1,  F3,  T4  Fase 1: menumpuk batu yang  Crushed:  DEF
Abyss  n  3  sorting  F4  harus disusun (Swap Puzzle 6  turun 20% selama
| Golem      | dasar        |     | elemen,                      | algoritma               | diacak).  | 2 giliran  |     |
| ---------- | ------------ | --- | ---------------------------- | ----------------------- | --------- | ---------- | --- |
| (Boss Act  | (TP-06  s/d  |     | Fase  2:                     | soal  analisis seperti  |           |            |     |
| II)        | TP-09)       |     | “Algoritma mana yang paling  |                         |           |            |     |
sedikit melakukan swap?”
Act III & Final: Divide & Conquer dan Evaluasi
|          | L Materi  | Forma    | Pola            | serangan  | &   |                  |     |
| -------- | --------- | -------- | --------------- | --------- | --- | ---------------- | --- |
| Monster  | v  (TP)   | t  Tier  | mekanik khusus  |           |     | Efek bila gagal  |     |
Hydra  of  10  Merge  Sort  F3, F7  T3  Setiap  kepala  yang  Damage  normal  +
Division  (TP-10)  dipotong terbelah menjadi  Hydra  memulihkan
|     |     |     | 2  kepala      | kecil  | (divide),  | 10% HP  |     |
| --- | --- | --- | -------------- | ------ | ---------- | ------- | --- |
|     |     |     | lalu  menyatu  |        | kembali    |         |     |
sambil menyerang (merge)
Pivot  11  Quick  Sort  F3, F4  T3  Memilih  satu  korban  Split:  serangan
| Wraith  | (TP-11)  |     | sebagai    | pivot,     | lalu   | pemain  berikutnya  |     |
| ------- | -------- | --- | ---------- | ---------- | ------ | ------------------- | --- |
| (Arwah  |          |     | mendorong  | yang       | lebih  | hanya memberi 50%   |     |
| Poros)  |          |     | kecil  ke  | kiri  dan  | yang   | damage              |     |
lebih besar ke kanan
The  12  Big-O  &  F1, F7  T5  Teka-teki:  memilih  Segel bertambah 1
| Sphinx of  | pemilihan  |     | algoritma                 | terbaik     | untuk  |     |     |
| ---------- | ---------- | --- | ------------------------- | ----------- | ------ | --- | --- |
| Entropy    | algoritma  |     | suatu kasus dan menyusun  |             |        |     |     |
| (Boss Act  | (TP-12)    |     | langkah                   | algoritma.  |        |     |     |
| III)       |            |     | Mekanik Segel Kematian:   |             |        |     |     |
|            |            |     | setiap                    | jawaban     | salah  |     |     |

|          | L Materi  | Forma    | Pola  serangan  |     | &                |     |
| -------- | --------- | -------- | --------------- | --- | ---------------- | --- |
| Monster  | v  (TP)   | t  Tier  | mekanik khusus  |     | Efek bila gagal  |     |
menyalakan 1 dari 3 segel;
|     |     |     | saat  3  segel  | menyala,    |       |     |
| --- | --- | --- | --------------- | ----------- | ----- | --- |
|     |     |     | “Death          | Sentence”   |       |     |
|     |     |     | membuat         | HP  pemain  |       |     |
|     |     |     | tersisa  1      | (bukan      | mati  |     |
instan), lalu segel direset
Entropy,  15  Semua TP  Semua  T4–T Fase  1  searching,  fase  2  Fase  3:  kegagalan
| Raja       |     | format  5  | sorting,                 | fase      | 3  mengacak  | ulang  1  |
| ---------- | --- | ---------- | ------------------------ | --------- | ------------ | --------- |
| Kekacau    |     |            | Memulihkan               | Katalog:  | buku  yang   | sudah     |
| an  (Boss  |     |            | setiap  Perfect          | Guard     | tersusun     |           |
| Final)     |     |            | mengembalikan 1 buku ke  |           |              |           |
Katalog Agung; 10 buku =
Entropy tersegel
Varian Glitched (New Game+): monster yang sama dengan aturan diperumit, misalnya urutan
menurun, data berupa string (diurutkan secara alfabetis), atau data ganda untuk menguji sifat stabil.
10. Contoh Bank Soal
Berikut 16 soal contoh (satu atau lebih per monster) yang sudah ditelusuri dan diperiksa kuncinya. Soal-soal ini
menjadi template bagi generator prosedural di bagian 11: struktur tetap, angka diacak.
| No  Monster  | Format  | Soal  | Kunci  | Pembahasan singkat  |     |     |
| ------------ | ------- | ----- | ------ | ------------------- | --- | --- |
1  Corrupted  F1  A = [4, 9, 7, 1]. Berapa nilai  7  Indeks  dimulai dari 0: A[0]=4, A[1]=9,
| Slime  |     | A[2]?  |     | A[2]=7  |     |     |
| ------ | --- | ------ | --- | ------- | --- | --- |
2  Lost Wisp  F1  Linear search mengembalikan  1  Pencarian berhenti di kecocokan pertama
|     |     | indeks kemunculan pertama.  |     | (indeks 1), tidak lanjut ke indeks 3  |     |     |
| --- | --- | --------------------------- | --- | ------------------------------------- | --- | --- |
Cari 3 pada [7, 3, 9, 3, 5].
Indeks hasilnya?
3  Bookwor F6  Cari 8 pada [7, 3, 9, 3, 5].  5  perbandingan,  Data  tidak  ada,  jadi  semua  5  elemen
m Horror  Berapa  perbandingan  hasil -1  diperiksa (kasus terburuk = n)
dilakukan, dan apa hasilnya?
| 4   | F5  |     | 4 → 7 → 5  |     |     |     |
| --- | --- | --- | ---------- | --- | --- | --- |
Twin-Face A = [2, 5, 8, 12, 16, 23, 38, 56,  mid=4  (16<23,  low=5);  mid=7  (56>23,
d  72,  91],  cari  23.  Tentukan  high=6);  mid=5  (23,  ketemu). Hanya 3
| Librarian  |     | urutan indeks mid  |     | perbandingan  |     |     |
| ---------- | --- | ------------------ | --- | ------------- | --- | --- |
5  The Blind  F1  Array  terurut  berisi  1.000  10  Setiap  langkah  membagi  dua  ruang
Archivist  elemen. Paling banyak berapa  pencarian; 2^10 = 1.024 ≥ 1.000. Linear
|     |     | perbandingan Binary Search?  |     | search bisa butuh 1.000  |     |     |
| --- | --- | ---------------------------- | --- | ------------------------ | --- | --- |
6  The Blind  F1  Algoritma  mana  yang  tepat  Linear  Search  Binary search mensyaratkan data terurut;
Archivist  untuk mencari 9 pada [3, 8, 1,  (Binary  Search  array ini acak
9, 4]?
|     |     |     | tidak  | dapat  |     |     |
| --- | --- | --- | ------ | ------ | --- | --- |
digunakan)
| 7   | F3  |     | [1, 4, 2, 5, 8]  |     |     |     |
| --- | --- | --- | ---------------- | --- | --- | --- |
Skeleton  Bubble sort naik pada [5, 1, 4,  3 pertukaran (5↔1, 5↔4, 5↔2); elemen
Archer  2, 8]. Kondisi setelah pass 1?  terbesar “menggelembung” ke ujung
(panah 1)

| No  Monster  | Format  | Soal             |              | Kunci            | Pembahasan singkat        |
| ------------ | ------- | ---------------- | ------------ | ---------------- | ------------------------- |
| 8            | F3      |                  |              | [1, 2, 4, 5, 8]  | Hanya 1 pertukaran (4↔2)  |
| Skeleton     |         | Lanjutan  soal   | 7.  Kondisi  |                  |                           |
| Archer       |         | setelah pass 2?  |              |                  |                           |
(panah 2)
9  Skeleton  F2  “Pada  pass  3  tidak  terjadi  Benar  Pass tanpa pertukaran berarti array sudah
| Archer     |     | pertukaran,                     | sehingga  bubble  |     | terurut  |
| ---------- | --- | ------------------------------- | ----------------- | --- | -------- |
| (panah 3)  |     | sort versi optimasi berhenti.”  |                   |     |          |
10  Venomous  F3  Selection sort naik pada [64,  [11, 12, 25, 22, 64]  Pass  1:  11  ditukar  dengan 64. Pass 2:
Arachnid  25, 12, 22, 11]. Kondisi setelah  minimum sisa (12) ditukar dengan 25
pass 2?
11  Grave  F3  Insertion sort naik pada [12,  [5, 11, 12, 13, 6]  5 digeser melewati 13, 12, dan 11 lalu
| Digger  |     |     |     |     | disisipkan di depan  |
| ------- | --- | --- | --- | --- | -------------------- |
11, 13, 5, 6]. Kondisi setelah
|     |     | elemen  ke-4  | (nilai  | 5)  |     |
| --- | --- | ------------- | ------- | --- | --- |
disisipkan?
12  The Abyss  F1  Dari  Bubble,  Selection,  dan  Selection Sort  Selection  sort  menukar  paling  banyak
| Golem  |     | Insertion  Sort,  | mana  yang  |     | sekali per pass  |
| ------ | --- | ----------------- | ----------- | --- | ---------------- |
|        |     | paling  banyak    | hanya       |     |                  |
melakukan n−1 pertukaran?
13  Hydra  of  F3  Gabungkan (merge) [3, 8, 27]  [1, 3, 8, 9, 10, 27]  Bandingkan  elemen  terdepan  kedua
Division  dan [1, 9, 10]  sub-array, ambil yang lebih kecil, ulangi
14  Pivot  F1  Partisi  Quick  Sort  (pivot  =  3  Ada 3 elemen lebih kecil dari 4 (2, 1, 3),
Wraith
elemen terakhir, 4) pada [7, 2,  jadi pivot menempati indeks 3. Hasil: [2, 1,
|     |     | 1, 6, 8, 5, 3, 4]. Di indeks  |     |     | 3, 4, 8, 5, 7, 6]  |
| --- | --- | ----------------------------- | --- | --- | ------------------ |
berapa pivot berakhir?
15  The  F1  Data  1  juta  produk  sudah  Binary  Search,  Sekitar 20 perbandingan dibanding hingga
Sphinx  of  terurut  berdasarkan  ID  dan  O(log n)  1 juta pada linear search
| Entropy  |     | sering  dicari.  | Algoritma  |     |     |
| -------- | --- | ---------------- | ---------- | --- | --- |
terbaik?
16  The  F1  Data kecil yang hampir terurut  Insertion Sort  Pada  data  hampir terurut, insertion sort
mendekati O(n)
| Sphinx   | of  | (hanya 2 elemen salah posisi).  |             |     |     |
| -------- | --- | ------------------------------- | ----------- | --- | --- |
| Entropy  |     | Sorting  dasar                  | mana  yang  |     |     |
paling efisien?
Contoh soal Susun Langkah (F7) milik The Sphinx: potongan baris di bawah ditampilkan acak; pemain
menyusunnya menjadi Binary Search yang benar.
low = 0
high = n - 1
while low <= high:
    mid = (low + high) // 2
    if A[mid] == target: return mid
    elif A[mid] < target: low = mid + 1
    else: high = mid - 1
return -1
11. Generator Soal Prosedural & Anti-Hafalan
Soal dibuat ulang oleh generator setiap kali muncul, jadi pemain yang mati dan mengulang dungeon
tidak bisa menghafal jawaban; mereka harus benar-benar menjalankan algoritmanya.
Cara kerja generator:
1.  Pilih template sesuai monster dan TP (contoh: “kondisi array setelah pass ke-k pada Bubble
Sort”).

2. Bangkitkan array acak sesuai batasan tier: panjang 4–8, nilai 1–99, dengan aturan tambahan
(misalnya tanpa duplikat untuk Act I awal, atau wajib terurut untuk Binary Search).
3. Jalankan algoritma asli di kode game untuk menghasilkan kunci jawaban, sehingga kunci
selalu benar secara otomatis.
4. Bangkitkan distraktor dari miskonsepsi umum (tabel di bawah), bukan angka acak.
5. Validasi: semua pilihan harus berbeda, dan soal ditolak bila terlalu mudah (contoh: array
sudah terurut sejak awal pada soal trace sorting).
6. Simpan seed soal ke log, agar guru bisa melihat ulang soal persis yang dijawab siswa.
Distraktor berbasis miskonsepsi:
Materi Miskonsepsi Cara membuat distraktor
Array Indeks dimulai dari 1 Ambil elemen di indeks k−1
Linear Search Mengembalikan kemunculan terakhir Indeks duplikat terakhir
Linear Search Lupa kasus data tidak ditemukan Pilihan “0” atau indeks terakhir, bukan
-1
Binary Search Membulatkan mid ke atas Hitung mid dengan pembulatan ke atas
Binary Search low = mid (bukan mid + 1) Urutan mid yang tidak bergeser
Binary Search Dipakai pada data acak Opsi jawaban berisi hasil binary
search pada array tak terurut
Bubble Sort Salah menghitung pass (k−1 atau k+1) Kondisi array setelah pass k−1 dan
k+1
Bubble Sort Hanya menukar pasangan pertama Array dengan satu pertukaran saja
Selection Sort Tertukar dengan Bubble Sort Hasil bubble sort pada pass yang sama
Insertion Sort Menukar, bukan menggeser Hasil swap langsung elemen dengan
posisi tujuan
Merge Sort Menggabungkan tanpa Sub-array kiri lalu kanan disambung
membandingkan begitu saja
Quick Sort Pivot dianggap pasti di tengah Indeks tengah array sebagai jawaban
Karena setiap distraktor terhubung ke miskonsepsi, jawaban salah siswa bisa didiagnosis. Contoh:
laporan guru menulis “Siswa A 4 kali memilih distraktor indeks mulai dari 1”.
12. Penskalaan Kesulitan & Mode
Kesulitan diatur lewat 3 lapis: tier soal per monster, mode pilihan pemain, dan penyesuaian
dinamis berdasarkan performa.
Tier soal (dipakai di bestiary):
Tie Panjang Contoh timer
r T dasar array akhir* Dipakai oleh
T1 5 detik 4 7 detik Slime, Lost Wisp

| Tie         | Panjang  | Contoh  | timer  |               |     |     |
| ----------- | -------- | ------- | ------ | ------------- | --- | --- |
| r  T dasar  | array    | akhir*  |        | Dipakai oleh  |     |     |
T2  6 detik  5  8,5 detik  Bookworm, Skeleton Archer (per panah)
T3  8 detik  5–6  10,5 detik  Librarian, Arachnid, Grave Digger, Hydra,
Wraith
T4  12 detik  6–7  15 detik  Boss Act I & II, Entropy fase 1–2
T5  25 detik  7–8  28,5 detik  Sphinx (soal F7), Entropy fase 3
*Dihitung dengan rumus timer di bagian 7 pada mode Scholar tanpa item, memakai panjang array
terkecil. Format interaktif F4 dan F5 mendapat +1 detik per langkah yang diperlukan.
Mode kesulitan (bisa diganti kapan saja di Desa):
|       |        | Batas  | damage  |     |              |     |
| ----- | ------ | ------ | ------- | --- | ------------ | --- |
| Mode  | Timer  | Break  | Save    |     | Untuk siapa  |     |
Story  +50%  25% HP maks  Di  semua  save  point  +  Siswa  yang  baru
|     |     |     | autosave per lantai  |     | belajar materi  |     |
| --- | --- | --- | -------------------- | --- | --------------- | --- |
Scholar  Normal  50% HP maks  Hanya di Patung Malaikat  Mayoritas siswa
(default)
Nightmare  −20%  50%  HP  maks  +  Hanya di Patung Malaikat,  Siswa  yang  sudah
|     |     | Analyze            | tidak  3 kali per Act  |     | mahir,     | mencari  |
| --- | --- | ------------------ | ---------------------- | --- | ---------- | -------- |
|     |     | menampilkan topik  |                        |     | tantangan  |          |
Dynamic Difficulty Adjustment (DDA): game menghitung akurasi 10 soal terakhir.
•  Akurasi < 50%: soal berikutnya memakai array 1 elemen lebih pendek dan timer +2 detik.
•  Akurasi > 85% dan rata-rata sisa waktu > 50%: array 1 elemen lebih panjang.
•  Batas penyesuaian: ±1 tingkat dari tier asli, agar identitas tiap monster tetap terasa.
•  DDA aktif di Story dan Scholar, dan bisa dimatikan guru dalam mode kelas (bagian 16) agar
penilaian adil.
13. Statistik Karakter & Formula Combat
Pemain punya 6 statistik; angka di bawah adalah nilai awal (Lv 1) dan pertumbuhan per level
sebagai titik awal balancing, yang akan disetel ulang saat playtest.
| Statistik  |     | Fungsi          | Nilai awal  |     | Naik per level  |     |
| ---------- | --- | --------------- | ----------- | --- | --------------- | --- |
| HP         |     | Nyawa           | 100         |     | +12             |     |
| Stamina    |     | Biaya  Attack;  | pulih  50   |     | +3              |     |
+15 per giliran
| Memory  |     | Biaya spell (pengganti  | 40  |     | +5  |     |
| ------- | --- | ----------------------- | --- | --- | --- | --- |
Mana)
| ATK  |     | Damage fisik          | 12  |     | +2  |     |
| ---- | --- | --------------------- | --- | --- | --- | --- |
| INT  |     | Pengali damage spell  | 10  |     | +2  |     |

| Statistik  | Fungsi      |     | Nilai awal  |     | Naik per level  |
| ---------- | ----------- | --- | ----------- | --- | --------------- |
| DEF        | Mengurangi  |     | damage  5   |     | +1              |
Break
Rumus utama:
•  Damage Attack = max(1, ATK − DEF musuh) × variasi acak 0,9–1,1.
•  Damage spell = Kekuatan spell × (1 + INT/20); ×1,5 bila mengenai kelemahan musuh
(terlihat setelah Analyze).
•  Counter-Attack (Perfect Guard) = 50% damage Attack, tanpa biaya Stamina.
•  Damage Break = lihat bagian 7, maksimal 50% HP maks (25% pada mode Story).
•  EXP = Lv musuh × 10; bonus +20% bila seluruh soal pertarungan dijawab benar (Flawless).
Target balancing: pertarungan monster biasa selesai dalam 3–5 giliran musuh (3–5 soal), boss
8–12 soal. Dengan begitu satu lantai dungeon (± 4 pertarungan + 1 boss/mini-event) berisi sekitar
20–30 soal, sesuai durasi sesi 15–25 menit.
14. Progresi, Item & Ekonomi
Pemain mengumpulkan Gold (dari semua monster) dan Logic Fragments (hanya dari Perfect
Guard dan pertarungan Flawless), lalu membelanjakannya di Desa Perlindungan. Logic Fragments
sengaja hanya didapat dari jawaban benar dan cepat, agar penguasaan materi menjadi satu-satunya
jalan menuju perlengkapan terbaik.
Senjata (Mordecai si Pandai Besi):
| Senjata             | ATK  | Harga     |     | Efek tambahan        |     |
| ------------------- | ---- | --------- | --- | -------------------- | --- |
| Pedang Tumpul Ordo  | +0   | Awal      |     | —                    |     |
| Pedang Indeks       | +6   | 150 Gold  |     | Counter-Attack +10%  |     |
Bilah Pivot  +12  400 Gold + 10 Fragment  Damage +20% ke musuh Act III
Aegis Blade  +20  900 Gold + 25 Fragment  Perfect  Guard  memulihkan  5
Memory
Senjata lebih kuat mempercepat pertarungan sehingga jumlah soal berkurang, tetapi tidak pernah
membuat soal lebih mudah.
Armor & aksesoris edukatif (passive buff):
| Item  | Harga  | Efek  |     | Catatan desain  |     |
| ----- | ------ | ----- | --- | --------------- | --- |
Hourglass  200 Gold  +3  detik  pada  setiap  Bantuan untuk pemain lambat membaca
| Ring  (Cincin  |     | pop-up  |     |     |     |
| -------------- | --- | ------- | --- | --- | --- |
Waktu)
Helm  of  250 Gold +  Membuang  1  pilihan  Distraktor yang dibuang selalu yang paling
Clarity (Helm  5 Fragment  salah  (4  opsi  menjadi  jauh dari jawaban, agar tetap ada tantangan
| Kejernihan)  |     | 3). Hanya untuk F1 dan  |     |     |     |
| ------------ | --- | ----------------------- | --- | --- | --- |
F3

Item Harga Efek Catatan desain
Kacamata 120 Gold Nomor indeks Item baru, berguna untuk pemula
Indeks ditampilkan lebih besar
dan berwarna
Lentera Hadiah Pada Binary Search, Item baru; pemain cukup menghitung mid
Pointer boss Act I posisi low dan high
disorot otomatis
Sarung 300 Gold + Pada Swap Puzzle, 1 Item baru untuk format F4
Tangan 8 Fragment pertukaran salah
Penukar pertama dimaafkan
Aegis Charm 80 Gold, Memblokir otomatis 1 Maksimal membawa 3
(Jimat habis pakai Break
Pengampunan
)
Boots of 150 Gold Peluang Flee +10% per Tidak berlaku di pertarungan boss
Haste (Sepatu tingkat (maks 3 tingkat)
Kilat)
Batas slot: pemain hanya bisa memakai 2 aksesoris sekaligus, sehingga harus memilih bantuan
yang paling sesuai dengan kelemahannya sendiri.
Barang habis pakai: Potion (+40 HP, 30 Gold), Ether (+25 Memory, 40 Gold), Antidote
(menghapus Poison, 20 Gold).
Ekonomi: rata-rata 1 lantai menghasilkan ± 120–180 Gold dan 3–8 Logic Fragments pada mode
Scholar. Harga di atas dirancang agar 1 upgrade senjata bisa dibeli per Act tanpa grinding
berlebihan.
15. Penalti, Tantangan & Mercy System
Nuansa survival ala Fear & Hunger tetap dipertahankan, tetapi setiap hukuman dipasangkan
dengan jalan keluar yang mengajarkan materi, agar siswa merasa tertantang, bukan putus asa.
Penalti:
• Panic (Mental Fatigue): setiap Break menambah 1 tumpukan Panic, maksimal 3. Efek per
tumpukan: 1 = teks soal sedikit bergetar; 2 = tepi layar kabur; 3 = status Confusion, urutan
tombol pilihan diacak. Satu Perfect Guard menghapus 1 tumpukan; menang pertarungan
menghapus semuanya.
• Save point terbatas: menyimpan hanya di Patung Malaikat yang menyala (1–2 per lantai).
• Mati di dungeon: kembali ke save terakhir, kehilangan 30% Gold yang dibawa (Logic
Fragments aman). Soal pada percobaan berikutnya dibuat ulang oleh generator, sehingga
tidak bisa dihafal.
• Debuff khusus monster: Poison, Silence, Buried, dan lain-lain (lihat bestiary).
Mercy System (penyeimbang):

Pemicu Bantuan yang muncul
Salah 3 kali berturut-turut Bayang-bayang Ordo menawarkan Kilas Codex: animasi 20 detik
pada TP yang sama algoritma tersebut. Pertarungan dijeda, tidak ada penalti
Mati 2 kali di lantai yang Tawaran pindah ke mode Story untuk lantai ini saja (tanpa
sama mengurangi skor laporan)
Mati 3 kali di boss yang Boss memulai dengan HP −15% pada percobaan berikutnya
sama
HP di bawah 20% Musik meredam, timer +1 detik (“adrenalin”), memberi kesempatan
bangkit
Prinsip desain: hukuman membuat pemain berhati-hati, sedangkan Mercy System memastikan
pemain yang kesulitan diarahkan untuk belajar ulang, bukan berhenti bermain.
16. Fitur Pembelajaran
Empat fitur memastikan game ini mengajar, bukan hanya menguji: Codex, Training Grounds,
pembahasan pasca-pertarungan, dan mode kelas untuk guru.
1. Codex Grimoire (materi di dalam game). Setiap halaman Grimoire yang ditemukan membuka
satu entri Codex berisi:
• Penjelasan singkat dengan bahasa sehari-hari (maksimal 150 kata).
• Pseudocode lengkap dengan gaya yang sama seperti di soal.
• Visualisasi animasi: array digambarkan sebagai lilin/batang yang bisa diputar langkah demi
langkah (maju, mundur, kecepatan).
• Tabel kompleksitas: kasus terbaik, rata-rata, terburuk.
• Codex bisa dibuka kapan saja di luar pertarungan.
2. Training Grounds (Arena Latihan di Desa). Boneka latihan yang mengajukan soal TP pilihan
pemain tanpa damage dan tanpa batas waktu (timer opsional). Cocok untuk pemanasan di awal jam
pelajaran.
3. Pembahasan pasca-pertarungan (Battle Review). Setelah menang atau kalah, layar ringkasan
menampilkan soal yang salah, jawaban pemain, kunci, dan replay animasi algoritma pada array soal
tersebut. Pemain mendapat +5 EXP untuk setiap pembahasan yang dibuka, agar membaca
pembahasan terasa bernilai.
4. Mode Kelas & Laporan Guru.
• Guru membuat kelas dan membagikan kode kelas 6 karakter.
• Guru bisa mengunci Act tertentu sesuai materi minggu itu, memilih mode, serta menyalakan
atau mematikan DDA.
• Data yang dikirim ke laporan: akurasi per TP, rata-rata waktu jawab per TP, miskonsepsi
yang paling sering dipilih, progres Act, dan jumlah Codex yang dibuka.
• Laporan bisa diekspor ke CSV atau PDF.
• Pre-test dan post-test 10 soal (generator yang sama, tanpa elemen game) untuk mengukur
peningkatan belajar.

• Privasi: hanya nama panggilan dan kode kelas yang disimpan, tanpa data pribadi lain. Untuk
versi awal, data dapat disimpan lokal dan diekspor manual agar tidak memerlukan server.
17. Atmosfer Visual, Audio, UI/UX & Aksesibilitas
Gaya visual dan audio v1 dipertahankan, ditambah aturan agar array dan angka di pop-up selalu
mudah dibaca walaupun suasananya gelap.
Visual:
• Pixel art 16-bit (resolusi dasar 480×270, di-scale ke 1080p) dengan palet dingin dan gritty:
abu-abu, cokelat kusam, hijau lumut, merah gelap.
• Setiap Act punya warna aksen: Act I biru pucat (cahaya lentera), Act II kuning tulang (lilin),
Act III ungu (sihir pecah).
• Representasi array bertema: buku di rak (Act I), lilin dengan tinggi sesuai nilai (Act II),
kristal yang terbelah (Act III). Tinggi objek sebanding dengan nilainya, sehingga urutan bisa
dilihat sekilas, persis seperti visualisasi batang pada algoritma sorting.
• Elemen yang sedang dibandingkan berkedip kuning; elemen yang sudah di posisi final
menyala hijau redup.
UI pop-up:
• Perkamen usang dengan noda tinta dan darah kering hanya di bingkai; area teks tetap bersih
dengan kontras tinggi (rasio minimal 4,5:1).
• Font pixel yang jelas membedakan 0/O dan 1/l/I, ukuran minimal setara 16 px pada layar
1080p.
• Bar timer di atas perkamen, berubah warna dari biru → kuning → merah.
Audio:
• Eksplorasi: ambient sunyi, tetesan air, angin, langkah kaki menggema.
• Pertarungan: chiptune atau orkestra bertempo cepat dengan dentuman drum.
• Pop-up: detak jarum jam; detak jantung makin cepat saat sisa waktu 30%.
• Hasil: denting logam (Guard), denting + gema kristal (Perfect), kaca pecah (Break).
• Sonifikasi algoritma (opsional): setiap pertukaran elemen memainkan nada sesuai
nilainya, sehingga array yang makin terurut terdengar seperti tangga nada naik.
Aksesibilitas (menu Opsi):
Opsi Pilihan
Pengali timer 1,0× / 1,25× / 1,5× / 2,0× / Tanpa batas (Tanpa batas menonaktifkan Perfect
Guard)
Mode buta warna Protanopia, Deuteranopia, Tritanopia; status selalu punya ikon, tidak hanya
warna
Efek layar Matikan screen shake, efek kabur Panic, dan kilatan merah
Bahasa Indonesia (default), Inggris
Kontrol Keyboard, gamepad, sentuh; semua tombol bisa diatur ulang

| Opsi  | Pilihan                             |     |     |     |
| ----- | ----------------------------------- | --- | --- | --- |
| Teks  | Ukuran font kecil / sedang / besar  |     |     |     |
18. Metrik Keberhasilan, Rencana Uji & Roadmap
Game dinilai berhasil bila nilai post-test naik secara berarti dan siswa tetap ingin bermain; target di
bawah adalah usulan awal yang bisa disesuaikan dengan kebutuhan penelitian atau sekolah.
Metrik keberhasilan:
| Metrik  | Cara ukur  |     |     | Target awal  |
| ------- | ---------- | --- | --- | ------------ |
Peningkatan  Selisih  skor  pre-test  dan  post-test  N-gain ≥ 0,3 (kategori sedang)
| pemahaman  | (N-gain)  |     |     |     |
| ---------- | --------- | --- | --- | --- |
Akurasi per TP  Log soal dalam game  ≥ 70% di akhir Act terkait
| Keterlibatan  | Persentase  | siswa  yang  | menyelesaikan  | ≥ 80%  |
| ------------- | ----------- | ------------ | -------------- | ------ |
Act I
| Kesulitan wajar  | Rata-rata kematian per lantai  |        |                | 0,5–2 kali     |
| ---------------- | ------------------------------ | ------ | -------------- | -------------- |
| Kepuasan         | Kuesioner skala Likert 1–5     |        |                | Rata-rata ≥ 4  |
| Kegunaan materi  | Persentase                     | siswa  | yang  membuka  | ≥ 60%          |
minimal 1 Codex
Rencana uji:
1.  Uji ahli: guru produktif RPL memvalidasi kebenaran soal dan kesesuaian dengan modul
ajar; ahli media menilai UI dan gameplay.
2.  Uji alfa internal: tim memainkan Prolog dan Act I untuk mencari bug serta menyetel timer
dan damage.
3.  Uji terbatas: 1 kelas kecil (10–15 siswa) dengan pre-test, 2 sesi bermain, post-test, dan
wawancara singkat.
4.  Uji lapangan: 1–2 kelas penuh; bandingkan dengan kelas yang belajar tanpa game bila
memungkinkan.
Scope & roadmap:
| Tahap  | Isi  |     | Kriteria selesai  |     |
| ------ | ---- | --- | ----------------- | --- |
1. Prototipe  1  pertarungan  Skeleton  Archer:  Loop  soal  →  hasil  →  damage  terasa
Active Defend System, format F2  menegangkan saat diuji 5 orang
& F3, generator Bubble Sort
2. MVP  Prolog + Act I + Act II, Codex  Bisa  dimainkan  dalam  2–3  jam  pelajaran
|     | dasar,  Battle  | Review,  | mode  tanpa bantuan  |     |
| --- | --------------- | -------- | -------------------- | --- |
Scholar, laporan CSV lokal
3.  Konten  Act III, Final, semua spell & item,  Seluruh 12 TP tercakup dan lolos uji ahli
| penuh  | mode Story/Nightmare, DDA  |     |     |     |
| ------ | -------------------------- | --- | --- | --- |

| Tahap  | Isi  |     |     | Kriteria selesai  |
| ------ | ---- | --- | --- | ----------------- |
4.  Kelas  &  Mode kelas, pre/post-test, ekspor  Lolos uji lapangan
| rilis  | PDF, aksesibilitas lengkap, build  |     |     |     |
| ------ | ---------------------------------- | --- | --- | --- |
Android
5. Pasca-rilis  New  Game+  (varian  Glitched),  Berdasarkan masukan guru dan siswa
|     | sorting   | string,    | algoritma  |     |
| --- | --------- | ---------- | ---------- | --- |
|     | tambahan  | (misalnya  | Counting   |     |
Sort)
Pertanyaan terbuka:
•  Apakah game dipakai sebagai media pembelajaran utama atau latihan/pengayaan setelah
materi diajarkan di kelas?
•  Bahasa pemrograman acuan pseudocode (Python, C++, atau Java) perlu disesuaikan dengan
yang dipakai sekolah.