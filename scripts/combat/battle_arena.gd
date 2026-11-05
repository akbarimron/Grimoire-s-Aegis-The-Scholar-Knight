extends Control

# Grimoire's Aegis: The Scholar Knight - Battle Arena Controller
# Implements Player Turn (5 actions + 8 Spells), Active Defend System (3 Tiers),
# 13 Bestiary Monsters with algorithmic attack patterns & debuffs,
# Themed Array Bar Visualization (Books, Candles, Crystals), Mercy System, and CSV Export.

signal turn_cycle_completed

enum BattleState {
	PLAYER_TURN,
	PLAYER_ACTION_ANIMATING,
	ENEMY_WINDUP,
	QUESTION_ACTIVE,
	QUESTION_RESOLVING,
	VICTORY,
	DEFEAT
}

var current_state: BattleState = BattleState.PLAYER_TURN

# Ordered progression of all 13 Monsters from Prolog to Final (GDD Section 9)
const MONSTER_GAUNTLET: Array[String] = [
	"slime",
	"lost_wisp",
	"bookworm_horror",
	"twin_faced_librarian",
	"the_blind_archivist",
	"skeleton_archer",
	"venomous_arachnid",
	"grave_digger",
	"the_abyss_golem",
	"hydra_of_division",
	"pivot_wraith",
	"the_sphinx_of_entropy",
	"entropy_chaos_king"
]
var gauntlet_index: int = 0
var current_monster: Dictionary = {}

# Active Question & Timing
var active_q: Dictionary = {}
var question_timer: float = 0.0
var question_max_time: float = 10.0
var reading_delay: float = 1.5
var is_reading_phase: bool = true
var flawless_battle: bool = true

# Status Effects & Boss Mechanics (GDD Section 6, 9, 15)
var monster_ordered_debuff: bool = false
var insertion_ward_active: bool = false
var poison_turns: int = 0
var silence_turns: int = 0
var buried_turns: int = 0
var crushed_turns: int = 0
var split_debuff: bool = false
var sphinx_death_seals: int = 0
var catalog_books_restored: int = 0
var selected_spell_idx: int = 0

# UI References
@onready var label_monster_name: Label = $ArenaLayout/TopPanel/MonsterInfo/LabelName
@onready var label_monster_title: Label = $ArenaLayout/TopPanel/MonsterInfo/LabelTitle
@onready var bar_monster_hp: ProgressBar = $ArenaLayout/TopPanel/MonsterInfo/BarHP
@onready var tex_monster_sprite: TextureRect = $ArenaLayout/CombatStage/MonsterNode/MonsterSprite

@onready var label_player_hp: Label = $ArenaLayout/BottomPanel/PlayerStats/LabelHP
@onready var bar_player_hp: ProgressBar = $ArenaLayout/BottomPanel/PlayerStats/BarHP
@onready var label_player_sta: Label = $ArenaLayout/BottomPanel/PlayerStats/LabelStamina
@onready var bar_player_sta: ProgressBar = $ArenaLayout/BottomPanel/PlayerStats/BarStamina
@onready var label_player_mem: Label = $ArenaLayout/BottomPanel/PlayerStats/LabelMemory
@onready var bar_player_mem: ProgressBar = $ArenaLayout/BottomPanel/PlayerStats/BarMemory
@onready var label_fragments: Label = $ArenaLayout/TopPanel/FragmentCounter/LabelCount

# Action Menu
@onready var btn_attack: Button = $ArenaLayout/BottomPanel/ActionMenu/TopActions/BtnAttack
@onready var btn_spell: Button = $ArenaLayout/BottomPanel/ActionMenu/TopActions/BtnSpell
@onready var btn_analyze: Button = $ArenaLayout/BottomPanel/ActionMenu/BottomActions/BtnAnalyze
@onready var btn_inventory: Button = $ArenaLayout/BottomPanel/ActionMenu/BottomActions/BtnInventory
@onready var btn_flee: Button = $ArenaLayout/BottomPanel/ActionMenu/BottomActions/BtnFlee

# Question Dialog Pop-up
@onready var question_panel: PanelContainer = $QuestionOverlay
@onready var label_q_title: Label = $QuestionOverlay/Margin/VBox/LabelQTitle
@onready var label_q_body: Label = $QuestionOverlay/Margin/VBox/LabelQBody
@onready var array_container: HBoxContainer = $QuestionOverlay/Margin/VBox/ArrayContainer
@onready var options_container: GridContainer = $QuestionOverlay/Margin/VBox/OptionsGrid
@onready var bar_timer: ProgressBar = $QuestionOverlay/Margin/VBox/TimerBar
@onready var label_feedback: Label = $QuestionOverlay/Margin/VBox/LabelFeedback

