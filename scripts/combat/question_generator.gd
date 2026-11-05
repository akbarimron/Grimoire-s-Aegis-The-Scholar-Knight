extends Node

# Grimoire's Aegis: Procedural Question & Anti-Memorization Generator
# Implements Sections 7, 8, 10, 11, and 12 of the Game Design Document.
# Covers all 12 Learning Objectives (TP-01 to TP-12), all 13 Monsters,
# all 7 Question Formats (F1 to F7), and Misconception-Tagged Distractors.

enum FormatType {
	F1_MULTIPLE_CHOICE,
	F2_TRUE_FALSE,
	F3_TRACE_ARRAY,
	F4_SWAP_PUZZLE,
	F5_POINTER_HUNT,
	F6_CALCULATION,
	F7_PARSONS_ORDER
}

func generate_question_for_monster(monster_id: String, arrow_index: int = 1) -> Dictionary:
	var q_seed: int = randi()
	seed(q_seed)
	var q: Dictionary = {}
	match monster_id:
		"slime":
			q = _gen_tp01_array_indexing()
		"lost_wisp":
			q = _gen_tp02_linear_search()
		"bookworm_horror", "bookworm":
			q = _gen_tp03_linear_special_cases()
		"twin_faced_librarian", "twin_librarian":
			q = _gen_tp05_binary_search() if randf() > 0.3 else _gen_tp04_binary_precondition()
		"the_blind_archivist":
			q = _gen_tp04_binary_precondition() if randf() > 0.45 else _gen_tp05_binary_search()
		"skeleton_archer":
			q = _gen_tp06_bubble_sort_pass(arrow_index)
		"venomous_arachnid":
			q = _gen_tp07_selection_sort_pass()
		"grave_digger":
			q = _gen_tp08_insertion_sort_step()
		"the_abyss_golem":
			q = _gen_tp09_sorting_comparison()
		"hydra_of_division":
			q = _gen_tp10_merge_sort()
		"pivot_wraith":
			q = _gen_tp11_quick_sort_partition()
		"the_sphinx_of_entropy":
			q = _gen_tp12_bigo_and_parsons()
		"entropy_chaos_king":
			var roll: int = randi() % 5
			match roll:
				0: q = _gen_tp04_binary_precondition()
				1: q = _gen_tp06_bubble_sort_pass(1)
				2: q = _gen_tp10_merge_sort()
				3: q = _gen_tp11_quick_sort_partition()
				_: q = _gen_tp12_bigo_and_parsons()
		_:
			q = _gen_tp01_array_indexing()

	q["seed"] = q_seed
	return q

# Calculate timer based on GDD Section 7 & 12:
# T = (T_base + 0.5 * n_elements + T_item + T_dda + T_adrenalin) * mode_multiplier
func calculate_timer(base_time: float, element_count: int, interactive_steps: int = 0) -> float:
	var dda: Dictionary = GameManager.get_dda_adjustment()
	var t: float = base_time + (0.5 * float(element_count)) + float(interactive_steps)
	t += float(dda.get("timer_bonus", 0.0))

	if GameManager.inventory.get("hourglass_ring", false) or GameManager.equipped_accessories.has("hourglass_ring"):
		t += 3.0
	# Mercy System: Adrenalin (+1s when HP < 20%)
	if float(GameManager.current_hp) < float(GameManager.max_hp) * 0.20:
		t += 1.0

	match GameManager.current_mode:
		GameManager.DifficultyMode.STORY:
			t *= 1.5
		GameManager.DifficultyMode.NIGHTMARE:
			t *= 0.8
	return max(4.0, snappedf(t, 0.1))

func _get_scaled_length(default_len: int, min_len: int = 4, max_len: int = 8) -> int:
	var dda: Dictionary = GameManager.get_dda_adjustment()
	return clampi(default_len + int(dda.get("array_len_delta", 0)), min_len, max_len)

func _generate_unique_array(length: int, min_val: int = 2, max_val: int = 89) -> Array:
	var arr: Array = []
	while arr.size() < length:
		var v: int = randi_range(min_val, max_val)
		if not arr.has(v):
			arr.append(v)
	return arr

