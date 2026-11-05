extends Node

# Grimoire's Aegis: The Scholar Knight - Core Game Manager
# Implements Sections 6, 9, 12, 13, 14, 15, and 16 of the Game Design Document.

signal stats_changed
signal panic_changed(level: int)
signal combat_ended(victory: bool)
signal mercy_kilas_codex_triggered(tp_code: String)

# Difficulty Modes (GDD Section 12)
enum DifficultyMode { STORY, SCHOLAR, NIGHTMARE }
var current_mode: DifficultyMode = DifficultyMode.SCHOLAR
var dda_enabled: bool = true

# Class Mode / Teacher Telemetry (GDD Section 16.4)
var student_nickname: String = "Scholar_01"
var class_code: String = "RPL26A"
var locked_acts: Array[int] = [] # Empty = all unlocked for testing

# Player Core Stats (Lv 1 Base Stats per GDD Section 13)
var level: int = 1
var exp_points: int = 0
var max_hp: int = 100
var current_hp: int = 100
var max_stamina: int = 50
var current_stamina: int = 50
var max_memory: int = 40
var current_memory: int = 40
var base_atk: int = 12
var intelligence: int = 10
var defense_power: int = 5

# Computed Attack Power including equipped weapon
var attack_power: int:
	get:
		var bonus: int = WEAPONS.get(equipped_weapon, {}).get("atk_bonus", 0)
		return base_atk + bonus

# Economy (GDD Section 14)
var gold: int = 100
var logic_fragments: int = 0

# Panic / Mental Fatigue (0 to 3 per GDD Section 15)
# 1 = question text shakes slightly
# 2 = screen edges blur
# 3 = choice buttons are shuffled / confusion
var panic_level: int = 0:
	set(val):
		panic_level = clampi(val, 0, 3)
		panic_changed.emit(panic_level)

# Equipment & Inventory (GDD Section 14)
var equipped_weapon: String = "pedang_tumpul_ordo"
var owned_weapons: Array[String] = ["pedang_tumpul_ordo"]
var equipped_accessories: Array[String] = [] # Max 2 slots per GDD Section 14

var inventory: Dictionary = {
	"potion": 3,                  # +40 HP (30 Gold)
	"ether": 2,                   # +25 Memory (40 Gold)
	"antidote": 1,                # Cure Poison (20 Gold)
	"aegis_charm": 1,             # Auto-blocks 1 Break, max 3 (80 Gold)
	"boots_of_haste_level": 0,    # +10% Flee per level, max 3 (150 Gold)
	"hourglass_ring": false,      # +3s on every pop-up (200 Gold)
	"helm_of_clarity": false,     # Removes 1 furthest distractor in F1/F3 (250G + 5 Frag)
	"kacamata_indeks": false,     # Enlarges and highlights index numbers (120 Gold)
	"lentera_pointer": false,     # Highlights low & high in Binary Search (Boss Act I reward)
	"sarung_tangan_penukar": false # Forgives first wrong swap in F4 (300G + 8 Frag)
}

# Weapons Database (Mordecai si Pandai Besi - GDD Section 14)
const WEAPONS: Dictionary = {
	"pedang_tumpul_ordo": {
		"name": "Pedang Tumpul Ordo",
		"atk_bonus": 0,
		"gold_cost": 0,
		"fragment_cost": 0,
		"effect_desc": "Senjata awal Scholar Knight."
	},
	"pedang_indeks": {
		"name": "Pedang Indeks",
		"atk_bonus": 6,
		"gold_cost": 150,
		"fragment_cost": 0,
		"effect_desc": "Counter-Attack (Perfect Guard) +10% damage."
	},
	"bilah_pivot": {
		"name": "Bilah Pivot",
		"atk_bonus": 12,
		"gold_cost": 400,
		"fragment_cost": 10,
		"effect_desc": "Damage +20% terhadap musuh Act III."
	},
	"aegis_blade": {
		"name": "Aegis Blade",
		"atk_bonus": 20,
		"gold_cost": 900,
		"fragment_cost": 25,
		"effect_desc": "Perfect Guard memulihkan +5 Memory."
	}
}

# Spells unlocked per Act progression (GDD Section 6)
var unlocked_spells: Array[String] = [
	"linear_strike",
	"binary_lance",
	"bubble_shift",
	"selection_mark",
	"insertion_ward",
	"merge_barrage",
	"quick_pivot",
	"order_restored"
]