# Visual FX & Shields
@onready var aegis_shield: TextureRect = $ArenaLayout/CombatStage/PlayerNode/AegisShieldFX
@onready var label_combat_log: Label = $ArenaLayout/LogPanel/LabelLog

func _ready() -> void:
	_connect_buttons()
	GameManager.stats_changed.connect(_update_ui)
	GameManager.mercy_kilas_codex_triggered.connect(_on_mercy_kilas_codex)
	question_panel.visible = false
	aegis_shield.visible = false
	_load_monster(MONSTER_GAUNTLET[gauntlet_index])
	_log("Pertarungan dimulai! Scholar Knight menghadapi %s. [Tab: Ganti Mantra | N: Monster Berikutnya | T: Ekspor CSV Guru]" % current_monster["name"])

func _connect_buttons() -> void:
	btn_attack.pressed.connect(_on_attack_pressed)
	btn_spell.pressed.connect(_on_spell_pressed)
	btn_analyze.pressed.connect(_on_analyze_pressed)
	btn_inventory.pressed.connect(_on_inventory_pressed)
	btn_flee.pressed.connect(_on_flee_pressed)

func _process(delta: float) -> void:
	if current_state == BattleState.QUESTION_ACTIVE:
		if is_reading_phase:
			reading_delay -= delta
			bar_timer.value = 100.0
			label_feedback.text = "Masa Baca: %.1f dtk..." % max(0.0, reading_delay)
			if reading_delay <= 0.0:
				is_reading_phase = false
				label_feedback.text = "PILIH JAWABAN ANDA! (Tombol 1-4 atau Klik)"
		else:
			question_timer -= delta
			var progress_pct: float = (question_timer / question_max_time) * 100.0
			bar_timer.value = progress_pct
			if progress_pct >= 60.0:
				bar_timer.modulate = Color(0.1, 0.9, 1.0) # Blue -> Perfect Guard zone
			elif progress_pct > 30.0:
				bar_timer.modulate = Color(1.0, 0.8, 0.2) # Yellow -> Normal Guard zone
			else:
				bar_timer.modulate = Color(1.0, 0.2, 0.2) # Red -> Critical 30% zone
			if question_timer <= 0.0:
				_resolve_answer("TIMEOUT_NO_ANSWER")

func _load_monster(monster_type: String) -> void:
	var template: Dictionary = GameManager.BESTIARY.get(monster_type, GameManager.BESTIARY["slime"])
	current_monster = template.duplicate(true)
	current_monster["current_hp"] = int(current_monster["max_hp"])
	# Mercy System: Boss starts with -15% HP after 3 deaths on the same boss (GDD Section 15)
	if current_monster.get("is_boss", false) and int(GameManager.boss_deaths.get(monster_type, 0)) >= 3:
		current_monster["current_hp"] = int(float(current_monster["max_hp"]) * 0.85)
	flawless_battle = true
	monster_ordered_debuff = false
	sphinx_death_seals = 0
	if ResourceLoader.exists(current_monster["sprite"]):
		tex_monster_sprite.texture = load(current_monster["sprite"])
	_update_ui()

func _get_active_spell_id() -> String:
	if GameManager.unlocked_spells.is_empty():
		return "linear_strike"
	selected_spell_idx = selected_spell_idx % GameManager.unlocked_spells.size()
	return GameManager.unlocked_spells[selected_spell_idx]