func _finalize_options(correct_str: String, distractor_pairs: Array, fallback_min: int = 1, fallback_max: int = 50) -> Dictionary:
	# distractor_pairs is Array of [option_str, misconception_tag]
	var options: Array = [correct_str]
	var misc_map: Dictionary = {}
	for pair in distractor_pairs:
		var opt_s: String = str(pair[0])
		var tag: String = str(pair[1])
		if opt_s != correct_str and not options.has(opt_s):
			options.append(opt_s)
			misc_map[opt_s] = tag
		if options.size() >= 4:
			break

	while options.size() < 4:
		var fake: String = str(randi_range(fallback_min, fallback_max))
		if not options.has(fake):
			options.append(fake)
			misc_map[fake] = "RANDOM_GUESS"

	# Helm of Clarity passive buff: removes 1 wrong option in F1/F3 (4 options -> 3)
	if (GameManager.inventory.get("helm_of_clarity", false) or GameManager.equipped_accessories.has("helm_of_clarity")) and options.size() == 4:
		options.pop_back()

	options.shuffle()
	return { "options": options, "misconception_map": misc_map }

# --- TP-01: Array & 0-Based Indexing (Corrupted Slime) ---
func _gen_tp01_array_indexing() -> Dictionary:
	var arr_len: int = _get_scaled_length(4, 4, 6)
	var arr: Array = _generate_unique_array(arr_len, 3, 45)
	var target_idx: int = randi_range(1, arr_len - 1)
	var correct_val: int = arr[target_idx]

	var distractors: Array = [
		[str(arr[target_idx - 1]), "INDEX_STARTS_AT_1"],
	]
	if target_idx + 1 < arr_len:
		distractors.append([str(arr[target_idx + 1]), "ADJACENT_INDEX_OFFSET"])
	distractors.append([str(arr_len), "CONFUSED_VALUE_WITH_LENGTH"])

	var pack: Dictionary = _finalize_options(str(correct_val), distractors, 2, 49)
	return {
		"format": FormatType.F1_MULTIPLE_CHOICE,
		"format_code": "F1",
		"tp": "TP-01",
		"title": "Fondasi Katalog: Indeks Array (Mulai dari 0)",
		"question": "A = %s\nBerapa nilai elemen pada indeks A[%d]?" % [str(arr), target_idx],
		"array": arr,
		"target_index": target_idx,
		"correct_answer": str(correct_val),
		"options": pack["options"],
		"misconception_map": pack["misconception_map"],
		"explanation": "Indeks dimulai dari 0: A[0]=%d ... sehingga A[%d]=%d." % [arr[0], target_idx, correct_val],
		"timer": calculate_timer(5.0, arr.size())
	}

# --- TP-02: Linear Search & Comparison Count (Lost Wisp) ---
func _gen_tp02_linear_search() -> Dictionary:
	var arr_len: int = _get_scaled_length(5, 4, 6)
	var arr: Array = _generate_unique_array(arr_len, 3, 35)
	# Inject a duplicate so we can test "first occurrence" misconception
	var dup_first_idx: int = randi_range(0, 1)
	var dup_last_idx: int = randi_range(3, arr_len - 1)
	arr[dup_last_idx] = arr[dup_first_idx]
	var target_val: int = arr[dup_first_idx]
	var first_idx: int = arr.find(target_val)
	var comparisons: int = first_idx + 1

	if randi() % 2 == 0:
		var distractors: Array = [
			[str(dup_last_idx), "LINEAR_RETURNS_LAST_DUPLICATE"],
			[str(first_idx + 1), "INDEX_STARTS_AT_1"],
			[str(arr_len), "RETURNED_ARRAY_LENGTH"]
		]
		var pack: Dictionary = _finalize_options(str(first_idx), distractors, 0, arr_len)
		return {
			"format": FormatType.F1_MULTIPLE_CHOICE,
			"format_code": "F1",
			"tp": "TP-02",
			"title": "Hantu Lentera: Linear Search Kemunculan Pertama",
			"question": "Linear Search mengembalikan indeks kemunculan PERTAMA.\nCari %d pada %s. Indeks hasilnya?" % [target_val, str(arr)],
			"array": arr,
			"correct_answer": str(first_idx),
			"options": pack["options"],
			"misconception_map": pack["misconception_map"],
			"explanation": "Pencarian berhenti di kecocokan pertama (indeks %d), tidak lanjut ke indeks %d." % [first_idx, dup_last_idx],
			"timer": calculate_timer(5.0, arr.size())
		}
	else:
		var distractors: Array = [
			[str(first_idx), "FORGOT_ZERO_INDEX_PLUS_ONE_COMPARISONS"],
			[str(dup_last_idx + 1), "LINEAR_RETURNS_LAST_DUPLICATE"],
			[str(arr_len), "CHECKED_ALL_DESPITE_EARLY_MATCH"]
		]
		var pack: Dictionary = _finalize_options(str(comparisons), distractors, 1, arr_len + 1)
		return {
			"format": FormatType.F6_CALCULATION,
			"format_code": "F6",
			"tp": "TP-02",
			"title": "Hitungan Lentera: Jumlah Perbandingan Linear Search",
			"question": "Cari %d pada %s dengan Linear Search.\nBerapa kali perbandingan dilakukan sampai target ditemukan?" % [target_val, str(arr)],
			"array": arr,
			"correct_answer": str(comparisons),
			"options": pack["options"],
			"misconception_map": pack["misconception_map"],
			"explanation": "Target ditemukan di indeks %d sehingga membutuhkan %d perbandingan dari kiri." % [first_idx, comparisons],
			"timer": calculate_timer(5.5, arr.size())
		}

