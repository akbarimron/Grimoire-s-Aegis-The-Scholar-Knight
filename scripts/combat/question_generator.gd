extends Node

# Grimoire's Aegis: Procedural Question Generator
# Generates searching & sorting challenges with misconception-based distractors.

enum FormatType {
	F1_MULTIPLE_CHOICE,
	F2_TRUE_FALSE,
	F3_TRACE_ARRAY,
	F4_SWAP_PUZZLE,
	F5_POINTER_HUNT,
	F6_CALCULATION,
	F7_PARSONS_ORDER
}

# Generates a question based on monster type, topic, and difficulty
func generate_question_for_monster(monster_id: String) -> Dictionary:
	match monster_id:
		"slime":
			# TP-01: Array & Index (0-indexed)
			return _gen_tp01_array_indexing()
		"lost_wisp":
			# TP-02: Linear Search Trace
			return _gen_tp02_linear_search()
		"bookworm":
			# TP-03: Linear Search Special Cases (not found, duplicates)
			return _gen_tp03_linear_special_cases()
		"twin_librarian":
			# TP-04 / TP-05: Binary Search (mid pointer, preconditions)
			return _gen_tp05_binary_search()
		"skeleton_archer":
			# TP-06: Bubble Sort pass k
			return _gen_tp06_bubble_sort_pass()
		"venomous_arachnid":
			# TP-07: Selection Sort pass
			return _gen_tp07_selection_sort_pass()
		"grave_digger":
			# TP-08: Insertion Sort pass
			return _gen_tp08_insertion_sort_step()
		_:
			# Default to TP-01 or TP-06
			if randf() > 0.5:
				return _gen_tp01_array_indexing()
			else:
				return _gen_tp06_bubble_sort_pass()

# Calculate timer based on GDD formula:
# T = T_base + 0.5 * n_elements + T_item + T_mode
func calculate_timer(base_time: float, element_count: int) -> float:
	var t: float = base_time + (0.5 * element_count)
	if GameManager.inventory.get("hourglass_ring", false):
		t += 3.0
	match GameManager.current_mode:
		GameManager.DifficultyMode.STORY:
			t *= 1.5
		GameManager.DifficultyMode.NIGHTMARE:
			t *= 0.8
	return max(4.0, t)

# --- TP-01: Array Indexing (Slime) ---
func _gen_tp01_array_indexing() -> Dictionary:
	var arr_len: int = randi_range(4, 6)
	var arr: Array = []
	while arr.size() < arr_len:
		var v: int = randi_range(3, 40)
		if not arr.has(v):
			arr.append(v)
	
	var target_idx: int = randi_range(0, arr_len - 1)
	var correct_val: int = arr[target_idx]
	
	# Distractors based on common misconceptions:
	# 1. 1-based indexing (misconception: index starts at 1)
	# 2. Adjacent element
	# 3. Array length or random element
	var options: Array = [str(correct_val)]
	
	# Misconception 1: 1-indexed (asking for index k might pick arr[k-1] or arr[k+1])
	if target_idx > 0 and not options.has(str(arr[target_idx - 1])):
		options.append(str(arr[target_idx - 1]))
	if target_idx < arr_len - 1 and not options.has(str(arr[target_idx + 1])):
		options.append(str(arr[target_idx + 1]))
		
	# Fallback options
	while options.size() < 4:
		var fake: int = randi_range(1, 50)
		if not options.has(str(fake)):
			options.append(str(fake))
	
	options.shuffle()
	
	return {
		"format": FormatType.F1_MULTIPLE_CHOICE,
		"tp": "TP-01",
		"title": "Kekuatan Fondasi: Indeks Array",
		"question": "A = %s\nBerapakah nilai dari elemen pada indeks A[%d]?" % [str(arr), target_idx],
		"array": arr,
		"target_index": target_idx,
		"correct_answer": str(correct_val),
		"options": options,
		"explanation": "Indeks array pada ilmu komputer selalu dimulai dari 0. Maka A[%d] = %d." % [target_idx, correct_val],
		"timer": calculate_timer(6.0, arr.size())
	}

