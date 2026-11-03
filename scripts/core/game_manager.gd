extends Node

# Grimoire's Aegis: The Scholar Knight - Core Game Manager
# Manages player progression, inventory, stats, and global state.

signal stats_changed
signal panic_changed(level: int)
signal combat_ended(victory: bool)

# Difficulty Modes
enum DifficultyMode { STORY, SCHOLAR, NIGHTMARE }
var current_mode: DifficultyMode = DifficultyMode.SCHOLAR

# Player Core Stats (Lv 1 Base Stats per GDD section 13)
var level: int = 1
var exp_points: int = 0
var max_hp: int = 100
var current_hp: int = 100
var max_stamina: int = 50
var current_stamina: int = 50
var max_memory: int = 40
var current_memory: int = 40
var attack_power: int = 12
var intelligence: int = 10
var defense_power: int = 5

# Economy
var gold: int = 0
var logic_fragments: int = 0

# Panic / Mental Fatigue (0 to 3 per GDD section 15)
# 1 = question text shakes slightly
# 2 = screen edges blur
# 3 = choice buttons are shuffled / confusion
var panic_level: int = 0:
	set(val):
		panic_level = clampi(val, 0, 3)
		panic_changed.emit(panic_level)

# Items
var inventory: Dictionary = {
	"potion": 3,      # +40 HP
	"ether": 2,       # +25 Memory
	"antidote": 1,    # Cure Poison
	"hourglass_ring": false, # +3 sec on timer
	"helm_of_clarity": false, # Removes 1 wrong option in F1/F3
}

# Equipped items (max 2 accessories)
var equipped_accessories: Array[String] = []

# Spells unlocked per Act progression
var unlocked_spells: Array[String] = ["linear_strike", "binary_lance"]

# Spell definitions
const SPELLS = {
	"linear_strike": {
		"name": "Linear Strike",
		"cost": 8,
		"power": 18,
		"desc": "Menyerang musuh secara beruntun dari indeks 0 hingga target.",
		"act": 1
	},
	"binary_lance": {
		"name": "Binary Lance",
		"cost": 14,
		"power": 32,
		"desc": "Tombak berdaya rusak tinggi. Efektif saat barisan musuh terurut.",
		"act": 1
	},
	"bubble_shift": {
		"name": "Bubble Shift",
		"cost": 10,
		"power": 15,
		"desc": "Menukar posisi elemen dan menyingkap musuh terlemah ke depan.",
		"act": 2
	},
	"selection_mark": {
		"name": "Selection Mark",
		"cost": 12,
		"power": 24,
		"desc": "Menandai kelemahan musuh bernilai terendah untuk serangan kritis.",
		"act": 2
	},
	"insertion_ward": {
		"name": "Insertion Ward",
		"cost": 12,
		"power": 0,
		"desc": "Menyisipkan perisai logika, mengurangi 30% damage di giliran berikutnya.",
		"act": 2
	},
	"merge_barrage": {
		"name": "Merge Barrage",
		"cost": 20,
		"power": 45,
		"desc": "Membelah serangan menjadi dua lalu menggabungkannya dalam combo dahsyat.",
		"act": 3
	},
	"quick_pivot": {
		"name": "Quick Pivot",
		"cost": 18,
		"power": 38,
		"desc": "Memilih pivot untuk memecah formasi musuh dan memberi debuff.",
		"act": 3
	}
}

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
	# Check level up
	var req_exp: int = level * 50
	if exp_points >= req_exp:
		exp_points -= req_exp
		level_up()
	stats_changed.emit()

func level_up() -> void:
	level += 1
	max_hp += 12
	current_hp = max_hp
	max_stamina += 3
	max_memory += 5
	attack_power += 2
	intelligence += 2
	defense_power += 1
	stats_changed.emit()