func _update_ui() -> void:
	if current_monster.is_empty():
		return
	var boss_tag: String = ""
	if current_monster["id"] == "the_sphinx_of_entropy":
		boss_tag = " [Segel: %d/3]" % sphinx_death_seals
	elif current_monster["id"] == "entropy_chaos_king":
		boss_tag = " [Katalog: %d/10]" % catalog_books_restored
	elif monster_ordered_debuff:
		boss_tag = " [ORDERED]"

	label_monster_name.text = "%s (Lv.%d)%s" % [current_monster["name"], current_monster.get("level", 1), boss_tag]
	label_monster_title.text = current_monster["title"]
	bar_monster_hp.max_value = current_monster["max_hp"]
	bar_monster_hp.value = current_monster["current_hp"]

	label_player_hp.text = "HP: %d / %d (Lv.%d)" % [GameManager.current_hp, GameManager.max_hp, GameManager.level]
	bar_player_hp.max_value = GameManager.max_hp
	bar_player_hp.value = GameManager.current_hp

	label_player_sta.text = "STA: %d / %d" % [GameManager.current_stamina, GameManager.max_stamina]
	bar_player_sta.max_value = GameManager.max_stamina
	bar_player_sta.value = GameManager.current_stamina

	label_player_mem.text = "MEM: %d / %d | Panic: %d/3" % [GameManager.current_memory, GameManager.max_memory, GameManager.panic_level]
	bar_player_mem.max_value = GameManager.max_memory
	bar_player_mem.value = GameManager.current_memory

	label_fragments.text = "Fragments: %d | Gold: %d" % [GameManager.logic_fragments, GameManager.gold]

	var spell_id: String = _get_active_spell_id()
	var spell_data: Dictionary = GameManager.SPELLS.get(spell_id, GameManager.SPELLS["linear_strike"])
	var spell_cost: int = int(spell_data["cost"])
	btn_spell.text = "[2] %s (%d MEM)" % [spell_data["name"], spell_cost]

	var is_player_turn: bool = (current_state == BattleState.PLAYER_TURN)
	btn_attack.disabled = (GameManager.current_stamina < 10 or not is_player_turn)
	btn_spell.disabled = (GameManager.current_memory < spell_cost or silence_turns > 0 or not is_player_turn)
	btn_inventory.disabled = (buried_turns > 0 or not is_player_turn)
	btn_analyze.disabled = (not is_player_turn)
	btn_flee.disabled = (current_monster.get("is_boss", false) or not is_player_turn)

func _log(msg: String) -> void:
	label_combat_log.text = msg

# --- PLAYER TURN ACTIONS (GDD Section 6) ---

func _on_attack_pressed() -> void:
	if current_state != BattleState.PLAYER_TURN or GameManager.current_stamina < 10:
		return
	GameManager.current_stamina -= 10
	current_state = BattleState.PLAYER_ACTION_ANIMATING
	_update_ui()

	var eff_def: int = int(current_monster["def"])
	if monster_ordered_debuff:
		eff_def = int(float(eff_def) * 0.75)
	var raw_dmg: float = max(1.0, float(GameManager.attack_power - eff_def))
	var final_dmg: int = int(raw_dmg * randf_range(0.9, 1.1))
	if split_debuff:
		final_dmg = max(1, int(float(final_dmg) * 0.5))
		split_debuff = false

	current_monster["current_hp"] = max(0, int(current_monster["current_hp"]) - final_dmg)
	_log("Tebasan Fisik mengenai %s sebesar %d damage!" % [current_monster["name"], final_dmg])
	_update_ui()

	if int(current_monster["current_hp"]) <= 0:
		_handle_victory()
	else:
		await get_tree().create_timer(0.8).timeout
		_start_enemy_turn()

func _on_spell_pressed() -> void:
	if current_state != BattleState.PLAYER_TURN or silence_turns > 0:
		return
	var spell_id: String = _get_active_spell_id()
	var spell_data: Dictionary = GameManager.SPELLS[spell_id]
	var cost: int = int(spell_data["cost"])
	if GameManager.current_memory < cost:
		return

	# Binary Lance precondition check (GDD Section 6: requires Ordered status)
	if spell_id == "binary_lance" and not monster_ordered_debuff:
		_log("SYARAT GAGAL! Binary Lance butuh target berstatus ORDERED (gunakan Bubble Shift / Order Restored dulu)!")
		return

	GameManager.current_memory -= cost
	current_state = BattleState.PLAYER_ACTION_ANIMATING

	if spell_id in ["bubble_shift", "order_restored"]:
		monster_ordered_debuff = true
	if spell_id == "insertion_ward":
		insertion_ward_active = true

	var base_power: float = float(spell_data["power"])
	var dmg: int = int(base_power * (1.0 + float(GameManager.intelligence) / 20.0))
	if current_monster.get("weakness", "") == spell_id:
		dmg = int(float(dmg) * 1.5) # +50% weakness multiplier per GDD Section 13

	current_monster["current_hp"] = max(0, int(current_monster["current_hp"]) - dmg)
	_log("Mantra Grimoire [%s] dilancarkan! Damage sihir: %d." % [spell_data["name"], dmg])
	_update_ui()

	if int(current_monster["current_hp"]) <= 0:
		_handle_victory()
	else:
		await get_tree().create_timer(0.9).timeout
		_start_enemy_turn()