const SPELLS: Dictionary = {
	"linear_strike": {
		"name": "Linear Strike",
		"cost": 8,
		"power": 18,
		"desc": "Menyerang musuh satu per satu dari kiri ke kanan; berhenti saat mengenai target yang ditandai.",
		"act": 1
	},
	"binary_lance": {
		"name": "Binary Lance",
		"cost": 14,
		"power": 32,
		"desc": "Damage besar ke satu musuh. Hanya bisa dipakai bila musuh berstatus Ordered (syarat Binary Search).",
		"act": 1
	},
	"bubble_shift": {
		"name": "Bubble Shift",
		"cost": 10,
		"power": 16,
		"desc": "Menukar posisi musuh bersebelahan dan mendorong musuh terlemah ke depan (+debuff Ordered).",
		"act": 2
	},
	"selection_mark": {
		"name": "Selection Mark",
		"cost": 12,
		"power": 26,
		"desc": "Menandai musuh dengan HP terkecil lalu memberi damage kritis kepadanya.",
		"act": 2
	},
	"insertion_ward": {
		"name": "Insertion Ward",
		"cost": 12,
		"power": 0,
		"desc": "Menyisipkan perisai ke giliran berikutnya: mengurangi damage serangan berikutnya 30%.",
		"act": 2
	},
	"merge_barrage": {
		"name": "Merge Barrage",
		"cost": 20,
		"power": 42,
		"desc": "Memecah serangan menjadi 2, lalu 4 tebasan kecil yang digabung menjadi satu combo.",
		"act": 3
	},
	"quick_pivot": {
		"name": "Quick Pivot",
		"cost": 18,
		"power": 35,
		"desc": "Memilih musuh sebagai pivot: memberi damage sekaligus debuff DEF turun.",
		"act": 3
	},
	"order_restored": {
		"name": "Order Restored",
		"cost": 30,
		"power": 25,
		"desc": "Memberi debuff Ordered ke musuh (DEF turun 25%, soal berikutnya mendapat +2 detik).",
		"act": 4
	}
}