# --- TP-03: Linear Search Special Cases: Not Found / Worst Case (Bookworm Horror) ---
func _gen_tp03_linear_special_cases() -> Dictionary:
	var arr_len: int = _get_scaled_length(5, 4, 6)
	var arr: Array = _generate_unique_array(arr_len, 2, 30)
	var missing_val: int = randi_range(40, 75)
	var correct_str: String = "%d perbandingan, hasil -1" % arr_len
	var distractors: Array = [
		["%d perbandingan, hasil 0" % arr_len, "FORGOT_NOT_FOUND_RETURNS_MINUS_ONE"],
		["%d perbandingan, hasil %d" % [arr_len, arr_len - 1], "RETURNED_LAST_INDEX_ON_NOT_FOUND"],
		["1 perbandingan, hasil -1", "STOPPED_AFTER_FIRST_MISMATCH"]
	]
	var pack: Dictionary = _finalize_options(correct_str, distractors)
	return {
		"format": FormatType.F6_CALCULATION,
		"format_code": "F6",
		"tp": "TP-03",
		"title": "Cacing Pustaka: Kasus Terburuk & Data Tidak Ada",
		"question": "Cari %d pada %s dengan Linear Search.\nBerapa perbandingan dilakukan, dan apa hasilnya?" % [missing_val, str(arr)],
		"array": arr,
		"correct_answer": correct_str,
		"options": pack["options"],
		"misconception_map": pack["misconception_map"],
		"explanation": "Data %d tidak ada, jadi semua %d elemen diperiksa (kasus terburuk = n) dan mengembalikan -1." % [missing_val, arr_len],
		"timer": calculate_timer(6.0, arr.size())
	}

# --- TP-04: Binary Search Precondition (The Blind Archivist / Librarian) ---
func _gen_tp04_binary_precondition() -> Dictionary:
	var arr: Array = [3, 8, 1, 9, 4, 7]
	arr.shuffle()
	# Ensure it is NOT sorted
	if arr == [1, 3, 4, 7, 8, 9]:
		arr = [3, 8, 1, 9, 4, 7]
	var target: int = arr[randi_range(0, arr.size() - 1)]
	var correct_str: String = "Linear Search (Binary Search tidak dapat digunakan)"
	var distractors: Array = [
		["Binary Search (langsung lompat ke mid)", "BINARY_SEARCH_ON_UNSORTED_ARRAY"],
		["Binary Search karena target ada di array", "BINARY_SEARCH_ON_UNSORTED_ARRAY"],
		["Tidak ada algoritma yang bisa mencari", "FALSE_IMPOSSIBILITY"]
	]
	var pack: Dictionary = _finalize_options(correct_str, distractors)
	return {
		"format": FormatType.F1_MULTIPLE_CHOICE,
		"format_code": "F1",
		"tp": "TP-04",
		"title": "Jebakan Arsiparis Buta: Syarat Binary Search",
		"question": "Algoritma mana yang tepat untuk mencari %d pada array %s?" % [target, str(arr)],
		"array": arr,
		"correct_answer": correct_str,
		"options": pack["options"],
		"misconception_map": pack["misconception_map"],
		"explanation": "Binary Search mensyaratkan data WAJIB terurut. Karena array ini acak, gunakan Linear Search.",
		"timer": calculate_timer(8.0, arr.size())
	}