func _on_analyze_pressed() -> void:
	if current_state != BattleState.PLAYER_TURN:
		return
	GameManager.codex_opened_count += 1
	if GameManager.current_mode == GameManager.DifficultyMode.NIGHTMARE:
		_log("ANALYZE (Nightmare): HP %d/%d, ATK %d, DEF %d. Topik soal disembunyikan!" % [
			current_monster["current_hp"], current_monster["max_hp"], current_monster["atk"], current_monster["def"]
		])
	else:
		_log("ANALYZE BESTIARY: %s | Kelemahan Spell: %s | Efek Gagal: %s" % [
			current_monster["title"], current_monster.get("weakness", "-"), current_monster.get("debuff_on_fail", "-")
		])
	await get_tree().create_timer(1.2).timeout
	_start_enemy_turn()

func _on_inventory_pressed() -> void:
	if current_state != BattleState.PLAYER_TURN or buried_turns > 0:
		return
	if poison_turns > 0 and int(GameManager.inventory.get("antidote", 0)) > 0:
		GameManager.inventory["antidote"] -= 1
		poison_turns = 0
		_log("Memakai Antidote! Status Poison sembuh.")
	elif GameManager.current_hp < GameManager.max_hp and int(GameManager.inventory.get("potion", 0)) > 0:
		GameManager.inventory["potion"] -= 1
		GameManager.heal_hp(40)
		_log("Meminum Potion! Pulih +40 HP.")
	elif int(GameManager.inventory.get("ether", 0)) > 0:
		GameManager.inventory["ether"] -= 1
		GameManager.restore_memory(25)
		_log("Meminum Ether! Pulih +25 Memory.")
	else:
		_log("Item tidak tersedia atau HP/Memory masih penuh!")
		return
	await get_tree().create_timer(0.8).timeout
	_start_enemy_turn()

func _on_flee_pressed() -> void:
	if current_state != BattleState.PLAYER_TURN or current_monster.get("is_boss", false):
		return
	var flee_chance: float = 0.40 + 0.10 * float(GameManager.inventory.get("boots_of_haste_level", 0))
	if randf() <= flee_chance:
		_log("Berhasil kabur dari pertarungan (Peluang %d%%)!" % int(flee_chance * 100.0))
		await get_tree().create_timer(1.0).timeout
		_load_monster(MONSTER_GAUNTLET[gauntlet_index])
		current_state = BattleState.PLAYER_TURN
	else:
		_log("Gagal kabur! Musuh langsung menyerang!")
		await get_tree().create_timer(0.8).timeout
		_start_enemy_turn()

# --- ENEMY ACTIVE DEFEND SYSTEM (GDD Section 7) ---

func _start_enemy_turn() -> void:
	current_state = BattleState.ENEMY_WINDUP
	_log("PERINGATAN! %s melancarkan serangan Glitch! (Bullet-time 0.6s)" % current_monster["name"])
	await get_tree().create_timer(0.6).timeout

	active_q = QuestionGenerator.generate_question_for_monster(current_monster["id"])
	question_max_time = float(active_q.get("timer", 8.0))
	if monster_ordered_debuff:
		question_max_time += 2.0
	question_timer = question_max_time
	reading_delay = 1.5
	is_reading_phase = true

	_show_question_popup(active_q)
	current_state = BattleState.QUESTION_ACTIVE