# --- TP-02: Linear Search (Lost Wisp) ---
func _gen_tp02_linear_search() -> Dictionary:
	var arr: Array = [7, 3, 9, 3, 5]
	arr.shuffle()
	# Ensure at least one element exists and duplicate
	var target_val: int = arr[randi_range(0, arr.size() - 1)]
	var first_idx: int = arr.find(target_val)
	var comparisons: int = first_idx + 1
	
	var q_type: int = randi() % 2
	if q_type == 0:
		# Return first index
		var options: Array = [str(first_idx)]
		# Misconception: last occurrence index
		var last_idx: int = arr.rfind(target_val)
		if last_idx != first_idx and not options.has(str(last_idx)):
			options.append(str(last_idx))
		# Misconception: 1-based index
		if not options.has(str(first_idx + 1)):
			options.append(str(first_idx + 1))
		while options.size() < 4:
			var fake_idx: int = randi_range(0, arr.size())
			if not options.has(str(fake_idx)):
				options.append(str(fake_idx))
		options.shuffle()
		
		return {
			"format": FormatType.F1_MULTIPLE_CHOICE,
			"tp": "TP-02",
			"title": "Penelusuran Cahaya: Linear Search",
			"question": "Lakukan Linear Search untuk mencari nilai %d pada deret %s.\nPada indeks berapakah pencarian pertama kali menemukan target?" % [target_val, str(arr)],
			"array": arr,
			"correct_answer": str(first_idx),
			"options": options,
			"explanation": "Linear Search memeriksa dari indeks 0 ke kanan dan berhenti di kecocokan pertama (indeks %d)." % first_idx,
			"timer": calculate_timer(7.0, arr.size())
		}
	else:
		# Comparison count
		var options: Array = [str(comparisons)]
		# Misconception: count = index (forgetting 0-index requires idx + 1 comparisons)
		if not options.has(str(first_idx)):
			options.append(str(first_idx))
		if not options.has(str(arr.size())):
			options.append(str(arr.size()))
		while options.size() < 4:
			var fake_c: int = randi_range(1, 6)
			if not options.has(str(fake_c)):
				options.append(str(fake_c))
		options.shuffle()
		
		return {
			"format": FormatType.F1_MULTIPLE_CHOICE,
			"tp": "TP-02",
			"title": "Hitungan Langkah: Linear Search",
			"question": "A = %s\nBerapa kali perbandingan dilakukan Linear Search untuk menemukan nilai %d?" % [str(arr), target_val],
			"array": arr,
			"correct_answer": str(comparisons),
			"options": options,
			"explanation": "Target ditemukan di indeks %d setelah memeriksa %d elemen." % [first_idx, comparisons],
			"timer": calculate_timer(7.0, arr.size())
		}

# --- TP-03: Linear Search Special Cases (Bookworm Horror) ---
func _gen_tp03_linear_special_cases() -> Dictionary:
	var arr: Array = [4, 8, 15, 16, 23, 42]
	var not_found_val: int = 99
	var options: Array = ["-1 (Data Tidak Ditemukan)", "0", str(arr.size()), "Null/Error"]
	options.shuffle()
	
	return {
		"format": FormatType.F1_MULTIPLE_CHOICE,
		"tp": "TP-03",
		"title": "Anomali Pustaka: Kasus Tidak Ditemukan",
		"question": "A = %s\nJika kita mencari nilai %d dengan fungsi pencarian standar, berapakah nilai return indeksnya?" % [str(arr), not_found_val],
		"array": arr,
		"correct_answer": "-1 (Data Tidak Ditemukan)",
		"options": options,
		"explanation": "Konvensi standar pencarian mengembalikan -1 jika nilai tidak ditemukan dalam array.",
		"timer": calculate_timer(6.5, arr.size())
	}

# --- TP-05: Binary Search (Twin Librarian) ---
func _gen_tp05_binary_search() -> Dictionary:
	var sorted_arr: Array = [2, 5, 8, 12, 16, 23, 38, 56]
	var low: int = 0
	var high: int = sorted_arr.size() - 1
	var mid: int = (low + high) / 2
	var target: int = 23
	
	# mid calculation: (0 + 7) // 2 = 3 (value 12)
	# 12 < 23 -> low = mid + 1 = 4
	var options: Array = [str(mid), str(mid + 1), str((low + high + 1) / 2), str(high)]
	# Remove duplicates
	var uniq: Array = []
	for o in options:
		if not uniq.has(o):
			uniq.append(o)
	while uniq.size() < 4:
		var r: String = str(randi_range(0, 7))
		if not uniq.has(r):
			uniq.append(r)
	uniq.shuffle()
	
	return {
		"format": FormatType.F1_MULTIPLE_CHOICE,
		"tp": "TP-05",
		"title": "Pecah Dua: Binary Search Mid Calculation",
		"question": "A = %s\nTarget: %d. Rentang awal low = %d, high = %d.\nDengan rumus mid = (low + high) // 2, berapakah indeks mid pertama?" % [str(sorted_arr), target, low, high],
		"array": sorted_arr,
		"correct_answer": str(mid),
		"options": uniq,
		"explanation": "mid = (0 + 7) // 2 = 3 (pembulatan ke bawah). Elemen A[3] bernilai %d." % sorted_arr[mid],
		"timer": calculate_timer(8.0, sorted_arr.size())
	}