# --- TP-05: Binary Search Pointer Hunt / Mid Trace (Twin-Faced Librarian) ---
func _gen_tp05_binary_search() -> Dictionary:
	var arr_len: int = _get_scaled_length(6, 5, 8)
	var sorted_arr: Array = _generate_unique_array(arr_len, 2, 95)
	sorted_arr.sort()
	# Pick target not at first mid so it takes 2-3 steps
	var low: int = 0
	var high: int = arr_len - 1
	var first_mid: int = (low + high) / 2
	var target_idx: int = (first_mid + 2) if (first_mid + 2 < arr_len) else max(0, first_mid - 2)
	var target_val: int = sorted_arr[target_idx]

	# Trace exact mid sequence
	var mids: Array[String] = []
	var l: int = 0
	var h: int = arr_len - 1
	while l <= h:
		var m: int = (l + h) / 2
		mids.append(str(m))
		if sorted_arr[m] == target_val:
			break
		elif sorted_arr[m] < target_val:
			l = m + 1
		else:
			h = m - 1
	var correct_seq: String = " -> ".join(mids)

	# Misconception 1: rounding mid up (low + high + 1) // 2
	var ceil_mids: Array[String] = []
	l = 0; h = arr_len - 1
	var steps_guard: int = 0
	while l <= h and steps_guard < 5:
		steps_guard += 1
		var m: int = (l + h + 1) / 2
		ceil_mids.append(str(m))
		if sorted_arr[m] == target_val:
			break
		elif sorted_arr[m] < target_val:
			l = m + 1
		else:
			h = m - 1
	var ceil_seq: String = " -> ".join(ceil_mids)
	if ceil_seq == correct_seq:
		ceil_seq = "%d -> %d" % [first_mid + 1, target_idx]

	# Misconception 2: low = mid instead of mid + 1
	var stuck_seq: String = "%d -> %d -> %d" % [first_mid, first_mid, target_idx]
	# Misconception 3: 1-based index
	var one_based_seq: String = "%d -> %d" % [first_mid + 1, target_idx + 1]

	var distractors: Array = [
		[ceil_seq, "BINARY_ROUND_MID_UP"],
		[stuck_seq, "BINARY_LOW_EQUALS_MID"],
		[one_based_seq, "INDEX_STARTS_AT_1"]
	]
	var pack: Dictionary = _finalize_options(correct_seq, distractors)
	return {
		"format": FormatType.F5_POINTER_HUNT,
		"format_code": "F5",
		"tp": "TP-05",
		"title": "Pointer Hunt: Urutan Indeks Mid Binary Search",
		"question": "A = %s, cari target = %d.\nRumus: mid = (low + high) // 2. Tentukan urutan indeks mid yang diperiksa!" % [str(sorted_arr), target_val],
		"array": sorted_arr,
		"low_index": 0,
		"high_index": arr_len - 1,
		"mid_sequence": mids,
		"correct_answer": correct_seq,
		"options": pack["options"],
		"misconception_map": pack["misconception_map"],
		"explanation": "Dengan mid = (low + high) // 2, urutan indeks tengah yang diperiksa adalah %s hingga %d ditemukan." % [correct_seq, target_val],
		"timer": calculate_timer(8.0, sorted_arr.size(), mids.size())
	}