func _show_question_popup(q: Dictionary) -> void:
	question_panel.visible = true
	label_q_title.text = "[ %s | %s ] — %s" % [q.get("tp", "TP-01"), q.get("format_code", "F1"), q.get("title", "Tantangan Logika")]
	label_q_body.text = q.get("question", "")
	label_feedback.text = "Masa Baca: 1.5 dtk..."
	label_feedback.modulate = Color(0.95, 0.9, 0.7)

	# Render Themed Array Visual Bars + 0-Based Index Badges (GDD Section 17)
	for child in array_container.get_children():
		child.queue_free()

	if q.has("array") and q["array"] is Array:
		var arr: Array = q["array"]
		var max_val: int = 1
		for v in arr:
			if int(v) > max_val:
				max_val = int(v)

		var act_id: int = int(current_monster.get("act", 1))
		var bar_color := Color(0.30, 0.72, 0.96) # Act I: Pale Blue Lantern / Books
		if act_id == 2:
			bar_color = Color(0.92, 0.78, 0.32)  # Act II: Bone Yellow / Candles
		elif act_id >= 3:
			bar_color = Color(0.72, 0.42, 0.96)  # Act III/Final: Split Purple Crystal

		for i in range(arr.size()):
			var card := VBoxContainer.new()
			card.alignment = BoxContainer.ALIGNMENT_END
			card.custom_minimum_size = Vector2(36, 36)

			var bar := ColorRect.new()
			var h_ratio: float = clampf(float(arr[i]) / float(max_val), 0.25, 1.0)
			bar.custom_minimum_size = Vector2(28, int(18.0 * h_ratio))
			bar.color = bar_color
			if q.get("target_index", -1) == i:
				bar.color = Color(1.0, 0.85, 0.2) # Highlight compared/target element

			var val_lbl := Label.new()
			val_lbl.text = str(arr[i])
			val_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			val_lbl.add_theme_font_size_override("font_size", 10)

			var idx_lbl := Label.new()
			idx_lbl.text = "[%d]" % i
			idx_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			idx_lbl.add_theme_font_size_override("font_size", 9)
			idx_lbl.modulate = Color(0.45, 0.85, 1.0)

			card.add_child(val_lbl)
			card.add_child(bar)
			card.add_child(idx_lbl)
			array_container.add_child(card)

	# Render Options Buttons
	for child in options_container.get_children():
		child.queue_free()

	var opts: Array = q.get("options", []).duplicate()
	if GameManager.panic_level >= 3:
		opts.shuffle() # Panic level 3 Confusion

	for i in range(opts.size()):
		var opt = opts[i]
		var btn := Button.new()
		btn.text = "[%d] %s" % [i + 1, str(opt)]
		btn.custom_minimum_size = Vector2(195, 28)
		btn.add_theme_font_size_override("font_size", 9)

		var sb := StyleBoxFlat.new()
		sb.bg_color = Color(0.16, 0.20, 0.27, 1.0)
		sb.set_border_width_all(1)
		sb.border_color = Color(0.4, 0.6, 0.8, 1.0)
		sb.set_corner_radius_all(3)
		sb.content_margin_left = 6
		sb.content_margin_right = 6
		btn.add_theme_stylebox_override("normal", sb)

		var sb_hover: StyleBoxFlat = sb.duplicate()
		sb_hover.bg_color = Color(0.24, 0.32, 0.44, 1.0)
		sb_hover.border_color = Color(0.2, 0.9, 1.0, 1.0)
		btn.add_theme_stylebox_override("hover", sb_hover)

		btn.pressed.connect(func():
			if not is_reading_phase and current_state == BattleState.QUESTION_ACTIVE:
				_resolve_answer(str(opt))
		)
		options_container.add_child(btn)

func _unhandled_input(event: InputEvent) -> void:
	if not (event is InputEventKey) or not event.is_pressed() or event.is_echo():
		return
	var key: int = event.keycode
	if key == KEY_TAB:
		selected_spell_idx = (selected_spell_idx + 1) % max(1, GameManager.unlocked_spells.size())
		_update_ui()
		return
	elif key == KEY_N and current_state == BattleState.PLAYER_TURN:
		gauntlet_index = (gauntlet_index + 1) % MONSTER_GAUNTLET.size()
		_load_monster(MONSTER_GAUNTLET[gauntlet_index])
		_log("Berpindah ke monster tantangan berikutnya: %s!" % current_monster["name"])
		return
	elif key == KEY_T:
		var path: String = GameManager.export_teacher_report_csv()
		_log("Laporan Guru diekspor ke CSV: %s" % path)
		return
	elif key == KEY_R and current_state == BattleState.DEFEAT:
		GameManager.reset_player_to_defaults()
		_load_monster(MONSTER_GAUNTLET[gauntlet_index])
		current_state = BattleState.PLAYER_TURN
		_log("Pertarungan diulang dengan soal prosedural baru!")
		return

	if current_state == BattleState.PLAYER_TURN:
		match key:
			KEY_1: _on_attack_pressed()
			KEY_2: _on_spell_pressed()
			KEY_3: _on_analyze_pressed()
			KEY_4: _on_inventory_pressed()
			KEY_5: _on_flee_pressed()
	elif current_state == BattleState.QUESTION_ACTIVE and not is_reading_phase:
		var opts: Array = active_q.get("options", [])
		var idx: int = -1
		match key:
			KEY_1: idx = 0
			KEY_2: idx = 1
			KEY_3: idx = 2
			KEY_4: idx = 3
		if idx >= 0 and idx < opts.size():
			_resolve_answer(str(opts[idx]))