# --- TP-06: Bubble Sort Pass 1 Trace (Skeleton Archer) ---
func _gen_tp06_bubble_sort_pass() -> Dictionary:
	var arr: Array = [5, 1, 4, 2, 8]
	# Simulate 1 pass of Bubble Sort ascending
	var pass1_arr: Array = arr.duplicate()
	for i in range(pass1_arr.size() - 1):
		if pass1_arr[i] > pass1_arr[i + 1]:
			var temp: int = pass1_arr[i]
			pass1_arr[i] = pass1_arr[i + 1]
			pass1_arr[i + 1] = temp
	
	# Correct answer is pass1_arr: [1, 4, 2, 5, 8]
	var correct_str: String = str(pass1_arr)
	
	# Distractor 1: fully sorted [1, 2, 4, 5, 8] (misconception: bubble sort sorts everything in 1 pass)
	var sorted_distractor: String = "[1, 2, 4, 5, 8]"
	# Distractor 2: only swapped first pair [1, 5, 4, 2, 8]
	var single_swap_distractor: String = "[1, 5, 4, 2, 8]"
	# Distractor 3: selection sort result [1, 5, 4, 2, 8] or inverted
	var inverted_distractor: String = "[5, 4, 2, 1, 8]"
	
	var options: Array = [correct_str, sorted_distractor, single_swap_distractor, inverted_distractor]
	options.shuffle()
	
	return {
		"format": FormatType.F3_TRACE_ARRAY,
		"tp": "TP-06",
		"title": "Panah Menggelembung: Bubble Sort Pass 1",
		"question": "Diberikan array acak: [5, 1, 4, 2, 8].\nBagaimanakah kondisi susunan array setelah PASS KE-1 Bubble Sort (urutan menaik)?",
		"array": arr,
		"correct_answer": correct_str,
		"options": options,
		"explanation": "Pada Pass 1 terjadi pertukaran bertahap (5↔1, 5↔4, 5↔2), sehingga elemen terbesar (5) terdorong ke posisi sebelum 8: %s." % correct_str,
		"timer": calculate_timer(8.5, arr.size())
	}

# --- TP-07: Selection Sort Pass (Venomous Arachnid) ---
func _gen_tp07_selection_sort_pass() -> Dictionary:
	var arr: Array = [64, 25, 12, 22, 11]
	# Pass 1: find min (11), swap with 64 -> [11, 25, 12, 22, 64]
	var pass1_arr: Array = [11, 25, 12, 22, 64]
	var correct_str: String = str(pass1_arr)
	var options: Array = [
		correct_str,
		"[25, 12, 22, 11, 64]", # bubble sort distractor
		"[11, 12, 22, 25, 64]", # fully sorted distractor
		"[64, 25, 12, 11, 22]"  # random swap
	]
	options.shuffle()
	
	return {
		"format": FormatType.F3_TRACE_ARRAY,
		"tp": "TP-07",
		"title": "Mangsa Terkecil: Selection Sort Pass 1",
		"question": "A = [64, 25, 12, 22, 11].\nSelection Sort mencari elemen terkecil untuk diletakkan di indeks awal.\nBagaimanakah kondisi array setelah Pass ke-1?",
		"array": arr,
		"correct_answer": correct_str,
		"options": options,
		"explanation": "Elemen terkecil (11) ditemukan lalu ditukar posisinya dengan elemen pada indeks 0 (64).",
		"timer": calculate_timer(8.5, arr.size())
	}

# --- TP-08: Insertion Sort (Grave Digger) ---
func _gen_tp08_insertion_sort_step() -> Dictionary:
	var arr: Array = [12, 11, 13, 5, 6]
	# After inserting 5: [5, 11, 12, 13, 6]
	var correct_str: String = "[5, 11, 12, 13, 6]"
	var options: Array = [
		correct_str,
		"[11, 12, 13, 5, 6]",
		"[5, 6, 11, 12, 13]",
		"[12, 5, 11, 13, 6]"
	]
	options.shuffle()
	
	return {
		"format": FormatType.F3_TRACE_ARRAY,
		"tp": "TP-08",
		"title": "Penyisipan Peti: Insertion Sort",
		"question": "Sub-array [11, 12, 13] sudah terurut. Kini elemen bernilai 5 disisipkan ke posisi yang tepat.\nBagaimanakah urutan array setelah elemen 5 disisipkan?",
		"array": arr,
		"correct_answer": correct_str,
		"options": options,
		"explanation": "Nilai 5 digeser melewati 13, 12, dan 11, lalu disisipkan tepat di bagian paling depan (indeks 0).",
		"timer": calculate_timer(8.5, arr.size())
	}