# Full 13-Monster Bestiary (GDD Section 9 & Section 12)
const BESTIARY: Dictionary = {
	"slime": {
		"id": "slime",
		"name": "Corrupted Slime",
		"title": "Prolog: Array & Indeks (TP-01)",
		"act": 0,
		"level": 1,
		"max_hp": 50,
		"atk": 9,
		"def": 2,
		"exp": 10,
		"gold": 25,
		"tier": "T1",
		"base_timer": 5.0,
		"tp_list": ["TP-01"],
		"formats": ["F1", "F2"],
		"weakness": "linear_strike",
		"debuff_on_fail": "none",
		"sprite": "res://assets/sprites/monsters/corrupted_slime.png"
	},
	"lost_wisp": {
		"id": "lost_wisp",
		"name": "Lost Wisp (Hantu Lentera)",
		"title": "Act I: Linear Search Dasar (TP-02)",
		"act": 1,
		"level": 2,
		"max_hp": 62,
		"atk": 11,
		"def": 3,
		"exp": 20,
		"gold": 35,
		"tier": "T1",
		"base_timer": 5.0,
		"tp_list": ["TP-02"],
		"formats": ["F1", "F6"],
		"weakness": "linear_strike",
		"debuff_on_fail": "blind",
		"sprite": "res://assets/sprites/monsters/lost_wisp.png"
	},
	"bookworm_horror": {
		"id": "bookworm_horror",
		"name": "Bookworm Horror (Cacing Pustaka)",
		"title": "Act I: Linear Search Kasus Khusus (TP-03)",
		"act": 1,
		"level": 3,
		"max_hp": 72,
		"atk": 12,
		"def": 3,
		"exp": 30,
		"gold": 42,
		"tier": "T2",
		"base_timer": 6.0,
		"tp_list": ["TP-03"],
		"formats": ["F1", "F6"],
		"weakness": "linear_strike",
		"debuff_on_fail": "double_bite",
		"sprite": "res://assets/sprites/monsters/bookworm_horror.png"
	},
	"twin_faced_librarian": {
		"id": "twin_faced_librarian",
		"name": "Twin-Faced Librarian",
		"title": "Act I: Binary Search (TP-04, TP-05)",
		"act": 1,
		"level": 4,
		"max_hp": 85,
		"atk": 13,
		"def": 4,
		"exp": 40,
		"gold": 50,
		"tier": "T3",
		"base_timer": 8.0,
		"tp_list": ["TP-04", "TP-05"],
		"formats": ["F5", "F6"],
		"weakness": "binary_lance",
		"debuff_on_fail": "torn_page",
		"sprite": "res://assets/sprites/monsters/twin_faced_librarian.png"
	},
	"the_blind_archivist": {
		"id": "the_blind_archivist",
		"name": "The Blind Archivist (Boss Act I)",
		"title": "Boss Act I: Linear vs Binary Search (TP-02..05)",
		"act": 1,
		"level": 5,
		"is_boss": true,
		"max_hp": 130,
		"atk": 15,
		"def": 5,
		"exp": 50,
		"gold": 90,
		"tier": "T4",
		"base_timer": 12.0,
		"tp_list": ["TP-02", "TP-03", "TP-04", "TP-05"],
		"formats": ["F1", "F5", "F6"],
		"weakness": "binary_lance",
		"debuff_on_fail": "silence",
		"sprite": "res://assets/sprites/monsters/the_blind_archivist.png"
	},
	"skeleton_archer": {
		"id": "skeleton_archer",
		"name": "Skeleton Archer",
		"title": "Act II: Bubble Sort (TP-06)",
		"act": 2,
		"level": 6,
		"max_hp": 80,
		"atk": 14,
		"def": 4,
		"exp": 60,
		"gold": 55,
		"tier": "T2",
		"base_timer": 6.0,
		"tp_list": ["TP-06"],
		"formats": ["F3", "F2"],
		"weakness": "bubble_shift",
		"debuff_on_fail": "arrow_volley",
		"sprite": "res://assets/sprites/monsters/skeleton_archer.png"
	},
	"venomous_arachnid": {
		"id": "venomous_arachnid",
		"name": "Venomous Arachnid",
		"title": "Act II: Selection Sort (TP-07)",
		"act": 2,
		"level": 7,
		"max_hp": 92,
		"atk": 15,
		"def": 5,
		"exp": 70,
		"gold": 62,
		"tier": "T3",
		"base_timer": 8.0,
		"tp_list": ["TP-07"],
		"formats": ["F3", "F4"],
		"weakness": "selection_mark",
		"debuff_on_fail": "poison",
		"sprite": "res://assets/sprites/monsters/venomous_arachnid.png"
	},
	"grave_digger": {
		"id": "grave_digger",
		"name": "Grave Digger (Penggali Kubur)",
		"title": "Act II: Insertion Sort (TP-08)",
		"act": 2,
		"level": 8,
		"max_hp": 105,
		"atk": 16,
		"def": 6,
		"exp": 80,
		"gold": 70,
		"tier": "T3",
		"base_timer": 8.0,
		"tp_list": ["TP-08"],
		"formats": ["F3", "F4"],
		"weakness": "insertion_ward",
		"debuff_on_fail": "buried",
		"sprite": "res://assets/sprites/monsters/grave_digger.png"
	},
	"the_abyss_golem": {
		"id": "the_abyss_golem",
		"name": "The Abyss Golem (Boss Act II)",
		"title": "Boss Act II: Evaluasi 3 Sorting Dasar (TP-06..09)",
		"act": 2,
		"level": 9,
		"is_boss": true,
		"max_hp": 165,
		"atk": 18,
		"def": 7,
		"exp": 90,
		"gold": 120,
		"tier": "T4",
		"base_timer": 12.0,
		"tp_list": ["TP-06", "TP-07", "TP-08", "TP-09"],
		"formats": ["F1", "F3", "F4"],
		"weakness": "selection_mark",
		"debuff_on_fail": "crushed",
		"sprite": "res://assets/sprites/monsters/the_abyss_golem.png"
	},
	"hydra_of_division": {
		"id": "hydra_of_division",
		"name": "Hydra of Division",
		"title": "Act III: Merge Sort Divide & Merge (TP-10)",
		"act": 3,
		"level": 10,
		"max_hp": 120,
		"atk": 19,
		"def": 6,
		"exp": 100,
		"gold": 80,
		"tier": "T3",
		"base_timer": 8.0,
		"tp_list": ["TP-10"],
		"formats": ["F3", "F7"],
		"weakness": "merge_barrage",
		"debuff_on_fail": "hydra_regen",
		"sprite": "res://assets/sprites/monsters/hydra_of_division.png"
	},
	"pivot_wraith": {
		"id": "pivot_wraith",
		"name": "Pivot Wraith (Arwah Poros)",
		"title": "Act III: Quick Sort Partition (TP-11)",
		"act": 3,
		"level": 11,
		"max_hp": 128,
		"atk": 20,
		"def": 7,
		"exp": 110,
		"gold": 88,
		"tier": "T3",
		"base_timer": 8.0,
		"tp_list": ["TP-11"],
		"formats": ["F3", "F4"],
		"weakness": "quick_pivot",
		"debuff_on_fail": "split",
		"sprite": "res://assets/sprites/monsters/pivot_wraith.png"
	},
	"the_sphinx_of_entropy": {
		"id": "the_sphinx_of_entropy",
		"name": "The Sphinx of Entropy (Boss Act III)",
		"title": "Boss Act III: Big-O & Pemilihan Algoritma (TP-12)",
		"act": 3,
		"level": 12,
		"is_boss": true,
		"max_hp": 190,
		"atk": 22,
		"def": 8,
		"exp": 120,
		"gold": 150,
		"tier": "T5",
		"base_timer": 25.0,
		"tp_list": ["TP-12"],
		"formats": ["F1", "F7"],
		"weakness": "merge_barrage",
		"debuff_on_fail": "death_seal",
		"sprite": "res://assets/sprites/monsters/the_sphinx_of_entropy.png"
	},
	"entropy_chaos_king": {
		"id": "entropy_chaos_king",
		"name": "Entropy, Raja Kekacauan (Boss Final)",
		"title": "Final Act: Pemulihan Katalog Agung (Semua TP)",
		"act": 4,
		"level": 15,
		"is_boss": true,
		"max_hp": 240,
		"atk": 25,
		"def": 9,
		"exp": 200,
		"gold": 300,
		"tier": "T5",
		"base_timer": 18.0,
		"tp_list": ["TP-01", "TP-02", "TP-04", "TP-05", "TP-06", "TP-07", "TP-08", "TP-09", "TP-10", "TP-11", "TP-12"],
		"formats": ["F1", "F2", "F3", "F4", "F5", "F6", "F7"],
		"weakness": "order_restored",
		"debuff_on_fail": "unshuffle_catalog",
		"sprite": "res://assets/sprites/monsters/entropy_chaos_king.png"
	}
}