# --- TP-06: Bubble Sort Pass Trace & Optimization (Skeleton Archer) ---
func _gen_tp06_bubble_sort_pass(arrow_index: int = 1) -> Dictionary:
	# Arrow 3 uses F2 True/False about optimized Bubble Sort stopping early
	if arrow_index == 3:
		var correct_tf: String = "Benar"
		var opts: Array = ["Benar", "Salah"]
		return {
			"format": FormatType.F2_TRUE_FALSE,
			"format_code": "F2",
			"tp": "TP-06",
			"title": "Panah Ke-3 Skeleton Archer: Optimasi Bubble Sort",
			"question": "Pernyataan: \"Jika pada suatu pass Bubble Sort tidak terjadi pertukaran sama sekali, maka array sudah terurut dan algoritma versi optimasi berhenti.\"",
			"array": [1, 2, 4, 5, 8],
			"correct_answer": correct_tf,
			"options": opts,
			"misconception_map": { "Salah": "FORGOT_BUBBLE_EARLY_EXIT" },
			"explanation": "Pass tanpa pertukaran membuktikan tidak ada lagi pasangan bersebelahan yang salah urutan.",
			"timer": calculate_timer(6.0, 5)
		}

	var arr_len: int = _get_scaled_length(5, 4, 6)
	var arr: Array = _generate_unique_array(arr_len, 2, 40)
	# Ensure first element is not already minimum so swaps happen
	if arr[0] < arr[1]:
		var tmp: int = arr[0]
		arr[0] = arr[1]
		arr[1] = tmp

	# Run Pass 1 of Bubble Sort
	var pass1: Array = arr.duplicate()
	for i in range(pass1.size() - 1):
		if pass1[i] > pass1[i + 1]:
			var t: int = pass1[i]
			pass1[i] = pass1[i + 1]
			pass1[i + 1] = t

	# Run Pass 2 for distractor (wrong pass count k+1)
	var pass2: Array = pass1.duplicate()
	for i in range(pass2.size() - 2):
		if pass2[i] > pass2[i + 1]:
			var t: int = pass2[i]
			pass2[i] = pass2[i + 1]
			pass2[i + 1] = t

	# Distractor: only swapped first pair
	var first_swap_only: Array = arr.duplicate()
	var t0: int = first_swap_only[0]
	first_swap_only[0] = first_swap_only[1]
	first_swap_only[1] = t0

	# Distractor: fully sorted immediately
	var fully_sorted: Array = arr.duplicate()
	fully_sorted.sort()

	var correct_str: String = str(pass1)
	var distractors: Array = [
		[str(first_swap_only), "BUBBLE_ONLY_FIRST_PAIR_SWAPPED"],
		[str(pass2), "BUBBLE_WRONG_PASS_COUNT"],
		[str(fully_sorted), "BUBBLE_SORTED_IN_ONE_PASS"]
	]
	var pack: Dictionary = _finalize_options(correct_str, distractors)
	return {
		"format": FormatType.F3_TRACE_ARRAY,
		"format_code": "F3",
		"tp": "TP-06",
		"title": "Panah Skeleton Archer: Bubble Sort Pass 1",
		"question": "Bubble Sort naik pada %s.\nBagaimana kondisi array tepat setelah PASS KE-1 selesai?" % str(arr),
		"array": arr,
		"correct_answer": correct_str,
		"options": pack["options"],
		"misconception_map": pack["misconception_map"],
		"explanation": "Pada Pass 1, elemen terbesar menggelembung bertahap ke ujung kanan menghasilkan %s." % correct_str,
		"timer": calculate_timer(6.0, arr.size())
	}