func _resolve_answer(chosen_answer: String) -> void:
	current_state = BattleState.QUESTION_RESOLVING
	var correct: String = active_q.get("correct_answer", "")
	var is_correct: bool = (chosen_answer == correct)
	var time_left_ratio: float = clampf(question_timer / max(0.01, question_max_time), 0.0, 1.0)
	var time_spent: float = max(0.0, question_max_time - question_timer)
	var explanation: String = active_q.get("explanation", "")

	GameManager.record_question_attempt(active_q, chosen_answer, is_correct, time_left_ratio, time_spent)

	if is_correct and time_left_ratio >= 0.60:
		_handle_perfect_guard(explanation)
	elif is_correct:
		_handle_guard(explanation)
	else:
		flawless_battle = false
		_handle_break(chosen_answer, explanation)

func _handle_perfect_guard(explanation: String) -> void:
	_log("AEGIS ACTIVE! PERFECT GUARD! (0 Damage, Counter-Attack +50% ATK, +1 Logic Fragment)")
	label_feedback.text = "PERFECT GUARD! (+1 Logic Fragment, Counter Attack!)\n%s" % explanation
	label_feedback.modulate = Color(0.2, 1.0, 0.4)

	aegis_shield.visible = true
	GameManager.clear_one_panic()
	GameManager.add_reward(0, 1, 5)
	if GameManager.equipped_weapon == "aegis_blade":
		GameManager.restore_memory(5)

	if current_monster["id"] == "entropy_chaos_king":
		catalog_books_restored = min(10, catalog_books_restored + 1)

	var counter_mult: float = 0.60 if GameManager.equipped_weapon == "pedang_indeks" else 0.50
	var counter_dmg: int = max(1, int(float(GameManager.attack_power) * counter_mult))
	current_monster["current_hp"] = max(0, int(current_monster["current_hp"]) - counter_dmg)
	_update_ui()

	await get_tree().create_timer(2.0).timeout
	aegis_shield.visible = false
	question_panel.visible = false

	if int(current_monster["current_hp"]) <= 0 or catalog_books_restored >= 10:
		_handle_victory()
	else:
		_end_turn_and_start_player()

func _handle_guard(explanation: String) -> void:
	_log("GUARD BERHASIL! Perisai menahan serangan (0 Damage / MISS).")
	label_feedback.text = "GUARD SUKSES! (0 Damage)\n%s" % explanation
	label_feedback.modulate = Color(0.4, 0.8, 1.0)

	await get_tree().create_timer(1.8).timeout
	question_panel.visible = false
	_end_turn_and_start_player()

func _handle_break(chosen: String, explanation: String) -> void:
	# Check Aegis Charm auto-block (GDD Section 14)
	if int(GameManager.inventory.get("aegis_charm", 0)) > 0:
		GameManager.inventory["aegis_charm"] -= 1
		_log("AEGIS CHARM AKTIF! Jimat Pengampunan memblokir 1 Break secara otomatis!")
		label_feedback.text = "AEGIS CHARM MEMBLOKIR BREAK! Kunci: %s\n%s" % [active_q.get("correct_answer", ""), explanation]
		label_feedback.modulate = Color(1.0, 0.85, 0.3)
		await get_tree().create_timer(2.0).timeout
		question_panel.visible = false
		_end_turn_and_start_player()
		return

	# Damage Break Formula (GDD Section 7):
	# Damage = min(3 * ATK_musuh - DEF_pemain, 0.5 * HP_maks) (0.25 in Story Mode)
	var eff_player_def: int = GameManager.defense_power
	if crushed_turns > 0:
		eff_player_def = int(float(eff_player_def) * 0.80)
	var raw_break: int = (3 * int(current_monster["atk"])) - eff_player_def
	var max_cap: int = int(float(GameManager.max_hp) * (0.25 if GameManager.current_mode == GameManager.DifficultyMode.STORY else 0.50))
	var final_break_dmg: int = clampi(raw_break, 4, max_cap)
	if insertion_ward_active:
		final_break_dmg = int(float(final_break_dmg) * 0.70)
		insertion_ward_active = false

	GameManager.take_damage(final_break_dmg)
	GameManager.add_panic(1)
	_apply_monster_fail_effect()

	_log("BREAK (Lethal Strike)! Terkena %d damage + 1 tumpukan Panic." % final_break_dmg)
	label_feedback.text = "BREAK! Jawaban: %s | Kunci: %s\n%s" % [chosen, active_q.get("correct_answer", ""), explanation]
	label_feedback.modulate = Color(1.0, 0.3, 0.3)
	_update_ui()

	await get_tree().create_timer(2.2).timeout
	question_panel.visible = false

	if GameManager.current_hp <= 0:
		_handle_defeat()
	else:
		_end_turn_and_start_player()