# Codex Grimoire Entries (GDD Section 16.1)
const CODEX_ENTRIES: Dictionary = {
	"TP-01": {
		"title": "Halaman I: Array & Indeks Berbasis 0",
		"summary": "Array menyimpan sekumpulan elemen secara berurutan di memori. Penomoran posisi (indeks) selalu dimulai dari 0, sehingga elemen pertama berada di A[0] dan elemen terakhir berada di A[n-1].",
		"pseudocode": "A = [4, 9, 7, 1]\nn = len(A)      # panjang array = 4\npertama = A[0]  # bernilai 4\nketiga  = A[2]  # bernilai 7",
		"complexity": "Akses Indeks: Terbaik O(1) | Rata-rata O(1) | Terburuk O(1)"
	},
	"TP-02": {
		"title": "Halaman II: Linear Search",
		"summary": "Linear Search memeriksa elemen satu per satu dari indeks 0 ke kanan hingga target ditemukan. Jika ada duplikat, indeks kemunculan pertama yang dikembalikan. Jika tidak ada, kembalikan -1.",
		"pseudocode": "def linear_search(A, target):\n    for i in range(len(A)):\n        if A[i] == target:\n            return i\n    return -1",
		"complexity": "Terbaik O(1) | Rata-rata O(n) | Terburuk O(n)"
	},
	"TP-04": {
		"title": "Halaman III: Binary Search",
		"summary": "Binary Search membagi ruang pencarian menjadi dua di setiap langkah dengan titik tengah mid = (low + high) // 2. Syarat mutlak: data array WAJIB sudah terurut.",
		"pseudocode": "low = 0\nhigh = n - 1\nwhile low <= high:\n    mid = (low + high) // 2\n    if A[mid] == target: return mid\n    elif A[mid] < target: low = mid + 1\n    else: high = mid - 1\nreturn -1",
		"complexity": "Terbaik O(1) | Rata-rata O(log n) | Terburuk O(log n)"
	},
	"TP-06": {
		"title": "Halaman IV: Bubble Sort",
		"summary": "Bubble Sort membandingkan dua elemen bersebelahan dan menukarnya bila urutannya salah. Setiap pass mendorong elemen terbesar ke ujung kanan. Versi optimasi berhenti jika dalam 1 pass tidak ada pertukaran.",
		"pseudocode": "for pass_idx in range(n - 1):\n    swapped = False\n    for i in range(n - 1 - pass_idx):\n        if A[i] > A[i + 1]:\n            A[i], A[i + 1] = A[i + 1], A[i]\n            swapped = True\n    if not swapped: break",
		"complexity": "Terbaik O(n) | Rata-rata O(n^2) | Terburuk O(n^2) | Stabil: Ya"
	},
	"TP-07": {
		"title": "Halaman V: Selection Sort",
		"summary": "Selection Sort mencari elemen terkecil dari bagian array yang belum terurut, lalu menukarnya tepat satu kali ke posisi depan pass tersebut. Paling hemat jumlah swap (maksimal n-1 kali).",
		"pseudocode": "for i in range(n - 1):\n    min_idx = i\n    for j in range(i + 1, n):\n        if A[j] < A[min_idx]:\n            min_idx = j\n    A[i], A[min_idx] = A[min_idx], A[i]",
		"complexity": "Terbaik O(n^2) | Rata-rata O(n^2) | Terburuk O(n^2) | Swap maks: n-1"
	},
	"TP-08": {
		"title": "Halaman VI: Insertion Sort",
		"summary": "Insertion Sort mengambil satu elemen kunci lalu menggeser elemen-elemen di sebelah kirinya yang lebih besar hingga menemukan posisi sisip yang tepat. Sangat cepat pada data yang hampir terurut.",
		"pseudocode": "for i in range(1, n):\n    key = A[i]\n    j = i - 1\n    while j >= 0 and A[j] > key:\n        A[j + 1] = A[j]\n        j -= 1\n    A[j + 1] = key",
		"complexity": "Terbaik O(n) (hampir terurut) | Rata-rata O(n^2) | Terburuk O(n^2) | Stabil: Ya"
	},
	"TP-10": {
		"title": "Halaman VII: Merge Sort",
		"summary": "Merge Sort membelah array menjadi dua sub-array secara rekursif (divide), lalu menggabungkannya kembali (merge) dengan membandingkan elemen terdepan kedua sub-array yang sudah terurut.",
		"pseudocode": "def merge(left, right):\n    result = []\n    while left and right:\n        if left[0] <= right[0]: result.append(left.pop(0))\n        else: result.append(right.pop(0))\n    return result + left + right",
		"complexity": "Terbaik O(n log n) | Rata-rata O(n log n) | Terburuk O(n log n) | Stabil: Ya"
	},
	"TP-11": {
		"title": "Halaman VIII: Quick Sort",
		"summary": "Quick Sort memilih satu elemen sebagai pivot, lalu mempartisi array sehingga semua elemen lebih kecil berada di kiri pivot dan yang lebih besar di kanan pivot.",
		"pseudocode": "pivot = A[high]\ni = low\nfor j in range(low, high):\n    if A[j] < pivot:\n        A[i], A[j] = A[j], A[i]\n        i += 1\nA[i], A[high] = A[high], A[i] # i adalah indeks akhir pivot",
		"complexity": "Terbaik O(n log n) | Rata-rata O(n log n) | Terburuk O(n^2)"
	}
}