# --- TP-07: Selection Sort Pass Trace (Venomous Arachnid) ---
func _gen_tp07_selection_sort_pass() -> Dictionary:
	var arr_len: int = _get_scaled_length(5, 5, 6)
	var arr: Array = _generate_unique_array(arr_len, 10, 85)
	# Ensure minimum is not at index 0
	var min_val: int = arr[0]
	var min_idx: int = 0
	for i in range(1, arr_len):
		if arr[i] < min_val:
			min_val = arr[i]
			min_idx = i
	if min_idx == 0:
		var swap_i: int = arr_len - 1
		var tmp: int = arr[0]
		arr[0] = arr[swap_i]
		arr[swap_i] = tmp
		min_idx = swap_i
		min_val = arr[min_idx]

	# Selection Sort Pass 1: swap arr[0] with arr[min_idx]
	var sel_pass1: Array = arr.duplicate()
	sel_pass1[min_idx] = sel_pass1[0]
	sel_pass1[0] = min_val

	# Distractor 1: Bubble Sort Pass 1 (Misconception: confused Selection with Bubble)
	var bub_pass1: Array = arr.duplicate()
	for i in range(bub_pass1.size() - 1):
		if bub_pass1[i] > bub_pass1[i + 1]:
			var t: int = bub_pass1[i]
			bub_pass1[i] = bub_pass1[i + 1]
			bub_pass1[i + 1] = t

	# Distractor 2: Shifted instead of swapped (Insertion-like)
	var shifted: Array = arr.duplicate()
	shifted.remove_at(min_idx)
	shifted.push_front(min_val)

	# Distractor 3: Fully sorted
	var sorted_all: Array = arr.duplicate()
	sorted_all.sort()

	var correct_str: String = str(sel_pass1)
	var distractors: Array = [
		[str(bub_pass1), "SELECTION_CONFUSED_WITH_BUBBLE"],
		[str(shifted), "SELECTION_SHIFTED_INSTEAD_OF_SWAPPED"],
		[str(sorted_all), "SORTED_ALL_IN_ONE_PASS"]
	]
	var pack: Dictionary = _finalize_options(correct_str, distractors)
	return {
		"format": FormatType.F4_SWAP_PUZZLE,
		"format_code": "F4",
		"tp": "TP-07",
		"title": "Jaring Arachnid: Selection Sort Pass 1",
		"question": "Selection Sort naik pada %s.\nCari elemen terkecil lalu tukar dengan indeks 0. Bagaimana kondisi setelah Pass 1?" % str(arr),
		"array": arr,
		"swap_pair": [0, min_idx],
		"correct_answer": correct_str,
		"options": pack["options"],
		"misconception_map": pack["misconception_map"],
		"explanation": "Selection Sort memilih nilai minimum (%d di indeks %d) dan langsung menukarnya dengan A[0] (%d)." % [min_val, min_idx, arr[0]],
		"timer": calculate_timer(8.0, arr.size(), 1)
	}

# --- TP-08: Insertion Sort Step Trace (Grave Digger) ---
func _gen_tp08_insertion_sort_step() -> Dictionary:
	# Construct array where first 3 elements are sorted and 4th element (index 3) is small
	var base: Array = _generate_unique_array(4, 15, 65)
	base.sort()
	var key_val: int = randi_range(3, 12)
	var tail_val: int = randi_range(20, 70)
	var arr: Array = [base[0], base[1], base[2], key_val, tail_val]

	# Correct Insertion Sort after inserting index 3 (key_val): shifts base[0..2] right
	var correct_arr: Array = [key_val, base[0], base[1], base[2], tail_val]
	# Misconception: swapped key_val directly with arr[0] instead of shifting!
	var direct_swap_arr: Array = [key_val, base[1], base[2], base[0], tail_val]
	# Misconception: only swapped with left neighbor
	var one_step_arr: Array = [base[0], base[1], key_val, base[2], tail_val]
	var fully_sorted: Array = arr.duplicate()
	fully_sorted.sort()

	var correct_str: String = str(correct_arr)
	var distractors: Array = [
		[str(direct_swap_arr), "INSERTION_SWAPPED_INSTEAD_OF_SHIFTED"],
		[str(one_step_arr), "INSERTION_INCOMPLETE_SHIFT"],
		[str(fully_sorted), "SORTED_UNTOUCHED_ELEMENTS"]
	]
	var pack: Dictionary = _finalize_options(correct_str, distractors)
	return {
		"format": FormatType.F3_TRACE_ARRAY,
		"format_code": "F3",
		"tp": "TP-08",
		"title": "Penggali Kubur: Penyisipan Insertion Sort",
		"question": "Insertion Sort naik pada %s.\nSub-array kiri [%d, %d, %d] sudah terurut. Bagaimana kondisi array setelah elemen ke-4 (nilai %d) disisipkan?" % [str(arr), base[0], base[1], base[2], key_val],
		"array": arr,
		"correct_answer": correct_str,
		"options": pack["options"],
		"misconception_map": pack["misconception_map"],
		"explanation": "Nilai %d digeser melewati %d, %d, dan %d lalu disisipkan di indeks 0 (menggeser, bukan menukar langsung)." % [key_val, base[2], base[1], base[0]],
		"timer": calculate_timer(8.0, arr.size())
	}