func _apply_monster_fail_effect() -> void:
	var effect: String = current_monster.get("debuff_on_fail", "none")
	match effect:
		"poison":
			poison_turns = 3
		"silence", "torn_page":
			silence_turns = 2
		"buried":
			buried_turns = 2
		"crushed":
			crushed_turns = 2
		"hydra_regen":
			var heal: int = int(float(current_monster["max_hp"]) * 0.10)
			current_monster["current_hp"] = min(int(current_monster["max_hp"]), int(current_monster["current_hp"]) + heal)
		"split":
			split_debuff = true
		"death_seal":
			sphinx_death_seals += 1
			if sphinx_death_seals >= 3:
				sphinx_death_seals = 0
				GameManager.current_hp = max(1, min(GameManager.current_hp, 1))
				_log("DEATH SENTENCE! 3 Segel Sphinx menyala — HP Scholar Knight tersisa 1!")
		"unshuffle_catalog":
			catalog_books_restored = max(0, catalog_books_restored - 1)

func _end_turn_and_start_player() -> void:
	GameManager.restore_turn_stamina()
	if poison_turns > 0:
		poison_turns -= 1
		var p_dmg: int = max(1, int(float(GameManager.max_hp) * 0.05))
		GameManager.take_damage(p_dmg)
	if silence_turns > 0: silence_turns -= 1
	if buried_turns > 0: buried_turns -= 1
	if crushed_turns > 0: crushed_turns -= 1

	current_state = BattleState.PLAYER_TURN
	_update_ui()
	_log("Giliran Pemain! Stamina pulih +15. Pilih aksi [1-5] atau [Tab] ganti mantra.")

func _on_mercy_kilas_codex(tp_code: String) -> void:
	var entry: Dictionary = GameManager.CODEX_ENTRIES.get(tp_code, GameManager.CODEX_ENTRIES["TP-01"])
	_log("MERCY SYSTEM — KILAS CODEX (%s): %s" % [entry["title"], entry["summary"]])

func _handle_victory() -> void:
	current_state = BattleState.VICTORY
	GameManager.clear_all_panic()
	var base_exp: int = int(current_monster.get("level", 1)) * 10
	if flawless_battle:
		base_exp = int(float(base_exp) * 1.20) # +20% Flawless EXP bonus (GDD Section 13)
	var frag_bonus: int = 1 if flawless_battle else 0
	GameManager.add_reward(int(current_monster["gold"]), frag_bonus, base_exp)

	_log("KEMENANGAN%s! %s dikalahkan! (+%d Gold, +%d EXP)." % [
		" FLAWLESS" if flawless_battle else "",
		current_monster["name"],
		int(current_monster["gold"]),
		base_exp
	])
	await get_tree().create_timer(2.0).timeout

	gauntlet_index = (gauntlet_index + 1) % MONSTER_GAUNTLET.size()
	_load_monster(MONSTER_GAUNTLET[gauntlet_index])
	current_state = BattleState.PLAYER_TURN
	_log("Monster berikutnya muncul: %s!" % current_monster["name"])

func _handle_defeat() -> void:
	current_state = BattleState.DEFEAT
	var m_id: String = current_monster.get("id", "slime")
	if current_monster.get("is_boss", false):
		GameManager.boss_deaths[m_id] = int(GameManager.boss_deaths.get(m_id, 0)) + 1
	# Lose 30% Gold on dungeon death, Logic Fragments safe (GDD Section 15)
	GameManager.gold = int(float(GameManager.gold) * 0.70)
	_update_ui()
	_log("KSATRIA TUMBANG... Kehilangan 30% Gold (Logic Fragments aman). Tekan [R] untuk bangkit di Patung Malaikat.")