# Dynamic Difficulty Adjustment (DDA) & Telemetry State (GDD Section 12 & 16)
var recent_results: Array[Dictionary] = [] # Rolling window max 10: {correct: bool, time_ratio: float}
var wrong_streak_by_tp: Dictionary = {}
var question_history_log: Array[Dictionary] = []
var battle_wrong_questions: Array[Dictionary] = []
var codex_opened_count: int = 0
var boss_deaths: Dictionary = {}

func _ready() -> void:
	reset_player_to_defaults()

func reset_player_to_defaults() -> void:
	current_hp = max_hp
	current_stamina = max_stamina
	current_memory = max_memory
	panic_level = 0
	stats_changed.emit()

func restore_turn_stamina() -> void:
	current_stamina = min(max_stamina, current_stamina + 15)
	stats_changed.emit()

func heal_hp(amount: int) -> void:
	current_hp = min(max_hp, current_hp + amount)
	stats_changed.emit()

func restore_memory(amount: int) -> void:
	current_memory = min(max_memory, current_memory + amount)
	stats_changed.emit()

func take_damage(amount: int) -> void:
	current_hp = max(0, current_hp - amount)
	stats_changed.emit()

func add_panic(amount: int = 1) -> void:
	panic_level += amount

func clear_one_panic() -> void:
	if panic_level > 0:
		panic_level -= 1