# --- TP-09: Comparing Bubble, Selection, Insertion Sort (The Abyss Golem) ---
func _gen_tp09_sorting_comparison() -> Dictionary:
	var q_bank: Array = [
		{
			"q": "Dari Bubble Sort, Selection Sort, dan Insertion Sort, algoritma mana yang paling banyak hanya melakukan n - 1 pertukaran (swap)?",
			"ans": "Selection Sort",
			"opts": [
				["Bubble Sort", "CONFUSED_SWAP_COUNT"],
				["Insertion Sort", "CONFUSED_SHIFT_WITH_MIN_SWAPS"],
				["Ketiganya sama banyak", "IGNORED_SWAP_MECHANISM"]
			],
			"exp": "Selection Sort hanya melakukan paling banyak 1 pertukaran per pass, sehingga total maksimal n - 1 swap."
		},
		{
			"q": "Untuk data berukuran kecil yang sudah HAMPIR TERURUT (hanya 1-2 elemen salah posisi), algoritma sorting dasar mana yang paling efisien mendekati O(n)?",
			"ans": "Insertion Sort",
			"opts": [
				["Selection Sort", "SELECTION_ALWAYS_O_N2"],
				["Quick Sort tanpa partisi", "WRONG_COMPLEXITY_CLASS"],
				["Semua membutuhkan O(n^2) penuh", "IGNORED_BEST_CASE_INSERTION"]
			],
			"exp": "Pada data hampir terurut, perulangan dalam Insertion Sort hampir tidak pernah menggeser elemen sehingga mendekati O(n)."
		}
	]
	var item: Dictionary = q_bank[randi() % q_bank.size()]
	var pack: Dictionary = _finalize_options(item["ans"], item["opts"])
	return {
		"format": FormatType.F1_MULTIPLE_CHOICE,
		"format_code": "F1",
		"tp": "TP-09",
		"title": "Analisis Abyss Golem: Perbandingan 3 Sorting Dasar",
		"question": item["q"],
		"array": [11, 12, 22, 25, 64],
		"correct_answer": item["ans"],
		"options": pack["options"],
		"misconception_map": pack["misconception_map"],
		"explanation": item["exp"],
		"timer": calculate_timer(12.0, 5)
	}

# --- TP-10: Merge Sort Divide & Merge (Hydra of Division) ---
func _gen_tp10_merge_sort() -> Dictionary:
	var raw: Array = _generate_unique_array(6, 1, 45)
	var left: Array = [raw[0], raw[1], raw[2]]
	var right: Array = [raw[3], raw[4], raw[5]]
	left.sort()
	right.sort()

	var merged: Array = (left + right)
	merged.sort()
	var concat_without_compare: Array = left + right
	var reverse_concat: Array = right + left
	var bad_inner: Array = merged.duplicate()
	var tmp: int = bad_inner[2]
	bad_inner[2] = bad_inner[3]
	bad_inner[3] = tmp

	var correct_str: String = str(merged)
	var distractors: Array = [
		[str(concat_without_compare), "MERGE_CONCAT_WITHOUT_COMPARING"],
		[str(reverse_concat), "MERGE_CONCAT_WITHOUT_COMPARING"],
		[str(bad_inner), "MERGE_PARTIAL_COMPARE_ERROR"]
	]
	var pack: Dictionary = _finalize_options(correct_str, distractors)
	return {
		"format": FormatType.F3_TRACE_ARRAY,
		"format_code": "F3",
		"tp": "TP-10",
		"title": "Kepala Hydra: Penggabungan (Merge) Dua Sub-Array",
		"question": "Pada tahap Merge Sort, gabungkan dua sub-array terurut:\nKiri = %s dan Kanan = %s" % [str(left), str(right)],
		"array": left + right,
		"correct_answer": correct_str,
		"options": pack["options"],
		"misconception_map": pack["misconception_map"],
		"explanation": "Bandingkan elemen terdepan kedua sub-array satu per satu dan ambil yang lebih kecil: %s." % correct_str,
		"timer": calculate_timer(8.0, 6)
	}

# --- TP-11: Quick Sort Partition & Final Pivot Index (Pivot Wraith) ---
func _gen_tp11_quick_sort_partition() -> Dictionary:
	var arr_len: int = _get_scaled_length(6, 5, 7)
	var arr: Array = _generate_unique_array(arr_len, 2, 50)
	var pivot_val: int = arr[arr_len - 1]
	var smaller_count: int = 0
	for i in range(arr_len - 1):
		if arr[i] < pivot_val:
			smaller_count += 1

	var final_pivot_idx: int = smaller_count
	var mid_idx: int = (arr_len - 1) / 2
	var distractors: Array = [
		[str(mid_idx), "QUICK_PIVOT_ALWAYS_IN_MIDDLE"],
		[str(smaller_count + 1), "INDEX_STARTS_AT_1"],
		[str(arr_len - 1), "PIVOT_NEVER_MOVED"]
	]
	var pack: Dictionary = _finalize_options(str(final_pivot_idx), distractors, 0, arr_len - 1)
	return {
		"format": FormatType.F4_SWAP_PUZZLE,
		"format_code": "F4",
		"tp": "TP-11",
		"title": "Arwah Poros: Posisi Akhir Pivot Quick Sort",
		"question": "Partisi Quick Sort (pivot = elemen terakhir, %d) pada %s.\nSemua elemen < %d didorong ke kiri. Di indeks berapa pivot %d berakhir?" % [pivot_val, str(arr), pivot_val, pivot_val],
		"array": arr,
		"correct_answer": str(final_pivot_idx),
		"options": pack["options"],
		"misconception_map": pack["misconception_map"],
		"explanation": "Ada %d elemen yang lebih kecil dari pivot %d, sehingga pivot menempati indeks %d." % [smaller_count, pivot_val, final_pivot_idx],
		"timer": calculate_timer(8.5, arr.size(), 1)
	}

# --- TP-12: Big-O Selection & Parsons Problem (The Sphinx of Entropy / Final) ---
func _gen_tp12_bigo_and_parsons() -> Dictionary:
	if randi() % 2 == 0:
		var correct_str: String = "Binary Search — O(log n)"
		var distractors: Array = [
			["Linear Search — O(n)", "CHOSE_LINEAR_FOR_LARGE_SORTED_DATA"],
			["Bubble Sort lalu Linear Search — O(n^2)", "UNNECESSARY_RESORTING"],
			["Selection Sort — O(n^2)", "CONFUSED_SORTING_WITH_SEARCHING"]
		]
		var pack: Dictionary = _finalize_options(correct_str, distractors)
		return {
			"format": FormatType.F1_MULTIPLE_CHOICE,
			"format_code": "F1",
			"tp": "TP-12",
			"title": "Teka-Teki Sphinx: Evaluasi Kompleksitas Big-O",
			"question": "Katalog Agung memiliki 1.000.000 buku yang SUDAH TERURUT berdasarkan ID dan sangat sering dicari.\nAlgoritma pencarian mana yang paling efisien?",
			"array": [10, 20, 30, 40, 50, 60, 70],
			"correct_answer": correct_str,
			"options": pack["options"],
			"misconception_map": pack["misconception_map"],
			"explanation": "Pada 1 juta data terurut, Binary Search O(log n) hanya butuh maksimal ~20 perbandingan dibanding 1 juta pada Linear Search.",
			"timer": calculate_timer(14.0, 7)
		}
	else:
		# Format F7: Parsons Problem (Susun Langkah Binary Search)
		var correct_order: String = "1) mid = (low+high)//2  ->  2) if A[mid]==target  ->  3) elif A[mid]<target: low=mid+1"
		var distractors: Array = [
			["1) if A[mid]==target  ->  2) mid = (low+high)//2  ->  3) low = mid", "PARSONS_CHECK_BEFORE_COMPUTING_MID"],
			["1) mid = (low+high)//2  ->  2) elif A[mid]<target: high=mid-1  ->  3) return -1", "PARSONS_INVERTED_LOW_HIGH_UPDATE"],
			["1) low = mid + 1  ->  2) mid = (low+high)//2  ->  3) while low <= high", "PARSONS_LOOP_ORDER_BROKEN"]
		]
		var pack: Dictionary = _finalize_options(correct_order, distractors)
		return {
			"format": FormatType.F7_PARSONS_ORDER,
			"format_code": "F7",
			"tp": "TP-12",
			"title": "Susun Mantra (F7 Parsons Problem): Inti Binary Search",
			"question": "Susun urutan langkah di dalam perulangan 'while low <= high:' pada Binary Search yang benar!",
			"array": [2, 5, 8, 12, 16, 23, 38],
			"correct_answer": correct_order,
			"options": pack["options"],
			"misconception_map": pack["misconception_map"],
			"explanation": "Hitung titik tengah mid terlebih dahulu, periksa kecocokan A[mid], lalu perbarui batas low atau high.",
			"timer": calculate_timer(20.0, 7)
		}