func clear_all_panic() -> void:
	panic_level = 0

func add_reward(gold_gain: int, fragment_gain: int, exp_gain: int) -> void:
	gold += gold_gain
	logic_fragments += fragment_gain
	exp_points += exp_gain
	var req_exp: int = level * 50
	while exp_points >= req_exp:
		exp_points -= req_exp
		level_up()
		req_exp = level * 50
	stats_changed.emit()

func level_up() -> void:
	level += 1
	max_hp += 12
	current_hp = max_hp
	max_stamina += 3
	current_stamina = max_stamina
	max_memory += 5
	current_memory = max_memory
	base_atk += 2
	intelligence += 2
	defense_power += 1
	stats_changed.emit()

# --- DDA & MERCY SYSTEM (GDD Section 12 & 15) ---

func get_dda_adjustment() -> Dictionary:
	# Returns { "array_len_delta": int, "timer_bonus": float }
	if not dda_enabled or current_mode == DifficultyMode.NIGHTMARE or recent_results.size() < 3:
		return { "array_len_delta": 0, "timer_bonus": 0.0 }
	var correct_count: int = 0
	var total_time_ratio: float = 0.0
	for r in recent_results:
		if r.get("correct", false):
			correct_count += 1
		total_time_ratio += float(r.get("time_ratio", 0.0))
	var acc: float = float(correct_count) / float(recent_results.size())
	var avg_time_ratio: float = total_time_ratio / float(recent_results.size())
	if acc < 0.50:
		return { "array_len_delta": -1, "timer_bonus": 2.0 }
	elif acc > 0.85 and avg_time_ratio > 0.50:
		return { "array_len_delta": 1, "timer_bonus": 0.0 }
	return { "array_len_delta": 0, "timer_bonus": 0.0 }

func record_question_attempt(q_data: Dictionary, chosen: String, is_correct: bool, time_left_ratio: float, time_spent: float) -> void:
	var tp: String = q_data.get("tp", "TP-01")
	var misconception: String = "NONE"
	if not is_correct:
		var misc_map: Dictionary = q_data.get("misconception_map", {})
		misconception = misc_map.get(chosen, "TIMEOUT_OR_OTHER")
		wrong_streak_by_tp[tp] = int(wrong_streak_by_tp.get(tp, 0)) + 1
		battle_wrong_questions.append({
			"tp": tp,
			"title": q_data.get("title", ""),
			"question": q_data.get("question", ""),
			"chosen": chosen,
			"correct": q_data.get("correct_answer", ""),
			"explanation": q_data.get("explanation", ""),
			"reviewed": false
		})
		# Mercy trigger: 3 consecutive wrong on the same TP -> Kilas Codex
		if int(wrong_streak_by_tp[tp]) >= 3:
			wrong_streak_by_tp[tp] = 0
			mercy_kilas_codex_triggered.emit(tp)
	else:
		wrong_streak_by_tp[tp] = 0

	recent_results.append({ "correct": is_correct, "time_ratio": time_left_ratio })
	if recent_results.size() > 10:
		recent_results.pop_front()

	question_history_log.append({
		"timestamp": Time.get_datetime_string_from_system(),
		"student": student_nickname,
		"class_code": class_code,
		"seed": q_data.get("seed", 0),
		"tp": tp,
		"format": q_data.get("format_code", "F1"),
		"correct": is_correct,
		"time_spent_sec": snappedf(time_spent, 0.01),
		"misconception": misconception
	})

func export_teacher_report_csv(custom_path: String = "user://grimoires_aegis_teacher_report.csv") -> String:
	var file := FileAccess.open(custom_path, FileAccess.WRITE)
	if file == null:
		return ""
	file.store_line("timestamp,student,class_code,seed,tp,format,correct,time_spent_sec,misconception")
	for row in question_history_log:
		file.store_line("%s,%s,%s,%s,%s,%s,%s,%s,%s" % [
			row["timestamp"],
			row["student"],
			row["class_code"],
			str(row["seed"]),
			row["tp"],
			row["format"],
			"1" if row["correct"] else "0",
			str(row["time_spent_sec"]),
			row["misconception"]
		])
	file.close()
	return ProjectSettings.globalize_path(custom_path)
